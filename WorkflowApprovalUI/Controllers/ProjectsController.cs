// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: [Authorize] attribute doesn't exist — anonymous users could open project pages
using Microsoft.AspNetCore.Authorization;

// Without this: Controller, IActionResult, View, NotFound, RedirectToAction, TempData don't exist
using Microsoft.AspNetCore.Mvc;

// Without this: view models and API response types are unknown — project views won't compile
using WorkflowApprovalUI.Models;

// Without this: ApiService is unknown — this controller can't load or mutate projects via API
using WorkflowApprovalUI.Services;

namespace WorkflowApprovalUI.Controllers;

// ProjectsController is the main hub: list projects, create, view detail, update status, delete
// Without this class: most of the app (project list and detail) has no server logic — UI is empty
[Authorize]
public class ProjectsController : Controller
{
    // Injected ApiService for all /api/projects/* and related aggregated calls
    // Without this field: no data from backend — every project action fails
    private readonly ApiService _api;

    // DI injects ApiService when controller is created per request
    // Without this constructor: ProjectsController can't be built — main app routes crash
    public ProjectsController(ApiService api) => _api = api;

    // ── Project List ────────────────────────────────────────────────────────────
    // Loads all projects from API and renders the index view
    // Without this action: /Projects/Index 404 — users see no project dashboard after login
    public async Task<IActionResult> Index()
    {
        // GET /api/projects — list for cards/table on index page
        // Without this: View gets null/empty — index shows nothing
        var projects = await _api.GetProjectsAsync();

        // Passes project list to Index.cshtml
        // Without this: action returns no view — blank response
        return View(projects);
    }

    // ── Create Project (GET) ────────────────────────────────────────────────────
    // Shows empty create form for Admin/Manager flows linked from index
    // Without this action: "New project" link 404 — create form never appears
    [HttpGet]
    public IActionResult Create() => View();

    // ── Create Project (POST) ───────────────────────────────────────────────────
    // Submits new project name and description to the backend
    // Without this action: create form POST fails — projects can't be added from UI
    [HttpPost]
    public async Task<IActionResult> Create(ProjectCreateViewModel vm)
    {
        // POST /api/projects with name and description from form
        // Without this: project never created in database
        var (data, error) = await _api.CreateProjectAsync(vm.Name, vm.Description);

        // On API failure, show error and re-display form with user input
        // Without this block: errors redirect away — user loses context
        if (data == null)
        {
            // API error message on create view
            // Without this: failed create shows no reason
            ViewBag.Error = error;

            // Re-render create form with bound vm
            // Without this: user sent elsewhere on validation failure
            return View(vm);
        }

        // Success message flashed once on index after redirect
        // Without this: user lands on list with no confirmation
        TempData["Success"] = "Project created successfully.";

        // Go back to project list showing the new project
        // Without this: user stays on create page after success
        return RedirectToAction(nameof(Index));
    }

    // ── Project Detail ────────────────────────────────────────────────────────────
    // Loads one project plus tasks, approvals, comments, files, and per-task comments for tabs
    // Without this action: clicking a project 404 — detail page and all tabs unusable
    public async Task<IActionResult> Detail(int id)
    {
        // GET single project by id — header info and audit fields
        // Without this: detail page has no project title/status — 404 if missing handled next
        var project = await _api.GetProjectAsync(id);

        // Return HTTP 404 if project doesn't exist or user can't see it
        // Without this: null project crashes the view or shows garbage
        if (project == null) return NotFound();

        // Load tasks belonging to this project for Tasks tab
        // Without this: Tasks tab empty — task UI broken
        var tasks     = await _api.GetTasksAsync(id);

        // Load approval requests for Approvals tab
        // Without this: Approvals tab empty — workflow UI broken
        var approvals = await _api.GetApprovalsAsync(id);

        // Load project-level comments for Comments tab
        // Without this: Comments tab empty
        var comments  = await _api.GetCommentsAsync(id);

        // Load uploaded files for Files tab
        // Without this: Files tab empty — uploads list missing
        var files     = await _api.GetFilesAsync(id);

        // Build one API call per task to fetch task comments — run all at once for speed
        // Without this: task comment threads under each task won't load
        var taskCommentTasks = tasks.Select(t => _api.GetTaskCommentsAsync(t.Id));

        // Wait for all task-comment requests to finish
        // Without this: dictionary below would be incomplete or throw
        var taskCommentResults = await Task.WhenAll(taskCommentTasks);

        // Map task id → comment list so the view can render comments per task row
        // Without this: TaskComments dictionary empty — inline task discussion hidden
        var taskComments = tasks
            .Select((t, i) => (t.Id, taskCommentResults[i]))
            .ToDictionary(x => x.Id, x => x.Item2);

        // Aggregate everything into one view model for Detail.cshtml tabs
        // Without this: view has no single model — tabs can't bind data
        var vm = new ProjectDetailViewModel
        {
            // Without this property: header shows no project info
            Project      = project,

            // Without this property: Tasks tab has no data
            Tasks        = tasks,

            // Without this property: Approvals tab has no data
            Approvals    = approvals,

            // Without this property: Comments tab has no data
            Comments     = comments,

            // Without this property: Files tab has no data
            Files        = files,

            // Without this property: task rows can't show nested comments
            TaskComments = taskComments
        };

        // Render detail view with full tab payload
        // Without this: user gets no HTML after loading detail
        return View(vm);
    }

    // ── Update Project Status ─────────────────────────────────────────────────────
    // Admin/Manager change project status (e.g. Active, Completed) from detail page
    // Without this action: status change form POST fails — project status stuck
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        // PATCH status on backend for project id
        // Without this: new status never saved
        await _api.UpdateProjectStatusAsync(id, status);

        // Reload same detail page so user sees updated status badge
        // Without this: user doesn't see change reflected
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Delete Project ────────────────────────────────────────────────────────────
    // Admin-only hard delete with success/error flash on index
    // Without this action: delete button on project does nothing
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        // DELETE project on backend
        // Without this: project remains — delete appears broken
        var (success, error) = await _api.DeleteProjectAsync(id);

        // Show error if delete failed (dependencies, permissions, etc.)
        // Without this branch: failed delete is silent
        if (!success)
            TempData["Error"] = error ?? "Could not delete project.";

        // Show success when project was removed
        // Without this branch: successful delete gives no feedback
        else
            TempData["Success"] = "Project deleted successfully.";

        // Return to project list (deleted project should disappear)
        // Without this: user stays on detail of deleted project
        return RedirectToAction(nameof(Index));
    }
}
