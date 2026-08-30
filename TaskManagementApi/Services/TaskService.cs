using Microsoft.EntityFrameworkCore;
using TaskManagementApi.Data;
using TaskManagementApi.DTOs.Common;
using TaskManagementApi.DTOs.Tasks;
using TaskManagementApi.Exceptions;
using TaskManagementApi.Interfaces;
using TaskManagementApi.Models;

namespace TaskManagementApi.Services;

public class TaskService : ITaskService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public TaskService(ApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResultDto<TaskResponseDto>> GetAllAsync(TaskQueryDto query)
    {
        var userId = _currentUser.GetRequiredUserId();
        var tasks = _db.Tasks.AsNoTracking().Where(item => item.UserId == userId);

        if (query.Status is { } status)
        {
            tasks = tasks.Where(item => item.Status == status);
        }

        if (query.Priority is { } priority)
        {
            tasks = tasks.Where(item => item.Priority == priority);
        }

        var totalCount = await tasks.CountAsync();
        var items = await tasks
            .OrderByDescending(item => item.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResultDto<TaskResponseDto>
        {
            Items = items.Select(ToResponse).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<TaskResponseDto> GetByIdAsync(int id)
    {
        var task = await GetOwnedTaskAsync(id, track: false);
        return ToResponse(task);
    }

    public async Task<TaskResponseDto> CreateAsync(CreateTaskDto dto)
    {
        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            DueDate = ToUtc(dto.DueDate),
            Status = TaskItemStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            UserId = _currentUser.GetRequiredUserId(),
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return ToResponse(task);
    }

    public async Task<TaskResponseDto> UpdateAsync(int id, UpdateTaskDto dto)
    {
        var task = await GetOwnedTaskAsync(id, track: true);

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Priority = dto.Priority;
        task.DueDate = ToUtc(dto.DueDate);
        task.Status = dto.Status;
        task.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToResponse(task);
    }

    public async Task<TaskResponseDto> CompleteAsync(int id)
    {
        var task = await GetOwnedTaskAsync(id, track: true);
        task.Status = TaskItemStatus.Completed;
        task.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToResponse(task);
    }

    public async Task DeleteAsync(int id)
    {
        var userId = _currentUser.GetRequiredUserId();
        var affected = await _db
            .Tasks.Where(item => item.Id == id && item.UserId == userId)
            .ExecuteDeleteAsync();

        if (affected == 0)
        {
            throw new NotFoundException("Task not found");
        }
    }

    private async Task<TaskItem> GetOwnedTaskAsync(int id, bool track)
    {
        var userId = _currentUser.GetRequiredUserId();
        var query = track ? _db.Tasks : _db.Tasks.AsNoTracking();
        var task = await query.FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId);
        return task ?? throw new NotFoundException("Task not found");
    }

    private static DateTime? ToUtc(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        return value.Value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
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
            UpdatedAt = task.UpdatedAt,
        };
    }
}
