using Microsoft.AspNetCore.Mvc;
using TaskManagementApi.DTOs.Tasks;
using TaskManagementApi.Interfaces;

namespace TaskManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<TaskResponseDto>> GetAll()
    {
        return Ok(_taskService.GetAll());
    }

    [HttpGet("{id:int}")]
    public ActionResult<TaskResponseDto> GetById(int id)
    {
        var task = _taskService.GetById(id);
        if (task is null)
        {
            return NotFound();
        }

        return Ok(task);
    }

    [HttpPost]
    public ActionResult<TaskResponseDto> Create(CreateTaskDto dto)
    {
        var created = _taskService.Create(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
