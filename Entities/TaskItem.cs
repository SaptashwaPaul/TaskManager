namespace TaskManager.API.Entities
{
    public class TaskItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Priority { get; set; } = "Medium";

        public int StatusId { get; set; }
        public TaskState Status { get; set; } = null!;

        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;

        public int AssignedToUserId { get; set; }
        public User AssignedToUser { get; set; } = null!;

        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Constructor to ensure UTC dates
        public TaskItem()
        {
            CreatedAt = DateTime.UtcNow;
        }
    }
}
