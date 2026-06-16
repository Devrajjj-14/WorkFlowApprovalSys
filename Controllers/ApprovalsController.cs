// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: ClaimTypes.NameIdentifier is unknown — GetCurrentUserId() won't compile
using System.Security.Claims;

// Without this: [Authorize], [Authorize(Roles=...)] don't exist — role checks fail to compile
using Microsoft.AspNetCore.Authorization;

// Without this: [ApiController], [Route], [HttpPut], ControllerBase etc. don't exist — controller won't compile
using Microsoft.AspNetCore.Mvc;

// Without this: ApprovalCreateRequest, ApprovalResponse, ApprovalUpdateRequest are unknown
using WorkflowApprovalApi.DTOs;

// Without this: IApprovalService is unknown — ApprovalsController can't be constructed via DI
using WorkflowApprovalApi.Services.Interfaces;

// Without this: ApprovalsController lives in the global namespace — breaks project structure
namespace WorkflowApprovalApi.Controllers;

// Requires a valid JWT for every action — anonymous users can't view or act on approvals
// Without this: unauthenticated requests reach approval endpoints — workflow security is broken
[Authorize]

// Marks this class as an API controller — enables automatic model validation on request bodies
// Without this: invalid JSON on create/approve may not return clean 400 errors
[ApiController]

// Sets the base URL prefix for conventional routes on this controller to /api/approvals
// Without this: relative routes like {id}/approve won't resolve correctly
[Route("api/approvals")]

// ApprovalsController handles creating approval requests and approve/reject/changes-requested actions
// Without this class: the core workflow approval feature has no API — the app's main purpose is non-functional
public class ApprovalsController : ControllerBase
{
    // Holds the injected approval service that manages approval state in the database
    // Without this field: every action crashes — no approval workflow can run
    private readonly IApprovalService _approvalService;

    // Constructor — DI injects IApprovalService (registered as ApprovalService in Program.cs)
    // Without this constructor: DI can't create ApprovalsController — all approval endpoints return 500
    public ApprovalsController(IApprovalService approvalService)
    {
        // Stores the service instance for use in all action methods
        // Without this assignment: _approvalService stays null — NullReferenceException on every request
        _approvalService = approvalService;
    }

    // ── Create Approval Request ─────────────────────────────────────────────────

    // Restricts creation to Admin, Manager, and Designer — Clients/Reviewers can't initiate approvals
    // Without this: wrong roles could spawn approval requests — workflow assignment rules break
    [Authorize(Roles = "Admin,Manager,Designer")]

    // Maps POST /api/approvals to this method
    // Without this: new approval requests can't be submitted via the API
    [HttpPost]

    // Creates a new approval request on a project (e.g. "please review this deliverable")
    // Without this method: POST /api/approvals returns 404 — designers can't request sign-off
    public async Task<ActionResult<ApprovalResponse>> Create([FromBody] ApprovalCreateRequest request)
    {
        // Wraps service call so validation errors (e.g. duplicate pending approval) become 400 responses
        // Without this try: InvalidOperationException becomes an unhandled 500 error
        try
        {
            // Reads the logged-in user's ID — recorded as requester in the service/database
            // Without this: approval has no requester — audit trail is incomplete
            var userId = GetCurrentUserId();

            // Delegates to ApprovalService — inserts approval row, returns ApprovalResponse
            // Without this: no approval record is created — workflow never starts
            var result = await _approvalService.CreateAsync(request, userId);

            // Returns HTTP 200 with the new approval JSON (id, status, projectId, etc.)
            // Without this: client gets no body — UI can't show the pending approval
            return Ok(result);
        }
        // Catches business-rule failures like "project not found" or "approval already pending"
        // Without this catch: validation failures crash with 500 instead of readable 400
        catch (InvalidOperationException ex)
        {
            // Returns HTTP 400 with { message: "..." } — frontend can show why creation failed
            // Without this: client can't tell invalid input from server error
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── List Approvals By Project ─────────────────────────────────────────────────

    // Uses absolute route GET /api/projects/{projectId}/approvals
    // Without this full path: frontend approval list calls wouldn't match this action
    [HttpGet("/api/projects/{projectId}/approvals")]

    // Returns all approval requests and their statuses for a project
    // Without this method: project approval history panel has no data
    public async Task<ActionResult<List<ApprovalResponse>>> GetByProject(int projectId)
    {
        // Delegates to ApprovalService — queries approvals WHERE ProjectId = projectId
        // Without this: approval list is never fetched — response is always empty/crash
        var result = await _approvalService.GetByProjectIdAsync(projectId);

        // Returns HTTP 200 with JSON array of approvals
        // Without this: frontend can't render the approval timeline
        return Ok(result);
    }

    // ── Approve ───────────────────────────────────────────────────────────────────

    // Restricts approve action to Admin, Manager, Reviewer, and Client — Designers can't approve their own work
    // Without this: unauthorized roles could approve deliverables — workflow integrity breaks
    [Authorize(Roles = "Admin,Manager,Reviewer,Client")]

    // Maps PUT /api/approvals/{id}/approve to this method
    // Without this: approvers can't mark an approval as Approved via the API
    [HttpPut("{id}/approve")]

    // Sets an approval's status to Approved, optionally with a comment
    // Without this method: approve button in the UI does nothing — workflow can't complete
    public async Task<ActionResult<ApprovalResponse>> Approve(int id, [FromBody] ApprovalUpdateRequest request)
    {
        // Reads who is approving — stored for audit in the service layer
        // Without this: approval action is anonymous — history loses the acting reviewer/client
        var userId = GetCurrentUserId();

        // Delegates to ApprovalService — validates permissions, updates status to Approved, returns result
        // Without this: approval status in the database never changes to Approved
        var result = await _approvalService.ApproveAsync(id, request, userId);

        // Returns 404 if approval id doesn't exist
        // Without this if: missing id might return 200 with null — client can't handle error cleanly
        if (result == null)
        {
            // Returns HTTP 404 with clear message — frontend shows "Approval not found"
            // Without this: client gets confusing empty success on invalid id
            return NotFound(new { message = "Approval not found." });
        }

        // Returns HTTP 200 with updated approval JSON reflecting Approved status
        // Without this: client never sees confirmation that approval succeeded
        return Ok(result);
    }

    // ── Reject ────────────────────────────────────────────────────────────────────

    // Same role gate as Approve — only reviewers/clients/managers/admins can reject
    // Without this: unauthorized roles could reject work — workflow integrity breaks
    [Authorize(Roles = "Admin,Manager,Reviewer,Client")]

    // Maps PUT /api/approvals/{id}/reject to this method
    // Without this: reject action has no URL — reject button returns 404
    [HttpPut("{id}/reject")]

    // Sets an approval's status to Rejected, usually with a reason in the request body
    // Without this method: rejected deliverables can't be recorded — workflow stalls incorrectly
    public async Task<ActionResult<ApprovalResponse>> Reject(int id, [FromBody] ApprovalUpdateRequest request)
    {
        // Reads who is rejecting — stored for audit in the service layer
        // Without this: rejection is anonymous — designers can't see who rejected and why
        var userId = GetCurrentUserId();

        // Delegates to ApprovalService — validates permissions, updates status to Rejected
        // Without this: rejection never persists in the database
        var result = await _approvalService.RejectAsync(id, request, userId);

        // Returns 404 if approval id doesn't exist
        // Without this if: rejecting a non-existent approval might return 200 null — misleading
        if (result == null)
        {
            // Returns HTTP 404 — frontend can show "Approval not found"
            // Without this: client can't tell invalid id from successful reject
            return NotFound(new { message = "Approval not found." });
        }

        // Returns HTTP 200 with updated approval JSON reflecting Rejected status
        // Without this: client never sees rejection confirmation or updated status
        return Ok(result);
    }

    // ── Request Changes ───────────────────────────────────────────────────────────

    // Same role gate — reviewers/clients can ask for revisions without fully rejecting
    // Without this: only binary approve/reject exists — "needs changes" workflow step is missing
    [Authorize(Roles = "Admin,Manager,Reviewer,Client")]

    // Maps PUT /api/approvals/{id}/changes-requested to this method
    // Without this: "request changes" button has no endpoint — partial feedback can't be recorded
    [HttpPut("{id}/changes-requested")]

    // Sets an approval's status to ChangesRequested — designer must revise and resubmit
    // Without this method: reviewers can't send work back for edits — only full reject is possible
    public async Task<ActionResult<ApprovalResponse>> RequestChanges(int id, [FromBody] ApprovalUpdateRequest request)
    {
        // Reads who requested changes — stored for audit in the service layer
        // Without this: change requests are anonymous — designer doesn't know who asked for edits
        var userId = GetCurrentUserId();

        // Delegates to ApprovalService — validates permissions, updates status to ChangesRequested
        // Without this: "changes requested" state never persists — workflow can't loop back to designer
        var result = await _approvalService.RequestChangesAsync(id, request, userId);

        // Returns 404 if approval id doesn't exist
        // Without this if: invalid id might return 200 null — client can't handle error cleanly
        if (result == null)
        {
            // Returns HTTP 404 — frontend shows "Approval not found"
            // Without this: client gets confusing response on bad id
            return NotFound(new { message = "Approval not found." });
        }

        // Returns HTTP 200 with updated approval JSON reflecting ChangesRequested status
        // Without this: client never sees that changes were requested successfully
        return Ok(result);
    }

    // ── Helper — Current User Id ──────────────────────────────────────────────────

    // Extracts the numeric user ID from the JWT token's NameIdentifier claim
    // Without this method: create/approve/reject actions can't pass userId to the service
    private int GetCurrentUserId()
    {
        // Reads NameIdentifier claim set by TokenService when the JWT was issued
        // Without this: userIdClaim is null — int.Parse below throws and request returns 500
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Converts claim string to int — valid JWT always includes this claim
        // Without this: approval actions have no acting user — audit trail is empty
        return int.Parse(userIdClaim!);
    }
}
