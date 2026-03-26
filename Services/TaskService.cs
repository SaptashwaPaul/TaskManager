using Microsoft.EntityFrameworkCore;
using TaskManager.API.Data;
using TaskManager.API.DTOs.Tasks;
using TaskManager.API.Entities;
using TaskManager.API.Interfaces;

namespace TaskManager.API.Services
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;

        public TaskService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TaskItem> CreateTask(int userId, CreateTaskDto dto)
        {
            var userExists = await _context.Users
                .AnyAsync(u => u.Id == dto.AssignedToUserId);

            if (!userExists)
                throw new ArgumentException("Assigned user does not exist");

            var statusExists = await _context.TaskStates
                .AnyAsync(s => s.Id == 1);

            if (!statusExists)
                throw new ArgumentException("Default status not found");

            var task = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                AssignedToUserId = dto.AssignedToUserId,
                CreatedByUserId = userId,
                DueDate = dto.DueDate,
                Priority = dto.Priority.Trim().ToLower() switch
                {
                    "high" => "High",
                    "low" => "Low",
                    _ => "Medium"
                },
                StatusId = 1
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return task;
        }

        public async Task<List<TaskResponseDto>> GetTasks(int userId, string role, int page, int pageSize, string? search = null, int? statusId = null)
        {
            var query = _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.Status)
                .AsQueryable();

            // Role-based filtering
            if (role != "Admin")
            {
                query = query.Where(t => t.AssignedToUserId == userId);
            }

            // 🔍 Search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => t.Title.Contains(search));
            }

            // 📌 Status filter
            if (statusId.HasValue)
            {
                query = query.Where(t => t.StatusId == statusId);
            }

            // 📄 Pagination
            var tasks = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return tasks.Select(t => new TaskResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Priority = t.Priority,
                Status = t.Status.Name,
                AssignedTo = t.AssignedToUser.Name,
                DueDate = t.DueDate
            }).ToList();
        }

        public async Task<TaskResponseDto?> UpdateTaskStatus(int id, int statusId)
        {
            var task = await _context.Tasks
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return null;

            var status = await _context.TaskStates
                .FirstOrDefaultAsync(s => s.Id == statusId);

            if (status == null)
                throw new ArgumentException("Invalid status");

            task.StatusId = statusId;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new TaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Priority = task.Priority,
                Status = status.Name, // ✅ FIXED
                AssignedTo = task.AssignedToUser.Name,
                DueDate = task.DueDate
            };


        }

        public async Task<bool> DeleteTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
                return false;

            _context.Tasks.Remove(task);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}