// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: Include, AnyAsync, FirstOrDefaultAsync, Add, SaveChangesAsync don't exist
using Microsoft.EntityFrameworkCore;

// Without this: AppDbContext is unknown — approval DB operations fail to compile
using WorkflowApprovalApi.Data;

// Without this: ApprovalCreateRequest, ApprovalResponse, ApprovalUpdateRequest are unknown
using WorkflowApprovalApi.DTOs;

// Without this: Approval, ApprovalStatus models are unknown — entity creation fails
using WorkflowApprovalApi.Models;

// Without this: IApprovalService interface is unknown — ApprovalService can't implement the contract
using WorkflowApprovalApi.Services.Interfaces;

namespace WorkflowApprovalApi.Services.Implementations;

// ApprovalService handles approval requests: create, list, approve, reject, request changes
// Called by ApprovalsController — enforces Pending-only updates and optional file linkage
// Without this class: all /api/approvals endpoints return 500 — approval workflow broken
public class ApprovalService : IApprovalService
{
    // EF Core context for Approvals, Projects, UploadedFiles, and Users
    // Without this field: no database access — approvals can't be created or updated
    private readonly AppDbContext _context;

    // Logger for approval lifecycle and reviewer actions
    // Without this field: approve/reject events aren't traceable in logs
    private readonly ILogger<ApprovalService> _logger;

    // Constructor — DI injects database and logger
    // Without this constructor: ApprovalService can't be constructed — app crashes on startup
    public ApprovalService(AppDbContext context, ILogger<ApprovalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────
    // Creates a Pending approval for a project, optionally linked to an uploaded file
    // Without this: POST /api/approvals does nothing — "Request Approval" modal broken
    public async Task<ApprovalResponse> CreateAsync(ApprovalCreateRequest request, int userId)
    {
        _logger.LogInformation(
            "Creating approval for project {ProjectId} by user {UserId}",
            request.ProjectId,
            userId);

        // Project must exist — approval is always scoped to a project
        // Without this: orphan Approval rows or unclear FK errors
        var projectExists = await _context.Projects.AnyAsync(p => p.Id == request.ProjectId);
        if (!projectExists)
        {
            throw new InvalidOperationException("Project not found.");
        }

        // If client attached a FileId, verify file belongs to same project
        // Without this block: approvals could reference files from other projects — wrong audit trail
        if (request.FileId.HasValue)
        {
            var fileExists = await _context.UploadedFiles
                .AnyAsync(f => f.Id == request.FileId && f.ProjectId == request.ProjectId);
            if (!fileExists)
            {
                throw new InvalidOperationException("File not found for this project.");
            }
        }

        // New approval starts Pending — reviewer fields null until acted upon
        // Without this: nothing to insert — CreateAsync completes with no DB row
        var approval = new Approval
        {
            ProjectId = request.ProjectId,
            FileId = request.FileId,
            RequestedByUserId = userId,
            Status = ApprovalStatus.Pending,
            Remarks = request.Remarks,
            RequestedAt = DateTime.UtcNow
        };

        // Persist approval request
        // Without Add + SaveChangesAsync: approval never saved
        _context.Approvals.Add(approval);
        await _context.SaveChangesAsync();

        // Reload with File, RequestedByUser, ReviewedByUser for full response mapping
        // Without LoadApprovalAsync: audit names missing in response
        var saved = await LoadApprovalAsync(approval.Id);
        _logger.LogInformation("Approval {ApprovalId} created successfully", saved!.Id);
        return MapToResponse(saved!);
    }

    // ── GetByProjectIdAsync ───────────────────────────────────────────────────
    // Lists all approvals for a project, newest request first
    // Without this: GET /api/projects/{id}/approvals returns empty — Approvals tab blank
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

    // ── ApproveAsync ──────────────────────────────────────────────────────────
    // Marks approval Approved — delegates to shared status updater
    // Without this: PUT /api/approvals/{id}/approve does nothing — Approve button broken
    public async Task<ApprovalResponse?> ApproveAsync(int id, ApprovalUpdateRequest request, int userId)
    {
        return await UpdateApprovalStatusAsync(id, ApprovalStatus.Approved, request, userId);
    }

    // ── RejectAsync ───────────────────────────────────────────────────────────
    // Marks approval Rejected — delegates to shared status updater
    // Without this: PUT /api/approvals/{id}/reject does nothing — Reject button broken
    public async Task<ApprovalResponse?> RejectAsync(int id, ApprovalUpdateRequest request, int userId)
    {
        return await UpdateApprovalStatusAsync(id, ApprovalStatus.Rejected, request, userId);
    }

    // ── RequestChangesAsync ───────────────────────────────────────────────────
    // Marks approval ChangesRequested — delegates to shared status updater
    // Without this: PUT /api/approvals/{id}/changes-requested broken — Request Changes button dead
    public async Task<ApprovalResponse?> RequestChangesAsync(int id, ApprovalUpdateRequest request, int userId)
    {
        return await UpdateApprovalStatusAsync(id, ApprovalStatus.ChangesRequested, request, userId);
    }

    // ── UpdateApprovalStatusAsync (private) ───────────────────────────────────
    // Shared logic for approve/reject/request-changes — enforces Pending-only transitions
    // Without this: three public methods would duplicate validation and save logic
    private async Task<ApprovalResponse?> UpdateApprovalStatusAsync(
        int id,
        ApprovalStatus status,
        ApprovalUpdateRequest request,
        int reviewerId)
    {
        // Load approval with navigation properties for response mapping
        // Without this: null return when id missing — controller maps to 404
        var approval = await LoadApprovalAsync(id);
        if (approval == null)
        {
            return null;
        }

        // Only Pending approvals can be acted on — prevents double-approve or editing closed items
        // Without this: reviewers could change Approved back to Rejected — workflow integrity lost
        if (approval.Status != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException("Only pending approvals can be updated.");
        }

        // Apply new status and record reviewer + timestamp
        // Without these assignments: status stays Pending forever
        approval.Status = status;
        approval.ReviewedByUserId = reviewerId;

        // Keep existing remarks if request sends blank — otherwise replace with new remarks
        // Without this ternary: empty remarks from UI would wipe original request text
        approval.Remarks = string.IsNullOrWhiteSpace(request.Remarks)
            ? approval.Remarks
            : request.Remarks;
        approval.ReviewedAt = DateTime.UtcNow;

        // Persist reviewer decision to database
        // Without SaveChangesAsync: approve/reject appears to succeed but DB unchanged
        await _context.SaveChangesAsync();
        _logger.LogInformation(
            "Approval {ApprovalId} updated to {Status} by reviewer {ReviewerId}",
            approval.Id,
            status,
            reviewerId);

        // Return updated DTO — navigation properties already loaded on approval entity
        // Without this return: controller has nothing to send to client
        return MapToResponse(approval);
    }

    // ── LoadApprovalAsync (private) ───────────────────────────────────────────
    // Centralised loader with File and user navigation properties for audit display
    // Without this: Create and Update would duplicate the same Include chain
    private async Task<Approval?> LoadApprovalAsync(int id)
    {
        return await _context.Approvals
            .Include(a => a.File)
            .Include(a => a.RequestedByUser)
            .Include(a => a.ReviewedByUser)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    // ── MapToResponse ─────────────────────────────────────────────────────────
    // Converts Approval entity to API DTO with requester and reviewer audit fields
    // Without this: endpoints expose wrong shape — frontend ApprovalResponse binding fails
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
