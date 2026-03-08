using Microsoft.AspNetCore.Mvc;
using TaskManagement.Web.Models;
using TaskManagement.Web.Repositories;

namespace TaskManagement.Web.Controllers
{
    [Route("api/tasks")]
    [ApiController]
    public class TasksApiController : ControllerBase
    {
        private readonly ITaskRepository _taskRepository;

        public TasksApiController(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        [HttpGet]
        public IActionResult GetAll(int page = 1, int pageSize = 5)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 5;

            var allTasks = _taskRepository.GetAll()
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            var totalItems = allTasks.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var tasks = allTasks
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                page,
                pageSize,
                totalItems,
                totalPages,
                data = tasks
            });
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var task = _taskRepository.GetById(id);
            if (task == null)
            {
                return NotFound(new { message = "Không tìm thấy công việc." });
            }

            return Ok(task);
        }

        [HttpPost]
        public IActionResult Create([FromBody] TaskItem task)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            task.CreatedAt = DateTime.Now;

            if (string.IsNullOrWhiteSpace(task.Status))
                task.Status = "ToDo";

            if (string.IsNullOrWhiteSpace(task.Priority))
                task.Priority = "Medium";

            _taskRepository.Add(task);
            _taskRepository.Save();

            return Ok(new
            {
                message = "Tạo công việc thành công.",
                data = task
            });
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] TaskItem task)
        {
            if (id != task.TaskId)
            {
                return BadRequest(new { message = "TaskId không khớp." });
            }

            var existingTask = _taskRepository.GetById(id);
            if (existingTask == null)
            {
                return NotFound(new { message = "Không tìm thấy công việc." });
            }

            existingTask.Title = task.Title;
            existingTask.Description = task.Description;
            existingTask.Status = string.IsNullOrWhiteSpace(task.Status) ? "ToDo" : task.Status;
            existingTask.Priority = string.IsNullOrWhiteSpace(task.Priority) ? "Medium" : task.Priority;
            existingTask.Deadline = task.Deadline;
            existingTask.AssignedTo = task.AssignedTo;

            _taskRepository.Update(existingTask);
            _taskRepository.Save();

            return Ok(new
            {
                message = "Cập nhật công việc thành công.",
                data = existingTask
            });
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var existingTask = _taskRepository.GetById(id);
            if (existingTask == null)
            {
                return NotFound(new { message = "Không tìm thấy công việc." });
            }

            _taskRepository.Delete(id);
            _taskRepository.Save();

            return Ok(new { message = "Xóa công việc thành công." });
        }
    }
}