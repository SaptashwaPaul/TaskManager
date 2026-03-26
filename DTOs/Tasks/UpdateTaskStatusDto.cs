using System.ComponentModel.DataAnnotations;

public class UpdateTaskStatusDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "StatusId must be greater than 0")]
    public int StatusId { get; set; }
}