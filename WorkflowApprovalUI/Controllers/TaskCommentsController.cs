// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: [Authorize] attribute doesn't exist — anonymous users could post task comments
using Microsoft.AspNetCore.Authorization;

// Without this: Controller, IActionResult, RedirectToAction don't exist — MVC action won't compile
using Microsoft.AspNetCore.Mvc;

// Without this: ApiService is unknown — this controller can't call the backend task-comments API
using WorkflowApprovalUI.Services;

namespace WorkflowApprovalUI.Controllers;

// TaskCommentsController handles comments attached to individual tasks (not project-wide comments)
// Without this class: task comment forms on project detail have no server handler — comments can't be added
[Authorize]
public class TaskCommentsController : Controller
{
    // Injected ApiService for POST /api/tasks/{id}/comments
    // Without this field: Create action can't reach the backend — task comments never persist
    private readonly ApiService _api;

    // DI supplies ApiService when the controller is created
    // Without this constructor: controller instantiation fails — task comment routes break
    public TaskCommentsController(ApiService api) => _api = api;

    // ── Create Task Comment ───────────────────────────────────────────────────
    // Saves a comment on a specific task, then returns to the project detail tasks tab
    // Without this action: task comment form POST returns 404 — inline task discussion doesn't work
    [HttpPost]
    public async Task<IActionResult> Create(int taskId, string message, int projectId)
    {
        // Calls backend to attach message to the given task id
        // Without this: comment text is dropped — task thread never updates
        await _api.CreateTaskCommentAsync(taskId, message);

        // Redirects back to project detail on the tasks tab where the comment form lives
        // Without this: user gets no navigation after submit — poor UX and possible duplicate POSTs
        return RedirectToAction("Detail", "Projects", new { id = projectId, tab = "tasks" });
    }
}
