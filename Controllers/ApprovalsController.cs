using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowApprovalApi.DTOs;
using WorkflowApprovalApi.Services.Interfaces;

namespace WorkflowApprovalApi.Controllers;

[Authorize]
[ApiController]
[Route("api/approvals")]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvalService;

    public ApprovalsController(IApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    [Authorize(Roles = "Admin,Manager,Designer")]
    [HttpPost]
    public async Task<ActionResult<ApprovalResponse>> Create([FromBody] ApprovalCreateRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _approvalService.CreateAsync(request, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("/api/projects/{projectId}/approvals")]
    public async Task<ActionResult<List<ApprovalResponse>>> GetByProject(int projectId)
    {
        var result = await _approvalService.GetByProjectIdAsync(projectId);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Manager,Reviewer,Client")]
    [HttpPut("{id}/approve")]
    public async Task<ActionResult<ApprovalResponse>> Approve(int id, [FromBody] ApprovalUpdateRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _approvalService.ApproveAsync(id, request, userId);
        if (result == null)
        {
            return NotFound(new { message = "Approval not found." });
        }

        return Ok(result);
    }

    [Authorize(Roles = "Admin,Manager,Reviewer,Client")]
    [HttpPut("{id}/reject")]
    public async Task<ActionResult<ApprovalResponse>> Reject(int id, [FromBody] ApprovalUpdateRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _approvalService.RejectAsync(id, request, userId);
        if (result == null)
        {
            return NotFound(new { message = "Approval not found." });
        }

        return Ok(result);
    }

    [Authorize(Roles = "Admin,Manager,Reviewer,Client")]
    [HttpPut("{id}/changes-requested")]
    public async Task<ActionResult<ApprovalResponse>> RequestChanges(int id, [FromBody] ApprovalUpdateRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _approvalService.RequestChangesAsync(id, request, userId);
        if (result == null)
        {
            return NotFound(new { message = "Approval not found." });
        }

        return Ok(result);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim!);
    }
}
