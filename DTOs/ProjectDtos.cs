namespace WorkflowApprovalApi.DTOs;

public class ProjectCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ProjectUpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class ProjectResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // ── Audit: Creation ──────────────────────────────────────────────────────
    // These two fields are always populated — every project has a creator.
    public int CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = string.Empty;   // Full name from Users table
    public DateTime CreatedAt { get; set; }

    // ── Audit: Last Update ───────────────────────────────────────────────────
    // These are nullable — null means the project was never edited after creation.
    // The UI uses: if UpdatedByUserId has value → show "Last updated by", else "Created by".
    public int? UpdatedByUserId { get; set; }
    public string? UpdatedByName { get; set; }                  // null when never updated
    public DateTime UpdatedAt { get; set; }
}
