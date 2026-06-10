using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowApprovalApi.DTOs;
using WorkflowApprovalApi.Services.Interfaces;

namespace WorkflowApprovalApi.Controllers;

[Authorize]
[ApiController]
[Route("api/comments")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    // ── Project Comments ──────────────────────────────────────────────────
    [HttpPost]
    public async Task<ActionResult<CommentResponse>> Create([FromBody] CommentCreateRequest request)
    {
        try
        {
            var result = await _commentService.CreateAsync(request, GetCurrentUserId());
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("/api/projects/{projectId}/comments")]
    public async Task<ActionResult<List<CommentResponse>>> GetByProject(int projectId)
    {
        var result = await _commentService.GetByProjectIdAsync(projectId);
        return Ok(result);
    }

    // ── Task Comments ─────────────────────────────────────────────────────
    [HttpPost("task")]
    public async Task<ActionResult<TaskCommentResponse>> CreateTaskComment([FromBody] TaskCommentCreateRequest request)
    {
        try
        {
            var result = await _commentService.CreateTaskCommentAsync(request, GetCurrentUserId());
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("/api/tasks/{taskId}/comments")]
    public async Task<ActionResult<List<TaskCommentResponse>>> GetByTask(int taskId)
    {
        var result = await _commentService.GetByTaskIdAsync(taskId);
        return Ok(result);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim!);
    }
}
