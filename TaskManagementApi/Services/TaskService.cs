using TaskManagementApi.DTOs.Tasks;
using TaskManagementApi.Interfaces;
using TaskManagementApi.Models;

namespace TaskManagementApi.Services;

public class TaskService : ITaskService
{
    private readonly List<TaskItem> _tasks = [];
    private int _nextId = 1;

    public IEnumerable<TaskResponseDto> GetAll()
    {
        return _tasks.Select(ToResponse);
    }

    public TaskResponseDto? GetById(int id)
    {
        var task = _tasks.FirstOrDefault(item => item.Id == id);
        return task is null ? null : ToResponse(task);
    }

    public TaskResponseDto Create(CreateTaskDto dto)
    {
        var task = new TaskItem
        {
            Id = _nextId++,
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            DueDate = dto.DueDate,
            Status = TaskStatusEnum.Pending,
            CreatedAt = DateTime.UtcNow,
        };

        _tasks.Add(task);
        return ToResponse(task);
    }

    private static TaskResponseDto ToResponse(TaskItem task)
    {
        return new TaskResponseDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            DueDate = task.DueDate,
            CreatedAt = task.CreatedAt,
        };
    }
}
