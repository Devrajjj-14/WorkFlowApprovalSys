namespace WorkflowApprovalApi.Models;

public class WorkflowTask
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public Priority Priority { get; set; }

    // ── Audit: Assignment ────────────────────────────────────────────────────
    // AssignedToUserId  — the person who WILL DO the task (comes from form input).
    // AssignedByUserId  — the manager/admin who CREATED and assigned the task
    //                     (comes from JWT claim, never from form input).
    public int AssignedToUserId { get; set; }
    public int AssignedByUserId { get; set; }

    // ── Audit: Timestamps ────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; }

    // ── Audit: Last Update ───────────────────────────────────────────────────
    // Nullable — NULL means the task status has NEVER been changed since creation.
    // Set to the current user's ID every time UpdateStatusAsync is called.
    public int? UpdatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ── Navigation Properties ─────────────────────────────────────────────────
    public Project Project { get; set; } = null!;
    public User AssignedToUser { get; set; } = null!;
    public User AssignedByUser { get; set; } = null!;

    // Nullable because UpdatedByUserId is nullable — no updater on brand-new tasks.
    public User? UpdatedByUser { get; set; }

    public ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();
}
