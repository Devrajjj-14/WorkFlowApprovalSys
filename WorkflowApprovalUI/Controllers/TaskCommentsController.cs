using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowApprovalUI.Services;

namespace WorkflowApprovalUI.Controllers;

[Authorize]
public class TaskCommentsController : Controller
{
    private readonly ApiService _api;
    public TaskCommentsController(ApiService api) => _api = api;

    [HttpPost]
    public async Task<IActionResult> Create(int taskId, string message, int projectId)
    {
        await _api.CreateTaskCommentAsync(taskId, message);
        return RedirectToAction("Detail", "Projects", new { id = projectId, tab = "tasks" });
    }
}
