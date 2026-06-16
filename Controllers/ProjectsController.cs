// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: ClaimTypes.NameIdentifier is unknown — GetCurrentUserId() won't compile
using System.Security.Claims;

// Without this: [Authorize], [Authorize(Roles=...)] don't exist — role checks on delete/status fail to compile
using Microsoft.AspNetCore.Authorization;

// Without this: [ApiController], [Route], [HttpGet], ControllerBase etc. don't exist — controller won't compile
using Microsoft.AspNetCore.Mvc;

// Without this: ProjectCreateRequest, ProjectResponse etc. are unknown — action signatures fail
using WorkflowApprovalApi.DTOs;

// Without this: IProjectService is unknown — ProjectsController can't be constructed via DI
using WorkflowApprovalApi.Services.Interfaces;

// Without this: ProjectsController lives in the global namespace — breaks project structure
namespace WorkflowApprovalApi.Controllers;

// Requires a valid JWT for every action in this controller — anonymous users can't access projects
// Without this: unauthenticated requests reach project endpoints — anyone can list or create projects
[Authorize]

// Marks this class as an API controller — enables automatic model validation on request bodies
// Without this: invalid JSON on create/update may not return clean 400 errors
[ApiController]

// Sets the base URL prefix for all actions to /api/projects
// Without this: routes won't match what the frontend calls — all project API calls get 404
[Route("api/projects")]

// ProjectsController handles CRUD and status updates for workflow projects
// Without this class: no project endpoints exist — the entire project management feature is dead
public class ProjectsController : ControllerBase
{
    // Holds the injected project service that performs database operations
    // Without this field: every action has no business logic layer — all calls crash with NullReferenceException
    private readonly IProjectService _projectService;

    // Constructor — DI injects IProjectService (registered as ProjectService in Program.cs)
    // Without this constructor: DI can't create ProjectsController — all project endpoints return 500
    public ProjectsController(IProjectService projectService)
    {
        // Stores the service instance for use in all action methods
        // Without this assignment: _projectService stays null — every request throws NullReferenceException
        _projectService = projectService;
    }

    // ── Create Project ──────────────────────────────────────────────────────────

    // Maps POST /api/projects to this method — any authenticated user can create a project
    // Without this: new projects can't be created via the API
    [HttpPost]

    // Creates a new project owned by the current user
    // Without this method: POST /api/projects returns 404 — frontend create-project form fails
    public async Task<ActionResult<ProjectResponse>> Create([FromBody] ProjectCreateRequest request)
    {
        // Reads the logged-in user's ID from the JWT claims — used as project owner/creator
        // Without this: service doesn't know WHO created the project — ownership tracking breaks
        var userId = GetCurrentUserId();

        // Delegates to ProjectService — validates input, inserts row, returns ProjectResponse
        // Without this: no project is saved to the database
        var result = await _projectService.CreateAsync(request, userId);

        // Returns HTTP 201 Created with Location header pointing to GET /api/projects/{id}
        // Without this: client gets 200 with no canonical URL — REST clients can't discover the new resource
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // ── List All Projects ─────────────────────────────────────────────────────────

    // Maps GET /api/projects to this method
    // Without this: the project list page has no data source — frontend shows empty list forever
    [HttpGet]

    // Returns every project in the system (filtered by service-layer rules if any)
    // Without this method: GET /api/projects returns 404
    public async Task<ActionResult<List<ProjectResponse>>> GetAll()
    {
        // Delegates to ProjectService — queries DB and maps entities to DTOs
        // Without this: empty list is never fetched — response is always null/crash
        var result = await _projectService.GetAllAsync();

        // Returns HTTP 200 with JSON array of projects
        // Without this: client receives no body — UI can't render the project table
        return Ok(result);
    }

    // ── Get Project By Id ─────────────────────────────────────────────────────────

    // Maps GET /api/projects/{id} to this method — {id} is bound from the URL path
    // Without this: individual project detail pages can't load data
    [HttpGet("{id}")]

    // Fetches a single project by its primary key
    // Without this method: GET /api/projects/5 returns 404 even when project 5 exists in DB
    public async Task<ActionResult<ProjectResponse>> GetById(int id)
    {
        // Delegates to ProjectService — looks up project by id
        // Without this: id is never queried — result is always default/null
        var result = await _projectService.GetByIdAsync(id);

        // Checks whether the project exists
        // Without this if: missing projects return 200 with null body — client can't tell "not found" from empty
        if (result == null)
        {
            // Returns HTTP 404 with a clear message — frontend can show "Project not found"
            // Without this: client gets 200 null — confusing UX for deleted or invalid IDs
            return NotFound(new { message = "Project not found." });
        }

        // Returns HTTP 200 with the project JSON
        // Without this: even valid projects never reach the client
        return Ok(result);
    }

    // ── Update Project Status ─────────────────────────────────────────────────────

    // Restricts this action to Admin and Manager roles only — regular users can't change project status
    // Without this: any authenticated user could approve or cancel projects — security/ workflow risk
    [Authorize(Roles = "Admin,Manager")]

    // Maps PUT /api/projects/{id}/status to this method
    // Without this: status transitions (e.g. Draft → InReview) can't be triggered via API
    [HttpPut("{id}/status")]

    // Updates the workflow status of a project (e.g. Pending, Approved, Rejected)
    // Without this method: project status stays frozen — approval workflow can't progress
    public async Task<ActionResult<ProjectResponse>> UpdateStatus(int id, [FromBody] ProjectUpdateStatusRequest request)
    {
        // Reads who is performing the status change — stored for audit/history in the service layer
        // Without this: status changes are anonymous — audit trail loses the acting user
        var userId = GetCurrentUserId();

        // Delegates to ProjectService — validates transition rules, updates DB, returns updated project
        // Without this: status in the database never changes
        var result = await _projectService.UpdateStatusAsync(id, request, userId);

        // Returns 404 if project id doesn't exist
        // Without this if: missing id might return 200 with null — client can't handle error cleanly
        if (result == null)
            return NotFound(new { message = "Project not found." });

        // Returns HTTP 200 with updated project JSON
        // Without this: client never sees the new status after a successful update
        return Ok(result);
    }

    // ── Delete Project ────────────────────────────────────────────────────────────

    // Restricts delete to Admin role only — Managers and below can't remove projects
    // Without this: any Manager could permanently delete projects — too destructive for that role
    [Authorize(Roles = "Admin")]

    // Maps DELETE /api/projects/{id} to this method
    // Without this: projects can't be removed via the API
    [HttpDelete("{id}")]

    // Permanently deletes a project (and related data per service-layer cascade rules)
    // Without this method: DELETE /api/projects/5 returns 404 — admin delete button does nothing
    public async Task<IActionResult> Delete(int id)
    {
        // Delegates to ProjectService — returns true if a row was deleted, false if id not found
        // Without this: nothing is deleted from the database
        var deleted = await _projectService.DeleteAsync(id);

        // Returns 404 when the id doesn't match any project
        // Without this if: deleting a non-existent project might return 204 — misleading success
        if (!deleted)
            return NotFound(new { message = "Project not found." });

        // Returns HTTP 204 No Content — standard REST response for successful delete with no body
        // Without this: client doesn't know delete succeeded — may retry unnecessarily
        return NoContent();
    }

    // ── Helper — Current User Id ──────────────────────────────────────────────────

    // Extracts the numeric user ID from the JWT token's NameIdentifier claim
    // Without this method: every action duplicates claim-parsing logic — easy to get wrong
    private int GetCurrentUserId()
    {
        // Reads "sub" / NameIdentifier claim set by TokenService when the JWT was issued
        // Without this: userIdClaim is null — int.Parse below throws and request returns 500
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Converts the claim string to int — the ! asserts it's never null (valid JWT always has this claim)
        // Without this: actions can't pass userId to the service — ownership and audit break
        return int.Parse(userIdClaim!);
    }
}
