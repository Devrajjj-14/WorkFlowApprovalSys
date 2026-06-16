using Microsoft.EntityFrameworkCore;
using WorkflowApprovalApi.Data;
using WorkflowApprovalApi.DTOs;
using WorkflowApprovalApi.Models;
using WorkflowApprovalApi.Services.Interfaces;

namespace WorkflowApprovalApi.Services.Implementations;

public class ProjectService : IProjectService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(AppDbContext context, ILogger<ProjectService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProjectResponse> CreateAsync(ProjectCreateRequest request, int userId)
    {
        _logger.LogInformation("Creating project {ProjectName} for user {UserId}", request.Name, userId);
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

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Reload with navigation properties so MapToResponse can read the full names.
        var created = await LoadProjectAsync(project.Id);
        _logger.LogInformation("Project {ProjectId} created successfully", created!.Id);
        return MapToResponse(created!);
    }

    public async Task<List<ProjectResponse>> GetAllAsync()
    {
        _logger.LogDebug("Fetching all projects");
        var projects = await _context.Projects
            .Include(p => p.CreatedByUser)
            .Include(p => p.UpdatedByUser)          // needed for audit display
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return projects.Select(MapToResponse).ToList();
    }

    public async Task<ProjectResponse?> GetByIdAsync(int id)
    {
        _logger.LogDebug("Fetching project {ProjectId}", id);
        var project = await LoadProjectAsync(id);
        return project == null ? null : MapToResponse(project);
    }

    // userId is now required — we need to record WHO changed the status.
    public async Task<ProjectResponse?> UpdateStatusAsync(int id, ProjectUpdateStatusRequest request, int userId)
    {
        _logger.LogInformation("Updating project {ProjectId} status to {Status} by user {UserId}", id, request.Status, userId);
        var project = await LoadProjectAsync(id);
        if (project == null) return null;

        if (Enum.TryParse<ProjectStatus>(request.Status, out var status))
            project.Status = status;

        // Record who made this change and when.
        // After this point UpdatedByUserId will never be null for this project again.
        project.UpdatedByUserId = userId;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Reload so navigation property UpdatedByUser is populated with the new user's name.
        var updated = await LoadProjectAsync(id);
        return MapToResponse(updated!);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("Deleting project {ProjectId}", id);
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project == null) return false;

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Project {ProjectId} deleted successfully", id);
        return true;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    // Centralised loader that always includes both audit navigation properties.
    // Called from Create, GetById, UpdateStatus to avoid duplicating .Include() chains.
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
