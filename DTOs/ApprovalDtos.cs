// ── Namespace ─────────────────────────────────────────────────────────────────
// Groups approval request/response DTOs used by ApprovalsController and ApprovalService
// Without this: approval endpoints cannot bind JSON or return consistent response shapes
namespace WorkflowApprovalApi.DTOs;

// ── ApprovalCreateRequest — body for POST /api/approvals ──────────────────────
// Requester id and RequestedAt are set server-side from JWT — not sent by the client
// Without this class: create approval endpoint has no typed body — requests fail to bind
public class ApprovalCreateRequest
{
    // Project being submitted for review — must reference an existing Project.Id
    // Without this: approvals save with ProjectId 0 — FK errors or orphaned records
    public int ProjectId { get; set; }

    // Optional file id when reviewing a specific upload; null for whole-project approval
    // Without this: file-specific approval requests cannot specify which version to review
    public int? FileId { get; set; }

    // Notes from the requester explaining what is being submitted or context for reviewers
    // Without this: remarks field on the request form is dropped on create
    public string Remarks { get; set; } = string.Empty;
}

// ── ApprovalUpdateRequest — body for PATCH when a reviewer acts on an approval ──
// Reviewer id, status, and ReviewedAt are set server-side — client may only update remarks
// Without this class: reviewer PATCH endpoint cannot bind optional remark updates
public class ApprovalUpdateRequest
{
    // Optional updated remarks from the reviewer (rejection reason, change notes, etc.)
    // Without this: reviewers cannot append or edit remarks when approving/rejecting
    public string? Remarks { get; set; }
}

// ── ApprovalResponse — JSON returned for approval list and detail endpoints ───
// Includes requester and reviewer audit fields resolved from User navigation joins
// Without this class: frontend approval pages fail to deserialize API responses
public class ApprovalResponse
{
    // Approval primary key — used to PATCH status when a reviewer acts
    // Without this: UI cannot call update endpoints for a specific approval row
    public int Id { get; set; }

    // Project this approval belongs to — for linking back to project context
    // Without this: approval list cannot group or filter by project
    public int ProjectId { get; set; }

    // Linked file id when approval targets one upload; null for project-wide approvals
    // Without this: UI cannot show which file version is under review
    public int? FileId { get; set; }

    // Current ApprovalStatus as string (Pending, Approved, Rejected, ChangesRequested)
    // Without this: status badges and reviewer action buttons cannot reflect state
    public string Status { get; set; } = string.Empty;

    // Combined requester/reviewer notes shown on approval detail
    // Without this: remarks section on approval cards is always empty
    public string Remarks { get; set; } = string.Empty;

    // ── Audit: Who Requested ─────────────────────────────────────────────────
    // Always populated — every approval has a requester.
    // User id of who submitted the approval — from Approval.RequestedByUserId
    // Without this: "Requested by" audit line cannot identify the submitter
    public int RequestedByUserId { get; set; }

    // Full name of requester — from RequestedByUser navigation
    // Without this: UI shows numeric id instead of "Requested by Jane Smith"
    public string RequestedByName { get; set; } = string.Empty; // Full name from Users table

    // When the approval was submitted — from Approval.RequestedAt
    // Without this: timeline cannot show when the review was requested
    public DateTime RequestedAt { get; set; }

    // ── Audit: Who Reviewed (Approved/Rejected/ChangesRequested) ─────────────
    // Nullable — null means this approval is still Pending (not yet reviewed).
    // ReviewedByName will be shown as "Approved by / Rejected by / Changes by: {name}".
    // Reviewer user id — null while Status is still Pending
    // Without this: UI cannot tell if an approval awaits review vs is decided
    public int? ReviewedByUserId { get; set; }

    // Reviewer full name — null until a reviewer acts on the approval
    // Without this: "Approved by" / "Rejected by" labels have no name to show
    public string? ReviewedByName { get; set; }                 // null when still Pending

    // When the reviewer made a decision — null while still Pending
    // Without this: "Reviewed on" date and turnaround-time metrics are unavailable
    public DateTime? ReviewedAt { get; set; }
}
