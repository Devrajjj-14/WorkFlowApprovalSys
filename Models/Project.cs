namespace WorkflowApprovalApi.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;

    // ── Audit: Creation ──────────────────────────────────────────────────────
    // The user who originally created this project.
    // Populated from the JWT claim on POST /api/projects.
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    // ── Audit: Last Update ───────────────────────────────────────────────────
    // Nullable — NULL means the project has NEVER been updated since creation.
    // Set to the current user's ID every time UpdateStatusAsync is called.
    // Used by the UI to decide whether to show "Created by" or "Last updated by".
    public int? UpdatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ── Navigation Properties ─────────────────────────────────────────────────
    // EF Core uses these to JOIN the Users table when .Include() is called.
    public User CreatedByUser { get; set; } = null!;

    // Nullable because UpdatedByUserId is nullable — no updater exists on new records.
    public User? UpdatedByUser { get; set; }

    public ICollection<WorkflowTask> Tasks { get; set; } = new List<WorkflowTask>();
    public ICollection<UploadedFile> Files { get; set; } = new List<UploadedFile>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
}
