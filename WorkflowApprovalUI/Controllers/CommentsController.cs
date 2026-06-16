// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: [Authorize] attribute doesn't exist — anonymous users could post comments
using Microsoft.AspNetCore.Authorization;

// Without this: Controller, IActionResult, RedirectToAction don't exist — MVC action won't compile
using Microsoft.AspNetCore.Mvc;

// Without this: ApiService is unknown — this controller can't call the backend comments API
using WorkflowApprovalUI.Services;

namespace WorkflowApprovalUI.Controllers;

// CommentsController handles project-level discussion comments (not task comments)
// Without this class: users can't add comments on a project — comments tab forms fail
[Authorize]
public class CommentsController : Controller
{
    // Injected ApiService for POST /api/projects/{id}/comments
    // Without this field: Create action has no HTTP client — comment submission can't run
    private readonly ApiService _api;

    // DI wires ApiService into the controller per request
    // Without this constructor: ASP.NET can't create CommentsController — route crashes
    public CommentsController(ApiService api) => _api = api;

    // ── Create Comment ────────────────────────────────────────────────────────
    // Accepts a comment from the project detail page and saves it via the backend API
    // Without this action: comment form POST has no handler — new comments are never saved
    [HttpPost]
    public async Task<IActionResult> Create(int projectId, string message)
    {
        // Sends the comment text to the backend for the given project
        // Without this: message never reaches the API — comment list stays unchanged
        await _api.CreateCommentAsync(projectId, message);

        // Returns to project detail with the comments tab active so the user sees their new comment
        // Without this: user stays on a blank POST response or wrong page after submitting
        return RedirectToAction("Detail", "Projects", new { id = projectId, tab = "comments" });
    }
}
