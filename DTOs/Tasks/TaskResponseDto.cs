namespace TaskManager.API.DTOs.Tasks
{
    public class TaskResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Priority { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string AssignedTo { get; set; } = null!;
        public DateTime DueDate { get; set; }
    }
}