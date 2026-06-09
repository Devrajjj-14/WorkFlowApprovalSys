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

    public async Task<CommentResponse> CreateAsync(CommentCreateRequest request, int userId)
    {
        _logger.LogInformation(
            "Creating comment on project {ProjectId} by user {UserId}",
            request.ProjectId,
            userId);
        var projectExists = await _context.Projects.AnyAsync(p => p.Id == request.ProjectId);
        if (!projectExists)
        {
            throw new InvalidOperationException("Project not found.");
        }

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

    private static CommentResponse MapToResponse(Comment comment)
    {
        return new CommentResponse
        {
            Id = comment.Id,
            ProjectId = comment.ProjectId,
            UserId = comment.UserId,
            Message = comment.Message,
            CreatedAt = comment.CreatedAt
        };
    }
}
