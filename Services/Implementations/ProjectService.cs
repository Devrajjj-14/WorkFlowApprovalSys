// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: Include, AnyAsync, FirstOrDefaultAsync, Add, Remove, SaveChangesAsync don't exist
using Microsoft.EntityFrameworkCore;

// Without this: AppDbContext is unknown — all database operations fail to compile
using WorkflowApprovalApi.Data;

// Without this: ProjectCreateRequest, ProjectResponse etc. are unknown — method signatures break
using WorkflowApprovalApi.DTOs;

// Without this: Project, ProjectStatus models are unknown — entity creation fails
using WorkflowApprovalApi.Models;

// Without this: IProjectService interface is unknown — ProjectService can't implement the contract
using WorkflowApprovalApi.Services.Interfaces;

namespace WorkflowApprovalApi.Services.Implementations;

// ProjectService handles CRUD and status workflow for projects
// Called by ProjectsController — creates projects, lists them, updates status, deletes
// Without this class: all /api/projects endpoints return 500 — DI can't resolve IProjectService
public class ProjectService : IProjectService
{
    // EF Core database context for Projects table and related users
    // Without this field: no database access — every project operation fails
    private readonly AppDbContext _context;

    // Logger for create/update/delete audit trail
    // Without this field: project lifecycle events don't appear in logs
    private readonly ILogger<ProjectService> _logger;

    // Constructor — DI injects database and logger
    // Without this constructor: ProjectService can't be constructed — app crashes on startup
    public ProjectService(AppDbContext context, ILogger<ProjectService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────
    // Creates a new project in Draft status for the given user
    // Without this: POST /api/projects has no backend logic — new projects can't be saved
    public async Task<ProjectResponse> CreateAsync(ProjectCreateRequest request, int userId)
    {
        _logger.LogInformation("Creating project {ProjectName} for user {UserId}", request.Name, userId);

        // Build Project entity — starts as Draft, creator recorded, updater left null until first edit
        // Without this: nothing to insert — SaveChangesAsync would do nothing useful
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            Status = ProjectStatus.Draft,
            CreatedByUserId = userId,
            // UpdatedByUserId intentionally left NULL — the project has not been edited yet.
            // The UI will show "Created by" until the first status change occurs.
            UpdatedByUserId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Persist new project to database
        // Without Add + SaveChangesAsync: project never reaches MySQL
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Reload with CreatedByUser/UpdatedByUser so MapToResponse can fill audit names
        // Without this: CreatedByName would be null — UI shows blank creator
        var created = await LoadProjectAsync(project.Id);
        _logger.LogInformation("Project {ProjectId} created successfully", created!.Id);

        // Return DTO to controller — non-null asserted because we just inserted this Id
        // Without this return: client gets no project Id or audit fields
        return MapToResponse(created!);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────
    // Returns all projects newest-first with audit names populated
    // Without this: GET /api/projects returns nothing — project list page is empty
    public async Task<List<ProjectResponse>> GetAllAsync()
    {
        _logger.LogDebug("Fetching all projects");

        // Query all projects with creator and last-updater navigation properties
        // OrderByDescending CreatedAt = newest projects first in the UI
        // Without Include: CreatedByName/UpdatedByName can't be resolved — audit labels blank
        var projects = await _context.Projects
            .Include(p => p.CreatedByUser)
            .Include(p => p.UpdatedByUser)          // needed for audit display
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        // Map each entity to DTO — Select + ToList materializes the full list
        // Without this: controller would have to map manually — duplicated logic
        return projects.Select(MapToResponse).ToList();
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────
    // Returns one project by Id or null if not found
    // Without this: GET /api/projects/{id} can't load detail page data
    public async Task<ProjectResponse?> GetByIdAsync(int id)
    {
        _logger.LogDebug("Fetching project {ProjectId}", id);

        // Load single project with audit navigation properties
        // Without LoadProjectAsync: same Include logic would be duplicated in every method
        var project = await LoadProjectAsync(id);

        // Null if missing, otherwise mapped DTO
        // Without this ternary: we'd throw on missing project instead of returning null for 404
        return project == null ? null : MapToResponse(project);
    }

    // ── UpdateStatusAsync ─────────────────────────────────────────────────────
    // Changes project workflow status and records who made the change
    // userId is now required — we need to record WHO changed the status.
    // Without this: PUT /api/projects/{id}/status does nothing — workflow can't advance
    public async Task<ProjectResponse?> UpdateStatusAsync(int id, ProjectUpdateStatusRequest request, int userId)
    {
        _logger.LogInformation("Updating project {ProjectId} status to {Status} by user {UserId}", id, request.Status, userId);

        // Load project or return null — controller maps null to 404
        // Without this: we'd update a non-existent row or crash
        var project = await LoadProjectAsync(id);
        if (project == null) return null;

        // Parse status string from request (e.g. "InReview") into ProjectStatus enum
        // TryParse avoids throw on bad input — invalid status string is silently ignored
        // Without this: status field never changes even when request looks valid
        if (Enum.TryParse<ProjectStatus>(request.Status, out var status))
            project.Status = status;

        // Record who made this change and when.
        // After this point UpdatedByUserId will never be null for this project again.
        project.UpdatedByUserId = userId;
        project.UpdatedAt = DateTime.UtcNow;

        // Write status and audit fields to database
        // Without this: changes stay in EF memory only — lost when request ends
        await _context.SaveChangesAsync();

        // Reload so navigation property UpdatedByUser is populated with the new user's name.
        // Without this: UpdatedByName in response would still be stale or null
        var updated = await LoadProjectAsync(id);
        return MapToResponse(updated!);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────
    // Permanently removes a project (and cascaded related data per EF config)
    // Without this: DELETE /api/projects/{id} can't remove projects
    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("Deleting project {ProjectId}", id);

        // Find project without Includes — delete only needs the entity key
        // Without this: we can't know if project exists before Remove
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project == null) return false;

        // Mark deleted and persist — returns false if id not found, true on success
        // Without Remove + SaveChangesAsync: project remains in database
        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Project {ProjectId} deleted successfully", id);
        return true;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    // Centralised loader that always includes both audit navigation properties.
    // Called from Create, GetById, UpdateStatus to avoid duplicating .Include() chains.
    // Without this: every method would repeat the same Include chain — easy to miss UpdatedByUser
    private async Task<Project?> LoadProjectAsync(int id)
    {
        return await _context.Projects
            .Include(p => p.CreatedByUser)
            .Include(p => p.UpdatedByUser)  // null-safe — EF handles nullable FK correctly
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    // ── MapToResponse ─────────────────────────────────────────────────────────
    // Converts the EF entity into the DTO that travels to the frontend/API consumer.
    // This is the ONLY place audit name fields are populated — keeps naming logic centralised.
    //
    // Business rule for audit display:
    //   UpdatedByUserId == null  → record was never edited  → UI shows "Created by"
    //   UpdatedByUserId != null  → record was edited        → UI shows "Last updated by"
    // Without this: controllers would expose raw entities — wrong shape and leaked internals
    private static ProjectResponse MapToResponse(Project project)
    {
        return new ProjectResponse
        {
            Id                = project.Id,
            Name              = project.Name,
            Description       = project.Description,
            Status            = project.Status.ToString(),

            // Audit – creation (always present)
            CreatedByUserId   = project.CreatedByUserId,
            CreatedByName     = project.CreatedByUser.FullName,
            CreatedAt         = project.CreatedAt,

            // Audit – last update (null until first status change)
            UpdatedByUserId   = project.UpdatedByUserId,
            UpdatedByName     = project.UpdatedByUser?.FullName,  // ?. safe: may be null
            UpdatedAt         = project.UpdatedAt
        };
    }
}
