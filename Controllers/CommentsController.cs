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

    [HttpPost]
    public async Task<ActionResult<CommentResponse>> Create([FromBody] CommentCreateRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _commentService.CreateAsync(request, userId);
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

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim!);
    }
}
