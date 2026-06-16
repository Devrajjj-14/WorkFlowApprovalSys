// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: [Authorize] attribute doesn't exist — anonymous users could approve/reject work
using Microsoft.AspNetCore.Authorization;

// Without this: Controller, IActionResult, RedirectToAction, TempData don't exist — approval actions won't compile
using Microsoft.AspNetCore.Mvc;

// Without this: ApprovalCreateViewModel is unknown — Create action can't bind approval form
using WorkflowApprovalUI.Models;

// Without this: ApiService is unknown — this controller can't call backend approval endpoints
using WorkflowApprovalUI.Services;

namespace WorkflowApprovalUI.Controllers;

// ApprovalsController handles submitting and reviewing approval requests on a project
// Without this class: approval workflow buttons and forms have no server handlers — workflow is dead
[Authorize]
public class ApprovalsController : Controller
{
    // Injected ApiService for /api/approvals/* backend calls
    // Without this field: approval actions can't reach the API — no create/approve/reject
    private readonly ApiService _api;

    // DI supplies ApiService per HTTP request
    // Without this constructor: ApprovalsController fails to instantiate — routes break
    public ApprovalsController(ApiService api) => _api = api;

    // ── Create Approval Request ─────────────────────────────────────────────────
    // Designer/Manager/Admin submit a new approval request (optionally linked to a file)
    // Without this action: "Request approval" form POST fails — new approvals can't be started
    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Designer")]
    public async Task<IActionResult> Create(ApprovalCreateViewModel vm)
    {
        // Sends project id, optional file id, and remarks to backend
        // Without this: approval request never created — list stays empty
        var (_, error) = await _api.CreateApprovalAsync(vm);

        // Shows API error on next page if create failed
        // Without this branch: user thinks request was submitted when it wasn't
        if (error != null) TempData["Error"] = error;

        // Confirms success when API accepted the request
        // Without this branch: successful submit gives no positive feedback
        else TempData["Success"] = "Approval request submitted.";

        // Returns to project detail approvals tab
        // Without this: user doesn't see new pending approval in context
        return RedirectToAction("Detail", "Projects", new { id = vm.ProjectId, tab = "approvals" });
    }

    // ── Approve ─────────────────────────────────────────────────────────────────
    // Reviewer/Client/Manager/Admin marks an approval as Approved with optional remarks
    // Without this action: Approve button does nothing — pending items can't be approved
    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Reviewer,Client")]
    public async Task<IActionResult> Approve(int id, string? remarks, int projectId)
    {
        // Calls backend approve endpoint with remarks (empty string if null)
        // Without this: status stays Pending — approve UI is cosmetic only
        var (_, error) = await _api.ApproveAsync(id, remarks ?? string.Empty);

        // Flash error if API rejected the approve action
        // Without this branch: failed approve looks like it succeeded
        if (error != null) TempData["Error"] = error;

        // Flash success when approval was recorded
        // Without this branch: user gets no confirmation after approving
        else TempData["Success"] = "Approval marked as Approved.";

        // Back to project detail approvals tab with updated status badges
        // Without this: user doesn't see the approval card update
        return RedirectToAction("Detail", "Projects", new { id = projectId, tab = "approvals" });
    }

    // ── Reject ──────────────────────────────────────────────────────────────────
    // Reviewer/Client/Manager/Admin marks an approval as Rejected with optional remarks
    // Without this action: Reject button has no handler — rejections can't be recorded
    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Reviewer,Client")]
    public async Task<IActionResult> Reject(int id, string? remarks, int projectId)
    {
        // Calls backend reject endpoint
        // Without this: rejection never persists — workflow can't block bad deliverables
        var (_, error) = await _api.RejectAsync(id, remarks ?? string.Empty);

        // Shows error message if reject failed
        // Without this branch: silent failure on reject attempts
        if (error != null) TempData["Error"] = error;

        // Confirms rejection to the user
        // Without this branch: no success feedback after reject
        else TempData["Success"] = "Approval marked as Rejected.";

        // Redirects to approvals tab on the same project
        // Without this: user left on blank POST page
        return RedirectToAction("Detail", "Projects", new { id = projectId, tab = "approvals" });
    }

    // ── Request Changes ─────────────────────────────────────────────────────────
    // Reviewer asks for revisions without fully rejecting — ChangesRequested status
    // Without this action: "Request changes" control is dead — iterative review loop broken
    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Reviewer,Client")]
    public async Task<IActionResult> RequestChanges(int id, string? remarks, int projectId)
    {
        // Calls backend request-changes endpoint with reviewer remarks
        // Without this: status never becomes ChangesRequested — designer doesn't know to revise
        var (_, error) = await _api.RequestChangesAsync(id, remarks ?? string.Empty);

        // Surfaces API failure to the user
        // Without this branch: failed request-changes appears to work
        if (error != null) TempData["Error"] = error;

        // Confirms that changes were requested
        // Without this branch: no message after successful request-changes
        else TempData["Success"] = "Changes requested on approval.";

        // Returns to approvals tab to show updated status and remarks
        // Without this: user doesn't see updated approval card
        return RedirectToAction("Detail", "Projects", new { id = projectId, tab = "approvals" });
    }
}
