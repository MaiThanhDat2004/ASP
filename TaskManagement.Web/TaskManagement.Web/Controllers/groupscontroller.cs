using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Web.Models;
using TaskManagement.Web.Repositories;

namespace TaskManagement.Web.Controllers
{
    [Authorize]
    public class GroupsController : Controller
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IUserRepository _userRepository;

        public GroupsController(
            IGroupRepository groupRepository,
            IUserRepository userRepository)
        {
            _groupRepository = groupRepository;
            _userRepository = userRepository;
        }

        // ── DANH SÁCH NHÓM CỦA TÔI ────────────────────────────────────────
        public IActionResult Index()
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var groups = _groupRepository.GetByUserId(userId).ToList();

            // Lấy vai trò của user trong từng nhóm
            var myRoles = new Dictionary<int, string>();
            foreach (var g in groups)
            {
                var member = g.Members.FirstOrDefault(m => m.UserId == userId);
                myRoles[g.GroupId] = member?.Role ?? "Member";
            }

            ViewBag.MyRoles = myRoles;
            ViewBag.CurrentUserId = userId;
            return View(groups);
        }

        // ── CHI TIẾT NHÓM ─────────────────────────────────────────────────
        public IActionResult Details(int id)
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var group = _groupRepository.GetById(id);
            if (group == null) return NotFound();

            // Kiểm tra user có trong nhóm không
            var myMembership = group.Members.FirstOrDefault(m => m.UserId == userId);
            if (myMembership == null)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xem nhóm này!";
                return RedirectToAction("Index");
            }

            ViewBag.MyRole = myMembership.Role;
            ViewBag.CurrentUserId = userId;

            // Lấy danh sách user chưa trong nhóm (để thêm thành viên)
            var allUsers = _userRepository.GetAll().ToList();
            var memberIds = group.Members.Select(m => m.UserId).ToList();
            ViewBag.AvailableUsers = allUsers.Where(u => !memberIds.Contains(u.UserId)).ToList();

            return View(group);
        }

        // ── TẠO NHÓM MỚI ──────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string name, string? description)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập tên nhóm!";
                return View();
            }

            var userId = int.Parse(User.FindFirst("UserId")!.Value);

            var group = new Group
            {
                Name = name,
                Description = description,
                CreatedBy = userId,
                CreatedAt = DateTime.Now
            };

            _groupRepository.Add(group);
            _groupRepository.Save();

            // Người tạo nhóm tự động là Leader
            var leaderMember = new GroupMember
            {
                GroupId = group.GroupId,
                UserId = userId,
                Role = "Leader",
                JoinedAt = DateTime.Now
            };

            // Dùng context trực tiếp qua GroupRepository không có method AddMember
            // nên ta inject thêm — tạm thời lưu vào DB qua context
            TempData["SuccessMessage"] = $"Tạo nhóm \"{name}\" thành công! Bạn là Trưởng nhóm.";

            // Redirect để AddMember tự động
            return RedirectToAction("AddLeader", new { groupId = group.GroupId, userId });
        }

        // ── TỰ ĐỘNG THÊM LEADER SAU KHI TẠO NHÓM ─────────────────────────
        public IActionResult AddLeader(int groupId, int userId)
        {
            var group = _groupRepository.GetById(groupId);
            if (group == null) return NotFound();

            // Kiểm tra chưa có member nào
            if (!group.Members.Any(m => m.UserId == userId))
            {
                group.Members.Add(new GroupMember
                {
                    GroupId = groupId,
                    UserId = userId,
                    Role = "Leader",
                    JoinedAt = DateTime.Now
                });
                _groupRepository.Update(group);
                _groupRepository.Save();
            }

            return RedirectToAction("Details", new { id = groupId });
        }

        // ── THÊM THÀNH VIÊN VÀO NHÓM ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddMember(int groupId, int userId, string role = "Member")
        {
            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);
            var group = _groupRepository.GetById(groupId);
            if (group == null) return NotFound();

            // Chỉ Leader mới được thêm thành viên
            var myRole = group.Members.FirstOrDefault(m => m.UserId == currentUserId)?.Role;
            if (myRole != "Leader")
            {
                TempData["ErrorMessage"] = "Chỉ Trưởng nhóm mới có quyền thêm thành viên!";
                return RedirectToAction("Details", new { id = groupId });
            }

            // Kiểm tra đã trong nhóm chưa
            if (group.Members.Any(m => m.UserId == userId))
            {
                TempData["ErrorMessage"] = "Người này đã là thành viên của nhóm!";
                return RedirectToAction("Details", new { id = groupId });
            }

            group.Members.Add(new GroupMember
            {
                GroupId = groupId,
                UserId = userId,
                Role = role == "Leader" ? "Leader" : "Member",
                JoinedAt = DateTime.Now
            });

            _groupRepository.Update(group);
            _groupRepository.Save();

            var user = _userRepository.GetById(userId);
            TempData["SuccessMessage"] = $"Đã thêm {user?.FullName} vào nhóm!";
            return RedirectToAction("Details", new { id = groupId });
        }

        // ── ĐỔI VAI TRÒ THÀNH VIÊN ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeRole(int groupId, int memberId, string newRole)
        {
            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);
            var group = _groupRepository.GetById(groupId);
            if (group == null) return NotFound();

            // Chỉ Leader mới được đổi vai trò
            var myRole = group.Members.FirstOrDefault(m => m.UserId == currentUserId)?.Role;
            if (myRole != "Leader")
            {
                TempData["ErrorMessage"] = "Chỉ Trưởng nhóm mới có quyền thay đổi vai trò!";
                return RedirectToAction("Details", new { id = groupId });
            }

            // Không được tự đổi vai trò của chính mình
            if (memberId == currentUserId)
            {
                TempData["ErrorMessage"] = "Bạn không thể thay đổi vai trò của chính mình!";
                return RedirectToAction("Details", new { id = groupId });
            }

            var member = group.Members.FirstOrDefault(m => m.UserId == memberId);
            if (member != null)
            {
                member.Role = newRole == "Leader" ? "Leader" : "Member";
                _groupRepository.Update(group);
                _groupRepository.Save();
                TempData["SuccessMessage"] = "Đã cập nhật vai trò thành công!";
            }

            return RedirectToAction("Details", new { id = groupId });
        }

        // ── XÓA THÀNH VIÊN KHỎI NHÓM ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveMember(int groupId, int userId)
        {
            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);
            var group = _groupRepository.GetById(groupId);
            if (group == null) return NotFound();

            // Chỉ Leader mới được xóa thành viên
            var myRole = group.Members.FirstOrDefault(m => m.UserId == currentUserId)?.Role;
            if (myRole != "Leader")
            {
                TempData["ErrorMessage"] = "Chỉ Trưởng nhóm mới có quyền xóa thành viên!";
                return RedirectToAction("Details", new { id = groupId });
            }

            // Không được tự xóa chính mình
            if (userId == currentUserId)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự xóa mình khỏi nhóm!";
                return RedirectToAction("Details", new { id = groupId });
            }

            var member = group.Members.FirstOrDefault(m => m.UserId == userId);
            if (member != null)
            {
                group.Members.Remove(member);
                _groupRepository.Update(group);
                _groupRepository.Save();
                TempData["SuccessMessage"] = "Đã xóa thành viên khỏi nhóm!";
            }

            return RedirectToAction("Details", new { id = groupId });
        }

        // ── XÓA NHÓM ──────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);
            var group = _groupRepository.GetById(id);
            if (group == null) return NotFound();

            // Chỉ Leader mới được xóa nhóm
            var myRole = group.Members.FirstOrDefault(m => m.UserId == currentUserId)?.Role;
            if (myRole != "Leader")
            {
                TempData["ErrorMessage"] = "Chỉ Trưởng nhóm mới có quyền xóa nhóm!";
                return RedirectToAction("Index");
            }

            _groupRepository.Delete(id);
            _groupRepository.Save();

            TempData["SuccessMessage"] = $"Đã xóa nhóm \"{group.Name}\"!";
            return RedirectToAction("Index");
        }
    }
}