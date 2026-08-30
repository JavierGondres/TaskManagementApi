using System.ComponentModel.DataAnnotations;

namespace TaskManagementApi.DTOs.Tasks;

public class CreateTaskDto
{
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public PriorityEnum Priority { get; set; } = PriorityEnum.Medium;

    public DateTime? DueDate { get; set; }
}

public enum PriorityEnum
{
    Low,
    Medium,
    High,
}

public enum TaskStatusEnum
{
    Pending,
    InProgress,
    Completed,
}