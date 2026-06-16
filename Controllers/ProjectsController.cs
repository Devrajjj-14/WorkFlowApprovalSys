using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowApprovalApi.DTOs;
using WorkflowApprovalApi.Services.Interfaces;

namespace WorkflowApprovalApi.Controllers;

[Authorize]
[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create([FromBody] ProjectCreateRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _projectService.CreateAsync(request, userId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectResponse>>> GetAll()
    {
        var result = await _projectService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectResponse>> GetById(int id)
    {
        var result = await _projectService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound(new { message = "Project not found." });
        }

        return Ok(result);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPut("{id}/status")]
    public async Task<ActionResult<ProjectResponse>> UpdateStatus(int id, [FromBody] ProjectUpdateStatusRequest request)
    {
        var userId = GetCurrentUserId();    // capture who is changing the status
        var result = await _projectService.UpdateStatusAsync(id, request, userId);
        if (result == null)
            return NotFound(new { message = "Project not found." });
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _projectService.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = "Project not found." });
        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim!);
    }
}
