// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: [Authorize] attribute doesn't exist — anonymous users could create/update/delete tasks
using Microsoft.AspNetCore.Authorization;

// Without this: Controller, IActionResult, RedirectToAction, TempData don't exist — task actions won't compile
using Microsoft.AspNetCore.Mvc;

// Without this: TaskCreateViewModel is unknown — Create action can't bind form data
using WorkflowApprovalUI.Models;

// Without this: ApiService is unknown — this controller can't call backend task endpoints
using WorkflowApprovalUI.Services;

namespace WorkflowApprovalUI.Controllers;

// TasksController handles create, status update, and delete for tasks on a project
// Without this class: task forms on project detail POST nowhere — task management UI is broken
[Authorize]
public class TasksController : Controller
{
    // Injected ApiService for /api/tasks/* backend calls
    // Without this field: no HTTP client — task operations can't reach the API
    private readonly ApiService _api;

    // DI injects ApiService when the controller is constructed
    // Without this constructor: TasksController can't be created — task routes crash
    public TasksController(ApiService api) => _api = api;

    // ── Create Task ───────────────────────────────────────────────────────────
    // Admin/Manager only — creates a new task on a project via the backend API
    // Without this action: "Add task" form submit returns 404 — tasks can't be created from UI
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(TaskCreateViewModel vm)
    {
        // Sends title, description, assignee, priority, and project id to the backend
        // Without this: form data never reaches API — task list never grows
        var (_, error) = await _api.CreateTaskAsync(vm);

        // Surfaces API validation or permission errors on the next project detail load
        // Without this: failed create looks like success — user doesn't know task wasn't added
        if (error != null) TempData["Error"] = error;

        // Returns to project detail page for the same project (tasks tab implied by default detail view)
        // Without this: user stuck on POST response — no refreshed task list
        return RedirectToAction("Detail", "Projects", new { id = vm.ProjectId });
    }

    // ── Update Task Status ────────────────────────────────────────────────────
    // Any authenticated user can change task status (e.g. InProgress, Done) on project detail
    // Without this action: status dropdown/button forms do nothing — tasks stay stuck in old status
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, string status, int projectId)
    {
        // PATCH/PUT status to backend for the given task id
        // Without this: status change never persists — UI and API disagree
        await _api.UpdateTaskStatusAsync(id, status);

        // Redirects to project detail with tasks tab query so user sees updated status in context
        // Without this: user doesn't land back on tasks section after status change
        return RedirectToAction("Detail", "Projects", new { id = projectId, tab = "tasks" });
    }

    // ── Delete Task ─────────────────────────────────────────────────────────────
    // Admin/Manager only — removes a task and returns to project detail tasks tab
    // Without this action: delete buttons on tasks have no handler — tasks can't be removed
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id, int projectId)
    {
        // Calls backend DELETE for the task
        // Without this: task remains in database — delete button appears broken
        var (success, error) = await _api.DeleteTaskAsync(id);

        // Shows error flash if delete failed (permissions, not found, etc.)
        // Without this branch: failed delete gives no user feedback
        if (!success) TempData["Error"] = error ?? "Could not delete task.";

        // Shows success flash when delete succeeded
        // Without this branch: successful delete is silent — user unsure it worked
        else TempData["Success"] = "Task deleted.";

        // Returns to project detail on tasks tab with updated list
        // Without this: user doesn't see refreshed task list after delete
        return RedirectToAction("Detail", "Projects", new { id = projectId, tab = "tasks" });
    }
}
