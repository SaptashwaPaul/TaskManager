using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.DTOs.Tasks
{
    public class CreateTaskDto
    {
        [Required]
        [MinLength(3)]
        public string Title { get; set; } = null!;

        [Required]
        public string Description { get; set; } = null!;

        [Required]
        public int AssignedToUserId { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        [RegularExpression("High|Medium|Low", ErrorMessage = "Priority must be High, Medium or Low")]
        public string Priority { get; set; } = "Medium";
    }
}