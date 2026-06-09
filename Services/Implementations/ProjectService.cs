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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var created = await _context.Projects
            .Include(p => p.CreatedByUser)
            .FirstAsync(p => p.Id == project.Id);

        _logger.LogInformation("Project {ProjectId} created successfully", created.Id);
        return MapToResponse(created);
    }

    public async Task<List<ProjectResponse>> GetAllAsync()
    {
        _logger.LogDebug("Fetching all projects");
        var projects = await _context.Projects
            .Include(p => p.CreatedByUser)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return projects.Select(MapToResponse).ToList();
    }

    public async Task<ProjectResponse?> GetByIdAsync(int id)
    {
        _logger.LogDebug("Fetching project {ProjectId}", id);
        var project = await _context.Projects
            .Include(p => p.CreatedByUser)
            .FirstOrDefaultAsync(p => p.Id == id);

        return project == null ? null : MapToResponse(project);
    }

    public async Task<ProjectResponse?> UpdateStatusAsync(int id, ProjectUpdateStatusRequest request)
    {
        _logger.LogInformation("Updating project {ProjectId} status to {Status}", id, request.Status);
        var project = await _context.Projects
            .Include(p => p.CreatedByUser)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return null;
        }

        if (Enum.TryParse<ProjectStatus>(request.Status, out var status))
        {
            project.Status = status;
        }
        project.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToResponse(project);
    }

    private static ProjectResponse MapToResponse(Project project)
    {
        return new ProjectResponse
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Status = project.Status.ToString(),
            CreatedByUserId = project.CreatedByUserId,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }
}
