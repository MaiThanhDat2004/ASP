using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskManagement.Web.Models;
using TaskManagement.Web.Repositories;

namespace TaskManagement.Web.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly INotificationRepository _notificationRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly ITaskAssigneeRepository _taskAssigneeRepository;
        private readonly ITaskOutputRepository _taskOutputRepository;
        private readonly ITaskCommentRepository _commentRepository;

        public TasksController(
            ITaskRepository taskRepository,
            IUserRepository userRepository,
            ISubmissionRepository submissionRepository,
            IWebHostEnvironment webHostEnvironment,
            INotificationRepository notificationRepository,
            IGroupRepository groupRepository,
            ITaskAssigneeRepository taskAssigneeRepository,
            ITaskOutputRepository taskOutputRepository,
            ITaskCommentRepository commentRepository)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
            _submissionRepository = submissionRepository;
            _webHostEnvironment = webHostEnvironment;
            _notificationRepository = notificationRepository;
            _groupRepository = groupRepository;
            _taskAssigneeRepository = taskAssigneeRepository;
            _taskOutputRepository = taskOutputRepository;
            _commentRepository = commentRepository;
        }

        // ── HELPERS ────────────────────────────────────────────────────────
        private bool IsLeaderOfTask(TaskItem task, int userId)
        {
            if (task.GroupId == null) return User.IsInRole("SystemAdmin");
            var group = _groupRepository.GetById(task.GroupId.Value);
            if (group == null) return false;
            return group.Members.Any(m => m.UserId == userId && m.Role == "Leader");
        }

        private bool IsAssignedToTask(int taskId, int userId)
        {
            return _taskAssigneeRepository.IsAssigned(taskId, userId)
                || _taskRepository.GetById(taskId)?.AssignedTo == userId;
        }

        // Kiểm tra user có thể xem chat của output không
        // (Leader + người thực hiện chính + người hỗ trợ)
        private bool CanViewOutputChat(TaskOutputRequirement req, int userId, TaskItem task)
        {
            if (IsLeaderOfTask(task, userId) ||
                User.IsInRole("SystemAdmin") || User.IsInRole("Admin"))
                return true;

            if (req.PrimaryAssigneeId == userId) return true;

            if (!string.IsNullOrWhiteSpace(req.SupportAssigneeIds))
            {
                var supportIds = req.SupportAssigneeIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0);
                if (supportIds.Contains(userId)) return true;
            }

            return false;
        }

        private List<GroupMember> GetLeadersOfTask(TaskItem task)
        {
            if (task.GroupId == null) return new List<GroupMember>();
            var group = _groupRepository.GetById(task.GroupId.Value);
            if (group == null) return new List<GroupMember>();
            return group.Members.Where(m => m.Role == "Leader").ToList();
        }

        // ── INDEX ──────────────────────────────────────────────────────────
        public IActionResult Index(string? searchString, string? statusFilter,
            string? priorityFilter, int page = 1, int pageSize = 5)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 5;

            var tasksQuery = _taskRepository.GetAll()
                .OrderByDescending(t => t.CreatedAt)
                .AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchString))
                tasksQuery = tasksQuery.Where(t =>
                    (!string.IsNullOrWhiteSpace(t.Title) && t.Title.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(t.Description) && t.Description.Contains(searchString, StringComparison.OrdinalIgnoreCase)));

            if (!string.IsNullOrWhiteSpace(statusFilter))
                tasksQuery = tasksQuery.Where(t => t.Status == statusFilter);

            if (!string.IsNullOrWhiteSpace(priorityFilter))
                tasksQuery = tasksQuery.Where(t => t.Priority == priorityFilter);

            var filteredTasks = tasksQuery.ToList();
            var totalItems = filteredTasks.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var pagedTasks = filteredTasks
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);
            var userDict = _userRepository.GetAll().ToDictionary(u => u.UserId, u => u.FullName);

            ViewBag.CreatorNames = pagedTasks
                .Select(t => t.CreatedBy).Distinct()
                .ToDictionary(uid => uid, uid => userDict.ContainsKey(uid) ? userDict[uid] : "?");

            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.PriorityFilter = priorityFilter;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;
            ViewBag.Users = userDict;
            ViewBag.CurrentUserId = currentUserId;

            return View(pagedTasks);
        }

        // ── MY DASHBOARD ───────────────────────────────────────────────────
        public IActionResult MyDashboard()
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var now = DateTime.Now;

            var assignedTaskIds = _taskAssigneeRepository.GetByUserId(userId)
                .Select(ta => ta.TaskId).ToList();

            var allMyTasks = _taskRepository.GetAll()
                .Where(t => assignedTaskIds.Contains(t.TaskId) || t.AssignedTo == userId)
                .ToList();

            // Lấy số sản phẩm phải nộp cho từng task
            var outputCountDict = allMyTasks.ToDictionary(
                t => t.TaskId,
                t => _taskOutputRepository.GetRequirementsByTaskId(t.TaskId).Count()
            );

            var userDict = _userRepository.GetAll().ToDictionary(u => u.UserId, u => u.FullName);

            // Phân loại
            var todoTasks = allMyTasks.Where(t => t.Status == "Todo").OrderBy(t => t.Deadline).ToList();
            var doingTasks = allMyTasks.Where(t => t.Status == "Doing").OrderBy(t => t.Deadline).ToList();
            var upcomingTasks = allMyTasks.Where(t => t.Status != "Done" && t.Status != "Overdue"
                                                    && t.Deadline >= now
                                                    && t.Deadline <= now.AddDays(3)).OrderBy(t => t.Deadline).ToList();
            var overdueTasks = allMyTasks.Where(t => t.Status != "Done" && t.Status != "Overdue"
                                                    && t.Deadline < now).OrderBy(t => t.Deadline).ToList();
            var markedOverdue = allMyTasks.Where(t => t.Status == "Overdue").OrderByDescending(t => t.OverdueMarkedAt).ToList();
            var doneTasks = allMyTasks.Where(t => t.Status == "Done").OrderByDescending(t => t.Deadline).ToList();

            ViewBag.TodoTasks = todoTasks;
            ViewBag.DoingTasks = doingTasks;
            ViewBag.UpcomingTasks = upcomingTasks;
            ViewBag.OverdueTasks = overdueTasks;
            ViewBag.MarkedOverdue = markedOverdue;
            ViewBag.DoneTasks = doneTasks;
            ViewBag.OutputCountDict = outputCountDict;
            ViewBag.UserDict = userDict;
            ViewBag.CurrentUserId = userId;
            ViewBag.Now = now;

            return View();
        }

        // ── MY TASKS (giữ lại tương thích) ────────────────────────────────
        public IActionResult MyTasks(int page = 1, int pageSize = 5)
        {
            return RedirectToAction(nameof(MyDashboard));
        }

        // ── CALENDAR ───────────────────────────────────────────────────────
        public IActionResult Calendar()
        {
            var tasks = _taskRepository.GetAll().ToList();
            var userDict = _userRepository.GetAll().ToDictionary(u => u.UserId, u => u.FullName ?? "");
            ViewBag.Tasks = tasks;
            ViewBag.UserDict = userDict;
            return View();
        }

        // ── DETAILS ────────────────────────────────────────────────────────
        public IActionResult Details(int id)
        {
            var task = _taskRepository.GetById(id);
            if (task == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);

            var assignees = _taskAssigneeRepository.GetByTaskId(id).ToList();
            ViewBag.Assignees = assignees;
            ViewBag.AssignedUser = task.AssignedTo.HasValue
                ? _userRepository.GetById(task.AssignedTo.Value)?.FullName : null;
            ViewBag.CreatedUser = _userRepository.GetById(task.CreatedBy)?.FullName;

            var submissions = _submissionRepository.GetByTaskId(id).ToList();
            ViewBag.Submissions = submissions;

            var submitterNames = submissions
                .Select(s => s.SubmittedBy).Distinct()
                .ToDictionary(uid => uid, uid => _userRepository.GetById(uid)?.FullName ?? "?");
            ViewBag.SubmitterNames = submitterNames;

            bool isAssigned = IsAssignedToTask(id, currentUserId);
            bool isLeader = IsLeaderOfTask(task, currentUserId);
            bool isAdmin = User.IsInRole("SystemAdmin") || User.IsInRole("Admin");

            ViewBag.IsAssignedUser = isAssigned;
            ViewBag.IsLeader = isLeader;
            ViewBag.CurrentUserId = currentUserId;
            ViewBag.CanEdit = (isLeader || isAdmin) && task.Status == "Todo";
            ViewBag.CanDelete = (isLeader || isAdmin) && task.Status == "Todo" && !submissions.Any();

            // Output requirements
            var outputReqs = _taskOutputRepository.GetRequirementsByTaskId(id).ToList();
            ViewBag.OutputRequirements = outputReqs;

            var reviewerIds = outputReqs
                .SelectMany(r => r.Submissions)
                .Where(s => s.ReviewedBy.HasValue)
                .Select(s => s.ReviewedBy!.Value)
                .Distinct().ToList();
            ViewBag.ReviewerNames = reviewerIds.ToDictionary(
                uid => uid,
                uid => _userRepository.GetById(uid)?.FullName ?? "?"
            );

            // Danh sách user để Leader chọn khi thêm output
            var allUsers = _userRepository.GetAll().ToList();
            ViewBag.AllUsers = allUsers;

            // Kiểm tra user có trong nhóm chứa task không
            bool isGroupMember = false;
            if (task.GroupId.HasValue)
            {
                var group = _groupRepository.GetById(task.GroupId.Value);
                isGroupMember = group?.Members.Any(m => m.UserId == currentUserId) ?? false;
            }
            ViewBag.IsGroupMember = isGroupMember;

            // Chat chung của task
            var taskComments = _commentRepository.GetByTaskId(id).ToList();
            ViewBag.TaskComments = taskComments;

            // Chat riêng từng output — chỉ trả về những output user được phép xem
            var outputChats = new Dictionary<int, List<TaskComment>>();
            foreach (var req in outputReqs)
            {
                if (CanViewOutputChat(req, currentUserId, task))
                {
                    outputChats[req.Id] = _commentRepository
                        .GetByOutputRequirementId(req.Id).ToList();
                }
            }
            ViewBag.OutputChats = outputChats;

            // Quyền xem từng output chat
            var canViewOutputChat = outputReqs.ToDictionary(
                r => r.Id,
                r => CanViewOutputChat(r, currentUserId, task)
            );
            ViewBag.CanViewOutputChat = canViewOutputChat;

            return View(task);
        }

        // ── GỬI CHAT CHUNG (Task) ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendTaskComment(int taskId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập nội dung tin nhắn!";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            var task = _taskRepository.GetById(taskId);
            if (task == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);

            // Thành viên trong nhóm hoặc assignee hoặc leader đều được chat chung
            var taskGroup = task.GroupId.HasValue ? _groupRepository.GetById(task.GroupId.Value) : null;
            bool isGroupMemberChat = taskGroup?.Members.Any(m => m.UserId == currentUserId) ?? false;

            if (!IsAssignedToTask(taskId, currentUserId) &&
                !IsLeaderOfTask(task, currentUserId) &&
                !isGroupMemberChat &&
                !User.IsInRole("SystemAdmin") && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền tham gia chat này!";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            _commentRepository.Add(new TaskComment
            {
                TaskId = taskId,
                SenderId = currentUserId,
                Message = message.Trim(),
                SentAt = DateTime.Now
            });
            _commentRepository.Save();

            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        // ── GỬI CHAT OUTPUT (riêng từng sản phẩm) ─────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendOutputComment(int requirementId, int taskId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập nội dung tin nhắn!";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            var req = _taskOutputRepository.GetRequirementById(requirementId);
            var task = _taskRepository.GetById(taskId);
            if (req == null || task == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);

            if (!CanViewOutputChat(req, currentUserId, task))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chat trong sản phẩm này!";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            _commentRepository.Add(new TaskComment
            {
                OutputRequirementId = requirementId,
                TaskId = taskId,
                SenderId = currentUserId,
                Message = message.Trim(),
                SentAt = DateTime.Now
            });
            _commentRepository.Save();

            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        // ── BẮT ĐẦU LÀM (Todo → Doing) ────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StartTask(int id)
        {
            var task = _taskRepository.GetById(id);
            if (task == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);

            if (!IsAssignedToTask(id, currentUserId))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền bắt đầu công việc này!";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (task.Status != "Todo")
            {
                TempData["ErrorMessage"] = "Công việc không ở trạng thái Todo!";
                return RedirectToAction(nameof(Details), new { id });
            }

            task.Status = "Doing";
            _taskRepository.Update(task);
            _taskRepository.Save();

            TempData["SuccessMessage"] = "Đã bắt đầu công việc!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── UPLOAD SUBMISSION ──────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadSubmission(int taskId, IFormFile file, string? note)
        {
            try
            {
                var task = _taskRepository.GetById(taskId);
                if (task == null) return NotFound();

                var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);

                if (!IsAssignedToTask(taskId, currentUserId))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền nộp kết quả cho công việc này.";
                    return RedirectToAction(nameof(Details), new { id = taskId });
                }

                if (file == null || file.Length == 0)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn file để nộp.";
                    return RedirectToAction(nameof(Details), new { id = taskId });
                }

                if (file.Length > 20 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "File không được vượt quá 20MB.";
                    return RedirectToAction(nameof(Details), new { id = taskId });
                }

                var webRoot = _webHostEnvironment.WebRootPath
                              ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadFolder = Path.Combine(webRoot, "uploads", "tasks", taskId.ToString());
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                var uniqueFileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(stream);

                var prevSubmissions = _submissionRepository.GetByTaskId(taskId).ToList();
                int round = prevSubmissions.Count + 1;

                var submission = new TaskSubmission
                {
                    TaskId = taskId,
                    SubmittedBy = currentUserId,
                    FileName = file.FileName,
                    FilePath = $"/uploads/tasks/{taskId}/{uniqueFileName}",
                    Note = note,
                    SubmittedAt = DateTime.Now,
                    Status = "Pending",
                    IsApproved = null,
                    SubmissionRound = round
                };

                _submissionRepository.Add(submission);
                _submissionRepository.Save();

                foreach (var leader in GetLeadersOfTask(task))
                {
                    _notificationRepository.Add(new Notification
                    {
                        UserId = leader.UserId,
                        Message = $"Có bài nộp mới (lần {round}) cho \"{task.Title}\" đang chờ duyệt",
                        Type = "submission_uploaded",
                        Link = $"/Tasks/Details/{taskId}",
                        CreatedAt = DateTime.Now
                    });
                }
                _notificationRepository.Save();

                TempData["SuccessMessage"] = $"Nộp kết quả lần {round} thành công!";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction(nameof(Details), new { id = taskId });
            }
        }

        // ── REVIEW SUBMISSION ──────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReviewSubmission(int submissionId, string reviewResult, string? reviewComment)
        {
            var submission = _submissionRepository.GetById(submissionId);
            if (submission == null) return NotFound();

            var task = _taskRepository.GetById(submission.TaskId);
            if (task == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);

            if (!IsLeaderOfTask(task, currentUserId) &&
                !User.IsInRole("SystemAdmin") && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền duyệt bài nộp này!";
                return RedirectToAction(nameof(Details), new { id = task.TaskId });
            }

            submission.ReviewComment = reviewComment;
            submission.AdminComment = reviewComment;
            submission.ReviewedAt = DateTime.Now;
            submission.ReviewedBy = currentUserId;

            string notifyMessage;
            if (reviewResult == "Approved")
            {
                submission.Status = "Approved";
                submission.IsApproved = true;
                task.Status = "Done";
                _taskRepository.Update(task);
                _taskRepository.Save();
                notifyMessage = $"✅ Bài nộp của bạn cho \"{task.Title}\" đã được duyệt: Đạt!";
                TempData["SuccessMessage"] = "Đã duyệt Đạt. Task chuyển sang Done.";
            }
            else
            {
                submission.Status = "NeedsRevision";
                submission.IsApproved = false;
                if (task.Status == "Done") task.Status = "Doing";
                _taskRepository.Update(task);
                _taskRepository.Save();
                notifyMessage = $"❌ Bài nộp cho \"{task.Title}\" cần chỉnh sửa. Xem nhận xét và nộp lại!";
                TempData["SuccessMessage"] = "Đã yêu cầu chỉnh sửa.";
            }

            _submissionRepository.Update(submission);
            _submissionRepository.Save();

            _notificationRepository.Add(new Notification
            {
                UserId = submission.SubmittedBy,
                Message = notifyMessage,
                Type = "submission_reviewed",
                Link = $"/Tasks/Details/{task.TaskId}",
                CreatedAt = DateTime.Now
            });
            _notificationRepository.Save();

            return RedirectToAction(nameof(Details), new { id = task.TaskId });
        }

        // ── THÊM SẢN PHẨM YÊU CẦU (Leader) ───────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddOutputRequirement(
            int taskId, string name, string? description,
            string? allowedFileFormat, DateTime? deadline,
            int? primaryAssigneeId, string? supportAssigneeIds,
            bool isRequired = true)
        {
            var task = _taskRepository.GetById(taskId);
            if (task == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);
            if (!IsLeaderOfTask(task, currentUserId) &&
                !User.IsInRole("SystemAdmin") && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Chỉ Trưởng nhóm mới có thể thêm sản phẩm yêu cầu!";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập tên sản phẩm!";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            // Xử lý supportAssigneeIds: nhận dạng "1,2,3" từ multi-select
            var supportIds = string.IsNullOrWhiteSpace(supportAssigneeIds)
                ? null
                : string.Join(",", supportAssigneeIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s != primaryAssigneeId.ToString()));

            _taskOutputRepository.AddRequirement(new TaskOutputRequirement
            {
                TaskId = taskId,
                Name = name,
                Description = description,
                AllowedFileFormat = allowedFileFormat,
                Deadline = deadline,
                PrimaryAssigneeId = primaryAssigneeId,
                SupportAssigneeIds = supportIds,
                IsRequired = isRequired,
                SortOrder = _taskOutputRepository.GetRequirementsByTaskId(taskId).Count()
            });
            _taskOutputRepository.Save();

            // Thông báo cho người thực hiện chính
            if (primaryAssigneeId.HasValue)
            {
                _notificationRepository.Add(new Notification
                {
                    UserId = primaryAssigneeId.Value,
                    Message = $"Bạn được chỉ định thực hiện chính sản phẩm \"{name}\" trong công việc \"{task.Title}\"",
                    Type = "task_assigned",
                    Link = $"/Tasks/Details/{taskId}",
                    CreatedAt = DateTime.Now
                });
            }

            // Thông báo cho người hỗ trợ
            if (!string.IsNullOrWhiteSpace(supportIds))
            {
                var supportIdList = supportIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out var sid) ? sid : 0)
                    .Where(sid => sid > 0)
                    .ToList();

                foreach (var sid in supportIdList)
                {
                    _notificationRepository.Add(new Notification
                    {
                        UserId = sid,
                        Message = $"Bạn được chỉ định hỗ trợ sản phẩm \"{name}\" trong công việc \"{task.Title}\"",
                        Type = "task_assigned",
                        Link = $"/Tasks/Details/{taskId}",
                        CreatedAt = DateTime.Now
                    });
                }
            }

            _notificationRepository.Save();

            TempData["SuccessMessage"] = $"Đã thêm sản phẩm yêu cầu: {name}";
            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        // ── XÓA SẢN PHẨM YÊU CẦU ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteOutputRequirement(int requirementId, int taskId)
        {
            var task = _taskRepository.GetById(taskId);
            if (task == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);
            if (!IsLeaderOfTask(task, currentUserId) &&
                !User.IsInRole("SystemAdmin") && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Không có quyền xóa!";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            _taskOutputRepository.DeleteRequirement(requirementId);
            _taskOutputRepository.Save();

            TempData["SuccessMessage"] = "Đã xóa sản phẩm yêu cầu!";
            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        // ── NỘP SẢN PHẨM ĐẦU RA ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadOutput(int requirementId, IFormFile file, string? note)
        {
            var req = _taskOutputRepository.GetRequirementById(requirementId);
            if (req == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);

            // Cho phép người thực hiện chính hoặc người hỗ trợ nộp
            var task = _taskRepository.GetById(req.TaskId);
            if (task == null) return NotFound();

            bool canSubmit = IsAssignedToTask(req.TaskId, currentUserId)
                          || req.PrimaryAssigneeId == currentUserId
                          || CanViewOutputChat(req, currentUserId, task);

            if (!canSubmit)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền nộp sản phẩm này!";
                return RedirectToAction(nameof(Details), new { id = req.TaskId });
            }

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn file!";
                return RedirectToAction(nameof(Details), new { id = req.TaskId });
            }

            // Kiểm tra định dạng file nếu có yêu cầu
            if (!string.IsNullOrWhiteSpace(req.AllowedFileFormat) && req.AllowedFileFormat != "any")
            {
                var ext = Path.GetExtension(file.FileName).ToLower();
                var allowed = req.AllowedFileFormat.Split(',')
                    .Select(f => f.Trim().ToLower())
                    .ToList();
                if (!allowed.Contains(ext))
                {
                    TempData["ErrorMessage"] = $"File phải có định dạng: {req.AllowedFileFormat}";
                    return RedirectToAction(nameof(Details), new { id = req.TaskId });
                }
            }

            var webRoot = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folder = Path.Combine(webRoot, "uploads", "outputs", req.TaskId.ToString());
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var uniqueName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(folder, uniqueName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            _taskOutputRepository.AddSubmission(new TaskOutputSubmission
            {
                RequirementId = requirementId,
                SubmittedBy = currentUserId,
                FileName = file.FileName,
                FilePath = $"/uploads/outputs/{req.TaskId}/{uniqueName}",
                Note = note,
                Status = "Pending",
                SubmittedAt = DateTime.Now
            });
            _taskOutputRepository.Save();

            foreach (var leader in GetLeadersOfTask(task))
            {
                _notificationRepository.Add(new Notification
                {
                    UserId = leader.UserId,
                    Message = $"Sản phẩm \"{req.Name}\" đã được nộp — task \"{task.Title}\"",
                    Type = "output_submitted",
                    Link = $"/Tasks/Details/{task.TaskId}",
                    CreatedAt = DateTime.Now
                });
            }
            _notificationRepository.Save();

            TempData["SuccessMessage"] = "Nộp sản phẩm thành công!";
            return RedirectToAction(nameof(Details), new { id = req.TaskId });
        }

        // ── ĐÁNH KHÔNG ĐẠT TIẾN ĐỘ OUTPUT (Leader) ───────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkOutputOverdue(int requirementId, int taskId, string? reason)
        {
            var req = _taskOutputRepository.GetRequirementById(requirementId);
            var task = _taskRepository.GetById(taskId);
            if (req == null || task == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);
            if (!IsLeaderOfTask(task, currentUserId) &&
                !User.IsInRole("SystemAdmin") && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện thao tác này!";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            // Không đánh không đạt nếu đã được duyệt
            if (req.Submissions.Any(s => s.Status == "Approved"))
            {
                TempData["ErrorMessage"] = "Sản phẩm đã được duyệt đạt, không thể đánh không đạt tiến độ!";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            // Không đánh không đạt nếu chưa có deadline
            if (!req.Deadline.HasValue)
            {
                TempData["ErrorMessage"] = "Sản phẩm này chưa có deadline, không thể đánh không đạt tiến độ!";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            req.IsOverdue = true;
            req.OverdueMarkedAt = DateTime.Now;
            req.OverdueReason = reason;
            if (!req.OriginalDeadline.HasValue && req.Deadline.HasValue)
                req.OriginalDeadline = req.Deadline;

            _taskOutputRepository.UpdateRequirement(req);
            _taskOutputRepository.Save();

            // Thông báo người thực hiện chính
            if (req.PrimaryAssigneeId.HasValue)
            {
                _notificationRepository.Add(new Notification
                {
                    UserId = req.PrimaryAssigneeId.Value,
                    Message = $"⚠️ Sản phẩm \"{req.Name}\" trong \"{task.Title}\" bị đánh không đạt tiến độ!" +
                                (string.IsNullOrWhiteSpace(reason) ? "" : $" Lý do: {reason}"),
                    Type = "task_overdue",
                    Link = $"/Tasks/Details/{taskId}",
                    CreatedAt = DateTime.Now
                });
            }

            // Thông báo người hỗ trợ
            if (!string.IsNullOrWhiteSpace(req.SupportAssigneeIds))
            {
                var supportIds = req.SupportAssigneeIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out var sid) ? sid : 0)
                    .Where(sid => sid > 0);
                foreach (var sid in supportIds)
                {
                    _notificationRepository.Add(new Notification
                    {
                        UserId = sid,
                        Message = $"⚠️ Sản phẩm \"{req.Name}\" trong \"{task.Title}\" bị đánh không đạt tiến độ!",
                        Type = "task_overdue",
                        Link = $"/Tasks/Details/{taskId}",
                        CreatedAt = DateTime.Now
                    });
                }
            }
            _notificationRepository.Save();

            TempData["SuccessMessage"] = $"Đã đánh không đạt tiến độ sản phẩm \"{req.Name}\"!";
            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        // ── GIA HẠN OUTPUT (Leader) ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExtendOutputDeadline(int requirementId, int taskId,
            DateTime newDeadline, string? extendReason)
        {
            var req = _taskOutputRepository.GetRequirementById(requirementId);
            var task = _taskRepository.GetById(taskId);
            if (req == null || task == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);
            if (!IsLeaderOfTask(task, currentUserId) &&
                !User.IsInRole("SystemAdmin") && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền gia hạn!";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            // Deadline mới phải sau thời điểm hiện tại
            if (newDeadline <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Deadline mới phải lớn hơn thời điểm hiện tại!";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            // Deadline mới phải sau deadline cũ (nếu có)
            if (req.Deadline.HasValue && newDeadline <= req.Deadline.Value)
            {
                TempData["ErrorMessage"] = $"Deadline mới phải sau deadline hiện tại ({req.Deadline.Value:dd/MM/yyyy HH:mm})!";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            // Không gia hạn nếu đã được duyệt
            if (req.Submissions.Any(s => s.Status == "Approved"))
            {
                TempData["ErrorMessage"] = "Sản phẩm đã được duyệt đạt, không cần gia hạn!";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            if (!req.OriginalDeadline.HasValue && req.Deadline.HasValue)
                req.OriginalDeadline = req.Deadline;

            req.Deadline = newDeadline;
            req.ExtendCount += 1;
            req.IsOverdue = false; // Khôi phục nếu đang Overdue

            _taskOutputRepository.UpdateRequirement(req);
            _taskOutputRepository.Save();

            // Thông báo người thực hiện chính
            if (req.PrimaryAssigneeId.HasValue)
            {
                _notificationRepository.Add(new Notification
                {
                    UserId = req.PrimaryAssigneeId.Value,
                    Message = $"📅 Sản phẩm \"{req.Name}\" được gia hạn đến {newDeadline:dd/MM/yyyy HH:mm}" +
                                (string.IsNullOrWhiteSpace(extendReason) ? "" : $". Lý do: {extendReason}"),
                    Type = "task_extended",
                    Link = $"/Tasks/Details/{taskId}",
                    CreatedAt = DateTime.Now
                });
            }

            // Thông báo người hỗ trợ
            if (!string.IsNullOrWhiteSpace(req.SupportAssigneeIds))
            {
                var supportIds = req.SupportAssigneeIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out var sid) ? sid : 0)
                    .Where(sid => sid > 0);
                foreach (var sid in supportIds)
                {
                    _notificationRepository.Add(new Notification
                    {
                        UserId = sid,
                        Message = $"📅 Sản phẩm \"{req.Name}\" được gia hạn đến {newDeadline:dd/MM/yyyy HH:mm}",
                        Type = "task_extended",
                        Link = $"/Tasks/Details/{taskId}",
                        CreatedAt = DateTime.Now
                    });
                }
            }
            _notificationRepository.Save();

            TempData["SuccessMessage"] = $"Đã gia hạn sản phẩm \"{req.Name}\" đến {newDeadline:dd/MM/yyyy HH:mm}!";
            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        // ── DUYỆT SẢN PHẨM ĐẦU RA ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReviewOutput(int submissionId, string reviewResult, string? reviewComment)
        {
            var sub = _taskOutputRepository.GetSubmissionById(submissionId);
            if (sub == null) return NotFound();

            var req = _taskOutputRepository.GetRequirementById(sub.RequirementId);
            if (req == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);
            var task = _taskRepository.GetById(req.TaskId);
            if (task == null) return NotFound();

            if (!IsLeaderOfTask(task, currentUserId) &&
                !User.IsInRole("SystemAdmin") && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền duyệt!";
                return RedirectToAction(nameof(Details), new { id = req.TaskId });
            }

            sub.Status = reviewResult == "Approved" ? "Approved" : "NeedsRevision";
            sub.ReviewComment = reviewComment;
            sub.ReviewedBy = currentUserId;
            sub.ReviewedAt = DateTime.Now;
            _taskOutputRepository.UpdateSubmission(sub);
            _taskOutputRepository.Save();

            if (reviewResult == "Approved" && _taskOutputRepository.IsAllOutputsApproved(req.TaskId))
            {
                task.Status = "Done";
                _taskRepository.Update(task);
                _taskRepository.Save();
                TempData["SuccessMessage"] = "✅ Tất cả sản phẩm đã đạt! Task chuyển sang Done.";
            }
            else
            {
                TempData["SuccessMessage"] = reviewResult == "Approved"
                    ? "Đã duyệt sản phẩm này đạt!"
                    : "Đã yêu cầu chỉnh sửa sản phẩm này!";
            }

            _notificationRepository.Add(new Notification
            {
                UserId = sub.SubmittedBy,
                Message = reviewResult == "Approved"
                    ? $"✅ Sản phẩm \"{req.Name}\" đã được duyệt đạt!"
                    : $"❌ Sản phẩm \"{req.Name}\" cần chỉnh sửa!",
                Type = "output_reviewed",
                Link = $"/Tasks/Details/{req.TaskId}",
                CreatedAt = DateTime.Now
            });
            _notificationRepository.Save();

            return RedirectToAction(nameof(Details), new { id = req.TaskId });
        }

        // ── ĐÁNH KHÔNG ĐẠT TIẾN ĐỘ (Leader) ──────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkOverdue(int id, string? reason)
        {
            var task = _taskRepository.GetById(id);
            if (task == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);

            if (!IsLeaderOfTask(task, currentUserId) &&
                !User.IsInRole("SystemAdmin") && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện thao tác này!";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (task.Status == "Done")
            {
                TempData["ErrorMessage"] = "Công việc đã hoàn thành, không thể đánh không đạt tiến độ!";
                return RedirectToAction(nameof(Details), new { id });
            }

            task.Status = "Overdue";
            task.OverdueMarkedAt = DateTime.Now;
            task.OverdueMarkedBy = currentUserId;
            task.OverdueReason = reason;

            // Lưu deadline gốc nếu chưa có
            if (!task.OriginalDeadline.HasValue)
                task.OriginalDeadline = task.Deadline;

            _taskRepository.Update(task);
            _taskRepository.Save();

            // Thông báo cho tất cả người được giao
            var assignees = _taskAssigneeRepository.GetByTaskId(id).ToList();
            foreach (var a in assignees)
            {
                _notificationRepository.Add(new Notification
                {
                    UserId = a.UserId,
                    Message = $"⚠️ Công việc \"{task.Title}\" bị đánh không đạt tiến độ!" +
                                (string.IsNullOrWhiteSpace(reason) ? "" : $" Lý do: {reason}"),
                    Type = "task_overdue",
                    Link = $"/Tasks/Details/{id}",
                    CreatedAt = DateTime.Now
                });
            }
            // Tương thích AssignedTo cũ
            if (task.AssignedTo.HasValue)
            {
                _notificationRepository.Add(new Notification
                {
                    UserId = task.AssignedTo.Value,
                    Message = $"⚠️ Công việc \"{task.Title}\" bị đánh không đạt tiến độ!" +
                                (string.IsNullOrWhiteSpace(reason) ? "" : $" Lý do: {reason}"),
                    Type = "task_overdue",
                    Link = $"/Tasks/Details/{id}",
                    CreatedAt = DateTime.Now
                });
            }
            _notificationRepository.Save();

            TempData["SuccessMessage"] = "Đã đánh không đạt tiến độ. Người thực hiện đã được thông báo.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── GIA HẠN DEADLINE (Leader) ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExtendDeadline(int id, DateTime newDeadline, string? extendReason)
        {
            var task = _taskRepository.GetById(id);
            if (task == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);

            if (!IsLeaderOfTask(task, currentUserId) &&
                !User.IsInRole("SystemAdmin") && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền gia hạn công việc này!";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (newDeadline <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Deadline mới phải lớn hơn thời điểm hiện tại!";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Lưu deadline gốc nếu chưa có
            if (!task.OriginalDeadline.HasValue)
                task.OriginalDeadline = task.Deadline;

            task.Deadline = newDeadline;
            task.ExtendCount += 1;

            // Nếu đang Overdue thì chuyển về Doing
            if (task.Status == "Overdue")
                task.Status = "Doing";

            _taskRepository.Update(task);
            _taskRepository.Save();

            // Thông báo cho tất cả người được giao
            var assignees = _taskAssigneeRepository.GetByTaskId(id).ToList();
            foreach (var a in assignees)
            {
                _notificationRepository.Add(new Notification
                {
                    UserId = a.UserId,
                    Message = $"📅 Công việc \"{task.Title}\" đã được gia hạn đến {newDeadline:dd/MM/yyyy HH:mm}" +
                                (string.IsNullOrWhiteSpace(extendReason) ? "" : $". Lý do: {extendReason}"),
                    Type = "task_extended",
                    Link = $"/Tasks/Details/{id}",
                    CreatedAt = DateTime.Now
                });
            }
            if (task.AssignedTo.HasValue)
            {
                _notificationRepository.Add(new Notification
                {
                    UserId = task.AssignedTo.Value,
                    Message = $"📅 Công việc \"{task.Title}\" đã được gia hạn đến {newDeadline:dd/MM/yyyy HH:mm}",
                    Type = "task_extended",
                    Link = $"/Tasks/Details/{id}",
                    CreatedAt = DateTime.Now
                });
            }
            _notificationRepository.Save();

            TempData["SuccessMessage"] = $"Đã gia hạn deadline lần {task.ExtendCount}. Người thực hiện đã được thông báo.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── CREATE ─────────────────────────────────────────────────────────
        public IActionResult Create()
        {
            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);
            var myLeaderGroups = _groupRepository.GetByLeaderId(currentUserId).ToList();

            if (!myLeaderGroups.Any() && !User.IsInRole("SystemAdmin") && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn cần là Trưởng nhóm của ít nhất 1 nhóm để tạo công việc!";
                return RedirectToAction("Index", "Groups");
            }

            ViewBag.Groups = new SelectList(myLeaderGroups, "GroupId", "Name");
            ViewBag.Users = new SelectList(_userRepository.GetAll(), "UserId", "FullName");

            var model = new TaskItem
            {
                Deadline = DateTime.Now.AddDays(1),
                Status = "Todo",
                Priority = "Medium"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(TaskItem task, List<int> assigneeIds)
        {
            ModelState.Remove("GroupId");

            if (ModelState.IsValid)
            {
                var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);

                if (task.GroupId.HasValue)
                {
                    var group = _groupRepository.GetById(task.GroupId.Value);
                    var myRole = group?.Members.FirstOrDefault(m => m.UserId == currentUserId)?.Role;
                    if (myRole != "Leader" && !User.IsInRole("SystemAdmin") && !User.IsInRole("Admin"))
                    {
                        TempData["ErrorMessage"] = "Chỉ Trưởng nhóm mới có thể tạo công việc!";
                        return RedirectToAction(nameof(Index));
                    }
                }

                task.CreatedAt = DateTime.Now;
                task.CreatedBy = currentUserId;
                task.Status = "Todo";

                _taskRepository.Add(task);
                _taskRepository.Save();

                foreach (var uid in assigneeIds.Distinct())
                {
                    _taskAssigneeRepository.Add(new TaskAssignee
                    {
                        TaskId = task.TaskId,
                        UserId = uid,
                        AssignedBy = currentUserId,
                        AssignedAt = DateTime.Now
                    });

                    _notificationRepository.Add(new Notification
                    {
                        UserId = uid,
                        Message = $"Bạn được giao công việc mới: \"{task.Title}\"",
                        Type = "task_assigned",
                        Link = $"/Tasks/Details/{task.TaskId}",
                        CreatedAt = DateTime.Now
                    });
                }

                if (!assigneeIds.Any() && task.AssignedTo.HasValue)
                {
                    _notificationRepository.Add(new Notification
                    {
                        UserId = task.AssignedTo.Value,
                        Message = $"Bạn được giao công việc mới: \"{task.Title}\"",
                        Type = "task_assigned",
                        Link = $"/Tasks/Details/{task.TaskId}",
                        CreatedAt = DateTime.Now
                    });
                }

                _taskAssigneeRepository.Save();
                _notificationRepository.Save();

                TempData["SuccessMessage"] = "Thêm công việc thành công.";
                return RedirectToAction(nameof(Index));
            }

            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            ViewBag.Groups = new SelectList(_groupRepository.GetByLeaderId(userId), "GroupId", "Name");
            ViewBag.Users = new SelectList(_userRepository.GetAll(), "UserId", "FullName");
            return View(task);
        }

        // ── EDIT ───────────────────────────────────────────────────────────
        public IActionResult Edit(int id)
        {
            var task = _taskRepository.GetById(id);
            if (task == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);
            var submissions = _submissionRepository.GetByTaskId(id).ToList();

            if (task.Status != "Todo" || submissions.Any())
            {
                TempData["ErrorMessage"] = "Không thể sửa công việc đang thực hiện hoặc đã có báo cáo!";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!IsLeaderOfTask(task, currentUserId) &&
                !User.IsInRole("SystemAdmin") && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền sửa công việc này!";
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.Users = new SelectList(_userRepository.GetAll(), "UserId", "FullName", task.AssignedTo);
            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, TaskItem task)
        {
            if (id != task.TaskId) return NotFound();

            if (ModelState.IsValid)
            {
                var existingTask = _taskRepository.GetById(id);
                if (existingTask == null) return NotFound();

                if (existingTask.Status != "Todo")
                {
                    TempData["ErrorMessage"] = "Không thể sửa công việc đang thực hiện!";
                    return RedirectToAction(nameof(Details), new { id });
                }

                existingTask.Title = task.Title;
                existingTask.Description = task.Description;
                existingTask.Priority = string.IsNullOrWhiteSpace(task.Priority) ? "Medium" : task.Priority;
                existingTask.Deadline = task.Deadline;
                existingTask.AssignedTo = task.AssignedTo;

                _taskRepository.Update(existingTask);
                _taskRepository.Save();

                TempData["SuccessMessage"] = "Cập nhật công việc thành công.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Users = new SelectList(_userRepository.GetAll(), "UserId", "FullName", task.AssignedTo);
            return View(task);
        }

        // ── DELETE ─────────────────────────────────────────────────────────
        public IActionResult Delete(int id)
        {
            var task = _taskRepository.GetById(id);
            if (task == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);
            var submissions = _submissionRepository.GetByTaskId(id).ToList();

            if (task.Status != "Todo" || submissions.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa công việc đang thực hiện hoặc đã có báo cáo!";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!IsLeaderOfTask(task, currentUserId) &&
                !User.IsInRole("SystemAdmin") && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa công việc này!";
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.AssignedUser = task.AssignedTo.HasValue
                ? _userRepository.GetById(task.AssignedTo.Value)?.FullName : null;

            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var task = _taskRepository.GetById(id);
            if (task == null) return NotFound();

            var submissions = _submissionRepository.GetByTaskId(id).ToList();
            if (task.Status != "Todo" || submissions.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa công việc này!";
                return RedirectToAction(nameof(Details), new { id });
            }

            _taskRepository.Delete(id);
            _taskRepository.Save();

            TempData["SuccessMessage"] = "Xóa công việc thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}