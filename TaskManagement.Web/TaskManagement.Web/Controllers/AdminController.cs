using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Web.Models;
using TaskManagement.Web.Repositories;

namespace TaskManagement.Web.Controllers
{
    [Authorize(Roles = "Admin,SystemAdmin")]
    public class AdminController : Controller
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISubmissionRepository _submissionRepository;

        public AdminController(
            ITaskRepository taskRepository,
            IUserRepository userRepository,
            ISubmissionRepository submissionRepository)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
            _submissionRepository = submissionRepository;
        }

        // ── DASHBOARD ─────────────────────────────────────────────────────
        public IActionResult Dashboard()
        {
            var tasks = _taskRepository.GetAll().ToList();
            var users = _userRepository.GetAll().ToList();
            var allSubmissions = tasks
                .SelectMany(t => _submissionRepository.GetByTaskId(t.TaskId))
                .ToList();

            ViewBag.TotalTasks = tasks.Count;
            ViewBag.TodoCount = tasks.Count(t => t.Status == "Todo");
            ViewBag.DoingCount = tasks.Count(t => t.Status == "Doing");
            ViewBag.DoneCount = tasks.Count(t => t.Status == "Done");
            ViewBag.OverdueCount = tasks.Count(t => t.Deadline < DateTime.Now && t.Status != "Done");
            ViewBag.TotalUsers = users.Count(u => u.Role == "User");
            ViewBag.PendingReviews = allSubmissions.Count(s => s.Status == "Pending");
            ViewBag.TotalSubmissions = allSubmissions.Count;

            // 5 task sắp hết hạn (chưa Done, deadline trong 3 ngày tới)
            ViewBag.UpcomingTasks = tasks
                .Where(t => t.Status != "Done" && t.Deadline >= DateTime.Now && t.Deadline <= DateTime.Now.AddDays(3))
                .OrderBy(t => t.Deadline)
                .Take(5)
                .ToList();

            var userDict = users.ToDictionary(u => u.UserId, u => u.FullName);
            ViewBag.UserDict = userDict;

            // 5 submissions chờ duyệt gần nhất
            ViewBag.PendingSubmissions = allSubmissions
                .Where(s => s.Status == "Pending")
                .OrderByDescending(s => s.SubmittedAt)
                .Take(5)
                .ToList();

            ViewBag.SubmissionTaskDict = tasks.ToDictionary(t => t.TaskId, t => t.Title);

            return View();
        }

        // ── DUYỆT SUBMISSIONS ─────────────────────────────────────────────
        public IActionResult Submissions(string filter = "pending")
        {
            var tasks = _taskRepository.GetAll().ToList();
            var users = _userRepository.GetAll().ToList();

            var allSubmissions = tasks
                .SelectMany(t => _submissionRepository.GetByTaskId(t.TaskId))
                .ToList();

            var filtered = filter switch
            {
                "approved" => allSubmissions.Where(s => s.Status == "Approved").ToList(),
                "needsrevision" => allSubmissions.Where(s => s.Status == "NeedsRevision").ToList(),
                _ => allSubmissions.Where(s => s.Status == "Pending").ToList()
            };

            filtered = filtered.OrderByDescending(s => s.SubmittedAt).ToList();

            ViewBag.Filter = filter;
            ViewBag.PendingCount = allSubmissions.Count(s => s.Status == "Pending");
            ViewBag.ApprovedCount = allSubmissions.Count(s => s.Status == "Approved");
            ViewBag.NeedsRevisionCount = allSubmissions.Count(s => s.Status == "NeedsRevision");
            ViewBag.TaskDict = tasks.ToDictionary(t => t.TaskId, t => t.Title);
            ViewBag.UserDict = users.ToDictionary(u => u.UserId, u => u.FullName);

            return View(filtered);
        }

        // ── QUẢN LÝ USER ──────────────────────────────────────────────────
        public IActionResult Users()
        {
            var users = _userRepository.GetAll().ToList();
            var tasks = _taskRepository.GetAll().ToList();

            var taskCountDict = tasks
                .Where(t => t.AssignedTo.HasValue)
                .GroupBy(t => t.AssignedTo!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.TaskCountDict = taskCountDict;
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(string email, string password, string fullName, string role)
        {
            if (_userRepository.GetAll().Any(u => u.Email == email))
            {
                TempData["ErrorMessage"] = "Email đã tồn tại!";
                return RedirectToAction(nameof(Users));
            }

            var user = new User
            {
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = fullName,
                Role = role
            };

            _userRepository.Add(user);
            _userRepository.Save();

            TempData["SuccessMessage"] = $"Đã tạo tài khoản {fullName} thành công!";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(int userId, string fullName, string role, string? newPassword)
        {
            var user = _userRepository.GetById(userId);
            if (user == null) return NotFound();

            user.FullName = fullName;
            user.Role = role;
            if (!string.IsNullOrWhiteSpace(newPassword))
                user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);

            _userRepository.Update(user);
            _userRepository.Save();

            TempData["SuccessMessage"] = $"Đã cập nhật tài khoản {fullName}!";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);
            if (userId == currentUserId)
            {
                TempData["ErrorMessage"] = "Không thể xóa tài khoản đang đăng nhập!";
                return RedirectToAction(nameof(Users));
            }

            var user = _userRepository.GetById(userId);
            if (user == null) return NotFound();

            _userRepository.Delete(userId);
            _userRepository.Save();

            TempData["SuccessMessage"] = "Đã xóa tài khoản thành công!";
            return RedirectToAction(nameof(Users));
        }
    }
}