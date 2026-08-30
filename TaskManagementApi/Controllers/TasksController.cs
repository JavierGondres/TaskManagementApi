using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagementApi.DTOs.Tasks;
using TaskManagementApi.Interfaces;

namespace TaskManagementApi.Controllers;

[Authorize]
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
    public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetAll()
    {
        return Ok(await _taskService.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskResponseDto>> GetById(int id)
    {
        var task = await _taskService.GetByIdAsync(id);
        if (task is null)
        {
            return NotFound();
        }

        return Ok(task);
    }

    [HttpPost]
    public async Task<ActionResult<TaskResponseDto>> Create(CreateTaskDto dto)
    {
        var created = await _taskService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TaskResponseDto>> Update(int id, UpdateTaskDto dto)
    {
        var updated = await _taskService.UpdateAsync(id, dto);
        if (updated is null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    [HttpPatch("{id:int}/complete")]
    public async Task<ActionResult<TaskResponseDto>> Complete(int id)
    {
        var completed = await _taskService.CompleteAsync(id);
        if (completed is null)
        {
            return NotFound();
        }

        return Ok(completed);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _taskService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
