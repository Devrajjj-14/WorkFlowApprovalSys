using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowApprovalUI.Models;
using WorkflowApprovalUI.Services;

namespace WorkflowApprovalUI.Controllers;

[Authorize]
public class ApprovalsController : Controller
{
    private readonly ApiService _api;
    public ApprovalsController(ApiService api) => _api = api;

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Designer")]
    public async Task<IActionResult> Create(ApprovalCreateViewModel vm)
    {
        var (_, error) = await _api.CreateApprovalAsync(vm);
        if (error != null) TempData["Error"] = error;
        else TempData["Success"] = "Approval request submitted.";
        return RedirectToAction("Detail", "Projects", new { id = vm.ProjectId, tab = "approvals" });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Reviewer,Client")]
    public async Task<IActionResult> Approve(int id, string? remarks, int projectId)
    {
        var (_, error) = await _api.ApproveAsync(id, remarks ?? string.Empty);
        if (error != null) TempData["Error"] = error;
        else TempData["Success"] = "Approval marked as Approved.";
        return RedirectToAction("Detail", "Projects", new { id = projectId, tab = "approvals" });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Reviewer,Client")]
    public async Task<IActionResult> Reject(int id, string? remarks, int projectId)
    {
        var (_, error) = await _api.RejectAsync(id, remarks ?? string.Empty);
        if (error != null) TempData["Error"] = error;
        else TempData["Success"] = "Approval marked as Rejected.";
        return RedirectToAction("Detail", "Projects", new { id = projectId, tab = "approvals" });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Reviewer,Client")]
    public async Task<IActionResult> RequestChanges(int id, string? remarks, int projectId)
    {
        var (_, error) = await _api.RequestChangesAsync(id, remarks ?? string.Empty);
        if (error != null) TempData["Error"] = error;
        else TempData["Success"] = "Changes requested on approval.";
        return RedirectToAction("Detail", "Projects", new { id = projectId, tab = "approvals" });
    }
}
