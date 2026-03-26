using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManager.API.Data;
using TaskManager.API.DTOs.Tasks;
using TaskManager.API.Entities;
using TaskManager.API.Interfaces;
using TaskManager.API.Models;

namespace TaskManager.API.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    [Authorize]
    public class TasksController : ControllerBase
    {

        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        // CREATE TASK
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CreateTask(CreateTaskDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var task = await _taskService.CreateTask(userId, dto);

            var response = new TaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Priority = task.Priority,
                Status = task.Status?.Name ?? "",
                AssignedTo = task.AssignedToUser?.Name ?? "",
                DueDate = task.DueDate
            };

            return Ok(ApiResponse<TaskResponseDto>
                .SuccessResponse(response, "Task created successfully"));
        }

        // GET TASKS
        [HttpGet]
        public async Task<IActionResult> GetTasks(int page = 1, int pageSize = 5, string? search = null, int? statusId = null)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var role = User.FindFirst(ClaimTypes.Role)!.Value;

            var tasks = await _taskService.GetTasks(userId, role, page, pageSize, search, statusId);

            return Ok(new { data = tasks });
        }

        // UPDATE STATUS
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateTaskStatus(int id, UpdateTaskStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var task = await _taskService.UpdateTaskStatus(id, dto.StatusId);

                if (task == null)
                    return NotFound(new { message = "Task not found" });

                return Ok(task);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var result = await _taskService.DeleteTask(id);

            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}