// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: ClaimTypes.NameIdentifier is unknown — GetCurrentUserId() won't compile
using System.Security.Claims;

// Without this: [Authorize], [Authorize(Roles=...)] don't exist — role checks fail to compile
using Microsoft.AspNetCore.Authorization;

// Without this: [ApiController], [Route], [HttpPost], ControllerBase etc. don't exist — controller won't compile
using Microsoft.AspNetCore.Mvc;

// Without this: TaskCreateRequest, TaskResponse etc. are unknown — action signatures fail
using WorkflowApprovalApi.DTOs;

// Without this: ITaskService is unknown — TasksController can't be constructed via DI
using WorkflowApprovalApi.Services.Interfaces;

// Without this: TasksController lives in the global namespace — breaks project structure
namespace WorkflowApprovalApi.Controllers;

// Requires a valid JWT for every action — anonymous users can't access tasks
// Without this: unauthenticated requests reach task endpoints — anyone could create or delete tasks
[Authorize]

// Marks this class as an API controller — enables automatic model validation on request bodies
// Without this: invalid JSON on create may not return clean 400 errors
[ApiController]

// Sets the base URL prefix for conventional routes on this controller to /api/tasks
// Without this: relative routes like {id}/status won't resolve correctly
[Route("api/tasks")]

// TasksController handles task CRUD, listing by project, and status updates within projects
// Without this class: no task endpoints exist — project task boards can't load or save data
public class TasksController : ControllerBase
{
    // Holds the injected task service that performs database operations
    // Without this field: every action crashes — no business logic can run
    private readonly ITaskService _taskService;

    // Constructor — DI injects ITaskService (registered as TaskService in Program.cs)
    // Without this constructor: DI can't create TasksController — all task endpoints return 500
    public TasksController(ITaskService taskService)
    {
        // Stores the service instance for use in all action methods
        // Without this assignment: _taskService stays null — NullReferenceException on every request
        _taskService = taskService;
    }

    // ── Create Task ─────────────────────────────────────────────────────────────

    // Restricts task creation to Admin and Manager roles — Designers/Clients can't add tasks
    // Without this: any logged-in user could create tasks — workflow assignment rules break down
    [Authorize(Roles = "Admin,Manager")]

    // Maps POST /api/tasks to this method
    // Without this: new tasks can't be created via the API
    [HttpPost]

    // Creates a new task under a project with title, assignee, due date etc.
    // Without this method: POST /api/tasks returns 404 — frontend add-task form fails
    public async Task<ActionResult<TaskResponse>> Create([FromBody] TaskCreateRequest request)
    {
        // Wraps service call so validation errors (e.g. invalid project id) become clean 400 responses
        // Without this try: InvalidOperationException becomes an unhandled 500 error page
        try
        {
            // Reads the logged-in user's ID — recorded as creator in the service layer
            // Without this: service doesn't know who created the task — audit trail is incomplete
            var userId = GetCurrentUserId();

            // Delegates to TaskService — validates project exists, inserts task row, returns TaskResponse
            // Without this: no task is saved to the database
            var result = await _taskService.CreateAsync(request, userId);

            // Returns HTTP 200 with the new task JSON
            // Without this: client gets empty response — UI can't show the newly created task
            return Ok(result);
        }
        // Catches business-rule failures like "project not found" or "invalid assignee"
        // Without this catch: validation failures crash with 500 instead of readable 400
        catch (InvalidOperationException ex)
        {
            // Returns HTTP 400 with { message: "..." } — frontend can display the validation error
            // Without this: client can't tell WHY task creation failed
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── List Tasks By Project ─────────────────────────────────────────────────────

    // Uses absolute route GET /api/projects/{projectId}/tasks — nested under projects, not under /api/tasks
    // Without this full path: URL wouldn't match frontend calls like GET /api/projects/3/tasks
    [HttpGet("/api/projects/{projectId}/tasks")]

    // Returns all tasks belonging to a specific project
    // Without this method: project task list page has no data — board stays empty
    public async Task<ActionResult<List<TaskResponse>>> GetByProject(int projectId)
    {
        // Delegates to TaskService — queries tasks WHERE ProjectId = projectId
        // Without this: task list is never fetched — response is always empty/crash
        var result = await _taskService.GetByProjectIdAsync(projectId);

        // Returns HTTP 200 with JSON array of tasks
        // Without this: frontend receives no task data to render
        return Ok(result);
    }

    // ── Update Task Status ────────────────────────────────────────────────────────

    // Maps PUT /api/tasks/{id}/status to this method — any authenticated user can update status
    // Without this: assignees can't mark tasks InProgress or Done — workflow stalls
    [HttpPut("{id}/status")]

    // Changes a task's workflow status (e.g. Todo → InProgress → Done)
    // Without this method: task status never changes via the API
    public async Task<ActionResult<TaskResponse>> UpdateStatus(int id, [FromBody] TaskUpdateStatusRequest request)
    {
        // Reads who is changing the status — stored for audit in the service layer
        // Without this: status changes are anonymous — history loses the acting user
        var userId = GetCurrentUserId();

        // Delegates to TaskService — validates transition, updates DB, returns updated task
        // Without this: status column in the database never updates
        var result = await _taskService.UpdateStatusAsync(id, request, userId);

        // Returns 404 if task id doesn't exist
        // Without this if: missing id might return 200 with null — client can't handle error cleanly
        if (result == null)
            return NotFound(new { message = "Task not found." });

        // Returns HTTP 200 with updated task JSON
        // Without this: client never sees the new status after a successful update
        return Ok(result);
    }

    // ── Delete Task ───────────────────────────────────────────────────────────────

    // Restricts delete to Admin and Manager — regular assignees can't remove tasks
    // Without this: any user could delete tasks they didn't create — data loss risk
    [Authorize(Roles = "Admin,Manager")]

    // Maps DELETE /api/tasks/{id} to this method
    // Without this: tasks can't be removed via the API
    [HttpDelete("{id}")]

    // Permanently deletes a task by id
    // Without this method: DELETE /api/tasks/5 returns 404 — admin delete button does nothing
    public async Task<IActionResult> Delete(int id)
    {
        // Delegates to TaskService — returns true if deleted, false if id not found
        // Without this: nothing is removed from the database
        var deleted = await _taskService.DeleteAsync(id);

        // Returns 404 when the id doesn't match any task
        // Without this if: deleting a non-existent task might return 204 — misleading success
        if (!deleted)
            return NotFound(new { message = "Task not found." });

        // Returns HTTP 204 No Content — standard REST response for successful delete
        // Without this: client doesn't know delete succeeded
        return NoContent();
    }

    // ── Helper — Current User Id ──────────────────────────────────────────────────

    // Extracts the numeric user ID from the JWT token's NameIdentifier claim
    // Without this method: create and update actions can't pass userId to the service
    private int GetCurrentUserId()
    {
        // Reads NameIdentifier claim set by TokenService when the JWT was issued
        // Without this: userIdClaim is null — int.Parse below throws and request returns 500
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Converts claim string to int — valid JWT always includes this claim
        // Without this: service layer never receives the acting user's id
        return int.Parse(userIdClaim!);
    }
}
