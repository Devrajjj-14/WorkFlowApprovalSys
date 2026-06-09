using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowApprovalUI.Models;
using WorkflowApprovalUI.Services;

namespace WorkflowApprovalUI.Controllers;

[Authorize]
public class TasksController : Controller
{
    private readonly ApiService _api;
    public TasksController(ApiService api) => _api = api;

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(TaskCreateViewModel vm)
    {
        var (_, error) = await _api.CreateTaskAsync(vm);
        if (error != null) TempData["Error"] = error;
        return RedirectToAction("Detail", "Projects", new { id = vm.ProjectId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, string status, int projectId)
    {
        await _api.UpdateTaskStatusAsync(id, status);
        return RedirectToAction("Detail", "Projects", new { id = projectId });
    }
}
