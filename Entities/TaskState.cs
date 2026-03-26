using TaskManager.API.Entities;

public class TaskState
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
