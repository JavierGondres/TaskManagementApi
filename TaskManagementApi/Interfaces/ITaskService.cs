using TaskManagementApi.DTOs.Tasks;

namespace TaskManagementApi.Interfaces;

public interface ITaskService
{
    IEnumerable<TaskResponseDto> GetAll();

    TaskResponseDto? GetById(int id);

    TaskResponseDto Create(CreateTaskDto dto);
}
