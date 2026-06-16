// ── Namespace ─────────────────────────────────────────────────────────────────
// Declares WorkflowTask in the Models namespace used by EF Core and services
// Without this: DbSet<WorkflowTask> and task service imports fail to compile
namespace WorkflowApprovalApi.Models;

// ── WorkflowTask Entity — maps to the Tasks table ─────────────────────────────
// Named WorkflowTask to avoid clashing with System.Threading.Tasks.Task
// Represents a unit of work assigned to a user within a project
// Without this class: task CRUD, status updates, and task comments have no persistence layer
public class WorkflowTask
{
    // Primary key — unique identifier for each task row
    // Without this: EF Core cannot save or fetch individual tasks
    public int Id { get; set; }

    // Foreign key — which project this task belongs to
    // Without this: tasks float with no parent — project task lists return empty
    public int ProjectId { get; set; }

    // Short headline for the task (shown in kanban boards and detail views)
    // Without this: tasks have no title in API responses or the UI
    public string Title { get; set; } = string.Empty;

    // Detailed instructions or notes for the assignee
    // Without this: create/update payloads have nowhere to store task body text
    public string Description { get; set; } = string.Empty;

    // Current progress state — defaults to Pending when a task is first created
    // Without this: task boards cannot show Pending/InProgress/Completed/Blocked states
    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    // Urgency level (Low, Medium, High) — set from TaskCreateRequest.Priority string
    // Without this: priority sorting and filtering on task lists do not work
    public Priority Priority { get; set; }

    // ── Audit: Assignment ────────────────────────────────────────────────────
    // AssignedToUserId  — the person who WILL DO the task (comes from form input).
    // AssignedByUserId  — the manager/admin who CREATED and assigned the task
    //                     (comes from JWT claim, never from form input).
    // Foreign key to Users.Id — the assignee who must complete the work
    // Without this: "my tasks" and assignment notifications have no target user
    public int AssignedToUserId { get; set; }

    // Foreign key to Users.Id — the manager/admin who created and delegated the task
    // Without this: audit trail loses who assigned the work — AssignedByName is empty
    public int AssignedByUserId { get; set; }

    // ── Audit: Timestamps ────────────────────────────────────────────────────
    // When the task row was first inserted into the database
    // Without this: TaskResponse.CreatedAt is wrong — UI cannot show when task was created
    public DateTime CreatedAt { get; set; }

    // ── Audit: Last Update ───────────────────────────────────────────────────
    // Nullable — NULL means the task status has NEVER been changed since creation.
    // Set to the current user's ID every time UpdateStatusAsync is called.
    // Foreign key to Users.Id — null until someone changes task status
    // Without this: "Last updated by" on task cards cannot identify the editor
    public int? UpdatedByUserId { get; set; }

    // Timestamp of the last status change (or default when never updated)
    // Without this: TaskResponse.UpdatedAt is missing — relative "updated 2h ago" labels break
    public DateTime UpdatedAt { get; set; }

    // ── Navigation Properties ─────────────────────────────────────────────────
    // Parent project this task belongs to — loaded via ProjectId foreign key
    // Without this: .Include(t => t.Project) fails — project name on task detail is unavailable
    public Project Project { get; set; } = null!;

    // User who is responsible for doing the task — loaded via AssignedToUserId
    // Without this: assignee full name requires a manual Users join in every query
    public User AssignedToUser { get; set; } = null!;

    // User who created and assigned the task — loaded via AssignedByUserId
    // Without this: AssignedByName in TaskResponse cannot be populated from navigation
    public User AssignedByUser { get; set; } = null!;

    // Nullable because UpdatedByUserId is nullable — no updater on brand-new tasks.
    // User who last changed task status — null until the first status update
    // Without this: UpdatedByName stays null even when UpdatedByUserId is set
    public User? UpdatedByUser { get; set; }

    // All comments attached to this specific task (task-level discussion thread)
    // Without this: task comment lists cannot be eager-loaded with the task entity
    public ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();
}
