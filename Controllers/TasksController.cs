using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowApprovalApi.DTOs;
using WorkflowApprovalApi.Services.Interfaces;

namespace WorkflowApprovalApi.Controllers;

[Authorize]
[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Create([FromBody] TaskCreateRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _taskService.CreateAsync(request, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("/api/projects/{projectId}/tasks")]
    public async Task<ActionResult<List<TaskResponse>>> GetByProject(int projectId)
    {
        var result = await _taskService.GetByProjectIdAsync(projectId);
        return Ok(result);
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<TaskResponse>> UpdateStatus(int id, [FromBody] TaskUpdateStatusRequest request)
    {
        var result = await _taskService.UpdateStatusAsync(id, request);
        if (result == null)
            return NotFound(new { message = "Task not found." });
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _taskService.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = "Task not found." });
        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim!);
    }
}
