using TaskManagementApi.DTOs.Common;
using TaskManagementApi.DTOs.Tasks;

namespace TaskManagementApi.Interfaces;

public interface ITaskService
{
    Task<PagedResultDto<TaskResponseDto>> GetAllAsync(TaskQueryDto query);

    Task<TaskResponseDto> GetByIdAsync(int id);

    Task<TaskResponseDto> CreateAsync(CreateTaskDto dto);

    Task<TaskResponseDto> UpdateAsync(int id, UpdateTaskDto dto);

    Task<TaskResponseDto> CompleteAsync(int id);

    Task DeleteAsync(int id);
}
