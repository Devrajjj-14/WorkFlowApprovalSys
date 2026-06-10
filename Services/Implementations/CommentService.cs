using Microsoft.EntityFrameworkCore;
using WorkflowApprovalApi.Data;
using WorkflowApprovalApi.DTOs;
using WorkflowApprovalApi.Models;
using WorkflowApprovalApi.Services.Interfaces;

namespace WorkflowApprovalApi.Services.Implementations;

public class CommentService : ICommentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CommentService> _logger;

    public CommentService(AppDbContext context, ILogger<CommentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Project Comments ──────────────────────────────────────────────────
    public async Task<CommentResponse> CreateAsync(CommentCreateRequest request, int userId)
    {
        _logger.LogInformation("Creating comment on project {ProjectId} by user {UserId}", request.ProjectId, userId);

        var projectExists = await _context.Projects.AnyAsync(p => p.Id == request.ProjectId);
        if (!projectExists)
            throw new InvalidOperationException("Project not found.");

        var comment = new Comment
        {
            ProjectId = request.ProjectId,
            UserId = userId,
            Message = request.Message,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        var saved = await _context.Comments
            .Include(c => c.User)
            .FirstAsync(c => c.Id == comment.Id);

        _logger.LogInformation("Comment {CommentId} created successfully", saved.Id);
        return MapToResponse(saved);
    }

    public async Task<List<CommentResponse>> GetByProjectIdAsync(int projectId)
    {
        _logger.LogDebug("Fetching comments for project {ProjectId}", projectId);
        var comments = await _context.Comments
            .Include(c => c.User)
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return comments.Select(MapToResponse).ToList();
    }

    // ── Task Comments ─────────────────────────────────────────────────────
    public async Task<TaskCommentResponse> CreateTaskCommentAsync(TaskCommentCreateRequest request, int userId)
    {
        _logger.LogInformation("Creating task comment on task {TaskId} by user {UserId}", request.TaskId, userId);

        var taskExists = await _context.Tasks.AnyAsync(t => t.Id == request.TaskId);
        if (!taskExists)
            throw new InvalidOperationException("Task not found.");

        var comment = new TaskComment
        {
            TaskId = request.TaskId,
            UserId = userId,
            Message = request.Message,
            CreatedAt = DateTime.UtcNow
        };

        _context.TaskComments.Add(comment);
        await _context.SaveChangesAsync();

        var saved = await _context.TaskComments
            .Include(c => c.User)
            .FirstAsync(c => c.Id == comment.Id);

        _logger.LogInformation("Task comment {CommentId} created successfully", saved.Id);
        return MapToTaskCommentResponse(saved);
    }

    public async Task<List<TaskCommentResponse>> GetByTaskIdAsync(int taskId)
    {
        _logger.LogDebug("Fetching comments for task {TaskId}", taskId);
        var comments = await _context.TaskComments
            .Include(c => c.User)
            .Where(c => c.TaskId == taskId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return comments.Select(MapToTaskCommentResponse).ToList();
    }

    // ── Mappers ───────────────────────────────────────────────────────────
    private static CommentResponse MapToResponse(Comment comment) => new()
    {
        Id = comment.Id,
        ProjectId = comment.ProjectId,
        UserId = comment.UserId,
        Message = comment.Message,
        CreatedAt = comment.CreatedAt
    };

    private static TaskCommentResponse MapToTaskCommentResponse(TaskComment comment) => new()
    {
        Id = comment.Id,
        TaskId = comment.TaskId,
        UserId = comment.UserId,
        Message = comment.Message,
        CreatedAt = comment.CreatedAt
    };
}
