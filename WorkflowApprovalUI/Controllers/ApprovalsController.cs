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
        return RedirectToAction("Detail", "Projects", new { id = vm.ProjectId });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Reviewer,Client")]
    public async Task<IActionResult> Approve(int id, string remarks, int projectId)
    {
        await _api.ApproveAsync(id, remarks);
        return RedirectToAction("Detail", "Projects", new { id = projectId });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Reviewer,Client")]
    public async Task<IActionResult> Reject(int id, string remarks, int projectId)
    {
        await _api.RejectAsync(id, remarks);
        return RedirectToAction("Detail", "Projects", new { id = projectId });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Reviewer,Client")]
    public async Task<IActionResult> RequestChanges(int id, string remarks, int projectId)
    {
        await _api.RequestChangesAsync(id, remarks);
        return RedirectToAction("Detail", "Projects", new { id = projectId });
    }
}
