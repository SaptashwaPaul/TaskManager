using TaskManager.API.DTOs.Tasks;
using TaskManager.API.Entities;

namespace TaskManager.API.Interfaces
{
    public interface ITaskService
    {
        Task<TaskItem> CreateTask(int userId, CreateTaskDto dto);
        Task<List<TaskResponseDto>> GetTasks(int userId, string role, int page, int pageSize, string? search , int? statusId);
        Task<TaskResponseDto?> UpdateTaskStatus(int id, int statusId);
        Task<bool> DeleteTask(int id);
    }
}