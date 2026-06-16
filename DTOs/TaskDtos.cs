namespace WorkflowApprovalApi.DTOs;

public class TaskCreateRequest
{
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AssignedToUserId { get; set; }
    public string Priority { get; set; } = string.Empty;
}

public class TaskUpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class TaskResponse
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int AssignedToUserId { get; set; }

    // ── Audit: Creation / Assignment ─────────────────────────────────────────
    // AssignedByUserId/Name identifies who created and assigned the task.
    // These are always populated — every task has a creator.
    public int AssignedByUserId { get; set; }
    public string AssignedByName { get; set; } = string.Empty;  // Full name from Users table
    public DateTime CreatedAt { get; set; }

    // ── Audit: Last Status Update ─────────────────────────────────────────────
    // Nullable — null means the task status was never changed after creation.
    // The UI uses: if UpdatedByUserId has value → show "Last updated by", else "Created by".
    public int? UpdatedByUserId { get; set; }
    public string? UpdatedByName { get; set; }                  // null when never updated
    public DateTime UpdatedAt { get; set; }
}
