using Microsoft.EntityFrameworkCore;
using WorkflowApprovalApi.Data;
using WorkflowApprovalApi.DTOs;
using WorkflowApprovalApi.Models;
using WorkflowApprovalApi.Services.Interfaces;

namespace WorkflowApprovalApi.Services.Implementations;

public class ApprovalService : IApprovalService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(AppDbContext context, ILogger<ApprovalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApprovalResponse> CreateAsync(ApprovalCreateRequest request, int userId)
    {
        _logger.LogInformation(
            "Creating approval for project {ProjectId} by user {UserId}",
            request.ProjectId,
            userId);
        var projectExists = await _context.Projects.AnyAsync(p => p.Id == request.ProjectId);
        if (!projectExists)
        {
            throw new InvalidOperationException("Project not found.");
        }

        if (request.FileId.HasValue)
        {
            var fileExists = await _context.UploadedFiles
                .AnyAsync(f => f.Id == request.FileId && f.ProjectId == request.ProjectId);
            if (!fileExists)
            {
                throw new InvalidOperationException("File not found for this project.");
            }
        }

        var approval = new Approval
        {
            ProjectId = request.ProjectId,
            FileId = request.FileId,
            RequestedByUserId = userId,
            Status = ApprovalStatus.Pending,
            Remarks = request.Remarks,
            RequestedAt = DateTime.UtcNow
        };

        _context.Approvals.Add(approval);
        await _context.SaveChangesAsync();

        var saved = await LoadApprovalAsync(approval.Id);
        _logger.LogInformation("Approval {ApprovalId} created successfully", saved!.Id);
        return MapToResponse(saved!);
    }

    public async Task<List<ApprovalResponse>> GetByProjectIdAsync(int projectId)
    {
        _logger.LogDebug("Fetching approvals for project {ProjectId}", projectId);
        var approvals = await _context.Approvals
            .Include(a => a.File)
            .Include(a => a.RequestedByUser)
            .Include(a => a.ReviewedByUser)
            .Where(a => a.ProjectId == projectId)
            .OrderByDescending(a => a.RequestedAt)
            .ToListAsync();

        return approvals.Select(MapToResponse).ToList();
    }

    public async Task<ApprovalResponse?> ApproveAsync(int id, ApprovalUpdateRequest request, int userId)
    {
        return await UpdateApprovalStatusAsync(id, ApprovalStatus.Approved, request, userId);
    }

    public async Task<ApprovalResponse?> RejectAsync(int id, ApprovalUpdateRequest request, int userId)
    {
        return await UpdateApprovalStatusAsync(id, ApprovalStatus.Rejected, request, userId);
    }

    public async Task<ApprovalResponse?> RequestChangesAsync(int id, ApprovalUpdateRequest request, int userId)
    {
        return await UpdateApprovalStatusAsync(id, ApprovalStatus.ChangesRequested, request, userId);
    }

    private async Task<ApprovalResponse?> UpdateApprovalStatusAsync(
        int id,
        ApprovalStatus status,
        ApprovalUpdateRequest request,
        int reviewerId)
    {
        var approval = await LoadApprovalAsync(id);
        if (approval == null)
        {
            return null;
        }

        if (approval.Status != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException("Only pending approvals can be updated.");
        }

        approval.Status = status;
        approval.ReviewedByUserId = reviewerId;
        approval.Remarks = string.IsNullOrWhiteSpace(request.Remarks)
            ? approval.Remarks
            : request.Remarks;
        approval.ReviewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation(
            "Approval {ApprovalId} updated to {Status} by reviewer {ReviewerId}",
            approval.Id,
            status,
            reviewerId);
        return MapToResponse(approval);
    }

    private async Task<Approval?> LoadApprovalAsync(int id)
    {
        return await _context.Approvals
            .Include(a => a.File)
            .Include(a => a.RequestedByUser)
            .Include(a => a.ReviewedByUser)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    private static ApprovalResponse MapToResponse(Approval approval)
    {
        // ── Business rule for audit label ────────────────────────────────────
        // ReviewedByUserId == null  → approval is still Pending   → show nothing
        // ReviewedByUserId != null  → approval was acted on       → show "Approved/Rejected/Changes by"
        return new ApprovalResponse
        {
            Id                 = approval.Id,
            ProjectId          = approval.ProjectId,
            FileId             = approval.FileId,
            Status             = approval.Status.ToString(),
            Remarks            = approval.Remarks,

            // Audit – requester (always present)
            RequestedByUserId  = approval.RequestedByUserId,
            RequestedByName    = approval.RequestedByUser.FullName,
            RequestedAt        = approval.RequestedAt,

            // Audit – reviewer (null while Pending)
            ReviewedByUserId   = approval.ReviewedByUserId,
            ReviewedByName     = approval.ReviewedByUser?.FullName,  // ?. safe: may be null
            ReviewedAt         = approval.ReviewedAt
        };
    }
}
