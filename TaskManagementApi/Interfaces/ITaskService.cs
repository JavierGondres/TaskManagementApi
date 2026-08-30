using TaskManagementApi.DTOs.Tasks;

namespace TaskManagementApi.Interfaces;

public interface ITaskService
{
    Task<IReadOnlyList<TaskResponseDto>> GetAllAsync();

    Task<TaskResponseDto?> GetByIdAsync(int id);

    Task<TaskResponseDto> CreateAsync(CreateTaskDto dto);

    Task<TaskResponseDto?> UpdateAsync(int id, UpdateTaskDto dto);

    Task<TaskResponseDto?> CompleteAsync(int id);

    Task<bool> DeleteAsync(int id);
}
