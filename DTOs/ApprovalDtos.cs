namespace WorkflowApprovalApi.DTOs;

public class ApprovalCreateRequest
{
    public int ProjectId { get; set; }
    public int? FileId { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

public class ApprovalUpdateRequest
{
    public string? Remarks { get; set; }
}

public class ApprovalResponse
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? FileId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;

    // ── Audit: Who Requested ─────────────────────────────────────────────────
    // Always populated — every approval has a requester.
    public int RequestedByUserId { get; set; }
    public string RequestedByName { get; set; } = string.Empty; // Full name from Users table
    public DateTime RequestedAt { get; set; }

    // ── Audit: Who Reviewed (Approved/Rejected/ChangesRequested) ─────────────
    // Nullable — null means this approval is still Pending (not yet reviewed).
    // ReviewedByName will be shown as "Approved by / Rejected by / Changes by: {name}".
    public int? ReviewedByUserId { get; set; }
    public string? ReviewedByName { get; set; }                 // null when still Pending
    public DateTime? ReviewedAt { get; set; }
}
