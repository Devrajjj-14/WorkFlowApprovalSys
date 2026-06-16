// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: Include, AnyAsync, FirstAsync, Add, SaveChangesAsync don't exist
using Microsoft.EntityFrameworkCore;

// Without this: AppDbContext is unknown — comment DB operations fail to compile
using WorkflowApprovalApi.Data;

// Without this: CommentCreateRequest, CommentResponse, TaskCommentCreateRequest etc. are unknown
using WorkflowApprovalApi.DTOs;

// Without this: Comment and TaskComment models are unknown — entity creation fails
using WorkflowApprovalApi.Models;

// Without this: ICommentService interface is unknown — CommentService can't implement the contract
using WorkflowApprovalApi.Services.Interfaces;

namespace WorkflowApprovalApi.Services.Implementations;

// CommentService handles project-level comments and task-level comments
// Called by CommentsController — validates parent project/task exists before insert
// Without this class: all /api/comments endpoints return 500 — comment threads don't work
public class CommentService : ICommentService
{
    // EF Core context for Comments, TaskComments, Projects, and Tasks tables
    // Without this field: no database access — comments can't be saved or listed
    private readonly AppDbContext _context;

    // Logger for comment create/fetch events
    // Without this field: comment activity isn't visible in logs
    private readonly ILogger<CommentService> _logger;

    // Constructor — DI injects database and logger
    // Without this constructor: CommentService can't be constructed — app crashes on startup
    public CommentService(AppDbContext context, ILogger<CommentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════════════════════
    // PROJECT COMMENTS
    // ════════════════════════════════════════════════════════════════════════

    // ── CreateAsync ───────────────────────────────────────────────────────────
    // Adds a comment on a project — author is the authenticated userId
    // Without this: POST /api/comments does nothing — Project Comments tab can't post
    public async Task<CommentResponse> CreateAsync(CommentCreateRequest request, int userId)
    {
        _logger.LogInformation("Creating comment on project {ProjectId} by user {UserId}", request.ProjectId, userId);

        // Ensure project exists — prevents orphan comments with invalid ProjectId
        // Without this: DB FK error instead of clear "Project not found"
        var projectExists = await _context.Projects.AnyAsync(p => p.Id == request.ProjectId);
        if (!projectExists)
            throw new InvalidOperationException("Project not found.");

        // Build Comment entity with UTC timestamp
        // Without this: nothing to insert — SaveChangesAsync writes nothing
        var comment = new Comment
        {
            ProjectId = request.ProjectId,
            UserId = userId,
            Message = request.Message,
            CreatedAt = DateTime.UtcNow
        };

        // Persist to database
        // Without Add + SaveChangesAsync: comment never stored
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        // Reload with User navigation if needed for future mapper extensions
        // Without reload: Id is set but pattern matches other services for consistency
        var saved = await _context.Comments
            .Include(c => c.User)
            .FirstAsync(c => c.Id == comment.Id);

        _logger.LogInformation("Comment {CommentId} created successfully", saved.Id);
        return MapToResponse(saved);
    }

    // ── GetByProjectIdAsync ───────────────────────────────────────────────────
    // Returns all project comments newest first
    // Without this: GET /api/projects/{id}/comments returns empty — tab shows no thread
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

    // ════════════════════════════════════════════════════════════════════════
    // TASK COMMENTS
    // ════════════════════════════════════════════════════════════════════════

    // ── CreateTaskCommentAsync ────────────────────────────────────────────────
    // Adds a comment on a specific task — author is authenticated userId
    // Without this: POST /api/comments/task does nothing — inline task threads broken
    public async Task<TaskCommentResponse> CreateTaskCommentAsync(TaskCommentCreateRequest request, int userId)
    {
        _logger.LogInformation("Creating task comment on task {TaskId} by user {UserId}", request.TaskId, userId);

        // Verify task exists before insert
        // Without this: orphan TaskComment rows or FK violation
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

    // ── GetByTaskIdAsync ──────────────────────────────────────────────────────
    // Returns all comments for one task, newest first
    // Without this: GET /api/tasks/{id}/comments returns empty — task row threads empty
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

    // ════════════════════════════════════════════════════════════════════════
    // MAPPERS
    // ════════════════════════════════════════════════════════════════════════

    // Maps Comment entity to API response DTO
    // Without this: Create/Get would inline mapping — duplicated field lists
    private static CommentResponse MapToResponse(Comment comment) => new()
    {
        Id = comment.Id,
        ProjectId = comment.ProjectId,
        UserId = comment.UserId,
        Message = comment.Message,
        CreatedAt = comment.CreatedAt
    };

    // Maps TaskComment entity to API response DTO
    // Without this: task comment endpoints return wrong JSON shape
    private static TaskCommentResponse MapToTaskCommentResponse(TaskComment comment) => new()
    {
        Id = comment.Id,
        TaskId = comment.TaskId,
        UserId = comment.UserId,
        Message = comment.Message,
        CreatedAt = comment.CreatedAt
    };
}
