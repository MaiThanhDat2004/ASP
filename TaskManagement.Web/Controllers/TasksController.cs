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

        public TasksController(ITaskRepository taskRepository, IUserRepository userRepository)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
        }

        public IActionResult Index(string? searchString, string? statusFilter, string? priorityFilter, int page = 1, int pageSize = 5)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 5;

            var tasksQuery = _taskRepository.GetAll()
                .OrderByDescending(t => t.CreatedAt)
                .AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                tasksQuery = tasksQuery.Where(t =>
                    (!string.IsNullOrWhiteSpace(t.Title) && t.Title.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(t.Description) && t.Description.Contains(searchString, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                tasksQuery = tasksQuery.Where(t => t.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(priorityFilter))
            {
                tasksQuery = tasksQuery.Where(t => t.Priority == priorityFilter);
            }

            var filteredTasks = tasksQuery.ToList();

            var totalItems = filteredTasks.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var pagedTasks = filteredTasks
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.PriorityFilter = priorityFilter;

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            ViewBag.Users = _userRepository.GetAll().ToDictionary(u => u.UserId, u => u.FullName);

            return View(pagedTasks);
        }

        public IActionResult Details(int id)
        {
            var task = _taskRepository.GetById(id);
            if (task == null)
            {
                return NotFound();
            }

            ViewBag.AssignedUser = task.AssignedTo.HasValue
                ? _userRepository.GetById(task.AssignedTo.Value)?.FullName
                : null;

            ViewBag.CreatedUser = _userRepository.GetById(task.CreatedBy)?.FullName;

            return View(task);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            var model = new TaskItem
            {
                Deadline = DateTime.Now.AddDays(1),
                Status = "ToDo",
                Priority = "Medium"
            };

            ViewBag.Users = new SelectList(_userRepository.GetAll(), "UserId", "FullName");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Create(TaskItem task)
        {
            if (ModelState.IsValid)
            {
                task.CreatedAt = DateTime.Now;
                task.CreatedBy = int.Parse(User.FindFirst("UserId")!.Value);

                if (string.IsNullOrWhiteSpace(task.Status))
                    task.Status = "ToDo";

                if (string.IsNullOrWhiteSpace(task.Priority))
                    task.Priority = "Medium";

                _taskRepository.Add(task);
                _taskRepository.Save();

                TempData["SuccessMessage"] = "Thêm công việc thành công.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Users = new SelectList(_userRepository.GetAll(), "UserId", "FullName", task.AssignedTo);
            return View(task);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            var task = _taskRepository.GetById(id);
            if (task == null)
            {
                return NotFound();
            }

            ViewBag.Users = new SelectList(_userRepository.GetAll(), "UserId", "FullName", task.AssignedTo);
            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id, TaskItem task)
        {
            if (id != task.TaskId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingTask = _taskRepository.GetById(id);
                if (existingTask == null)
                {
                    return NotFound();
                }

                existingTask.Title = task.Title;
                existingTask.Description = task.Description;
                existingTask.Status = string.IsNullOrWhiteSpace(task.Status) ? "ToDo" : task.Status;
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

        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var task = _taskRepository.GetById(id);
            if (task == null)
            {
                return NotFound();
            }

            ViewBag.AssignedUser = task.AssignedTo.HasValue
                ? _userRepository.GetById(task.AssignedTo.Value)?.FullName
                : null;

            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteConfirmed(int id)
        {
            _taskRepository.Delete(id);
            _taskRepository.Save();

            TempData["SuccessMessage"] = "Xóa công việc thành công.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult MyTasks(int page = 1, int pageSize = 5)
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);

            var allTasks = _taskRepository.GetAll()
                .Where(t => t.AssignedTo == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            var totalItems = allTasks.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var pagedTasks = allTasks
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Users = _userRepository.GetAll().ToDictionary(u => u.UserId, u => u.FullName);

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            ViewBag.SearchString = "";
            ViewBag.StatusFilter = "";
            ViewBag.PriorityFilter = "";

            return View("Index", pagedTasks);
        }
    }
}