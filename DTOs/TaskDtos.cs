// ── Namespace ─────────────────────────────────────────────────────────────────
// Groups task-related DTOs used by TasksController and TaskService
// Without this: task CRUD endpoints cannot bind requests or return typed responses
namespace WorkflowApprovalApi.DTOs;

// ── TaskCreateRequest — body for POST /api/tasks ──────────────────────────────
// Client supplies task details and assignee — AssignedByUserId comes from JWT, not this DTO
// Without this class: create task endpoint cannot deserialize the form payload
public class TaskCreateRequest
{
    // Which project the new task belongs to — must reference an existing Project.Id
    // Without this: tasks are created with ProjectId 0 — FK constraint or orphan tasks result
    public int ProjectId { get; set; }

    // Short task title from the create form
    // Without this: new tasks save with empty titles — kanban cards show nothing
    public string Title { get; set; } = string.Empty;

    // Detailed instructions for the assignee
    // Without this: task body text from the form is lost on create
    public string Description { get; set; } = string.Empty;

    // User id of the person who will do the work — selected from a dropdown on the form
    // Without this: tasks have no assignee — "my tasks" queries return nothing for anyone
    public int AssignedToUserId { get; set; }

    // Priority as string ("Low", "Medium", "High") — parsed to Priority enum in TaskService
    // Without this: every task gets default enum value — priority sorting breaks
    public string Priority { get; set; } = string.Empty;
}

// ── TaskUpdateStatusRequest — body for PATCH task status endpoints ─────────────
// Single status string parsed to TaskStatus enum — updater id comes from JWT server-side
// Without this class: status change endpoint cannot read the desired new status
public class TaskUpdateStatusRequest
{
    // Target status name (Pending, InProgress, Completed, Blocked)
    // Without this: UpdateStatusAsync gets empty string — enum parse fails with 400
    public string Status { get; set; } = string.Empty;
}

// ── TaskResponse — JSON returned for task list and detail endpoints ─────────────
// Maps WorkflowTask entity plus joined User names for audit display
// Without this class: frontend task pages fail to deserialize API responses
public class TaskResponse
{
    // Task primary key — used in URLs and task comment API calls
    // Without this: UI cannot open task detail or post task-level comments
    public int Id { get; set; }

    // Parent project id — links task cards back to their project context
    // Without this: task list cannot filter or group by project
    public int ProjectId { get; set; }

    // Task headline shown on boards and detail header
    // Without this: task title column is blank in the UI
    public string Title { get; set; } = string.Empty;

    // Full task description body
    // Without this: task detail page description section is empty
    public string Description { get; set; } = string.Empty;

    // Current TaskStatus as string for badges and filters
    // Without this: status colors and kanban columns cannot reflect task state
    public string Status { get; set; } = string.Empty;

    // Priority as string for display and sorting in the UI
    // Without this: priority labels on task cards are missing
    public string Priority { get; set; } = string.Empty;

    // Assignee user id — who must complete the task
    // Without this: "assigned to" filtering and user-specific task views break
    public int AssignedToUserId { get; set; }

    // ── Audit: Creation / Assignment ─────────────────────────────────────────
    // AssignedByUserId/Name identifies who created and assigned the task.
    // These are always populated — every task has a creator.
    // Manager/admin who created the task — from WorkflowTask.AssignedByUserId
    // Without this: audit trail loses who delegated the work
    public int AssignedByUserId { get; set; }

    // Full name of the assigner — from AssignedByUser navigation
    // Without this: UI shows id instead of "Assigned by John Doe"
    public string AssignedByName { get; set; } = string.Empty;  // Full name from Users table

    // When the task was created — from WorkflowTask.CreatedAt
    // Without this: "Created on" timestamp missing from task detail
    public DateTime CreatedAt { get; set; }

    // ── Audit: Last Status Update ─────────────────────────────────────────────
    // Nullable — null means the task status was never changed after creation.
    // The UI uses: if UpdatedByUserId has value → show "Last updated by", else "Created by".
    // User id of whoever last changed status — null if never updated
    // Without this: UI cannot switch between "Created by" and "Last updated by" labels
    public int? UpdatedByUserId { get; set; }

    // Full name of last status editor — null when never updated
    // Without this: "Last updated by" has no display name
    public string? UpdatedByName { get; set; }                  // null when never updated

    // Timestamp of last status change — from WorkflowTask.UpdatedAt
    // Without this: "updated ago" relative times are unavailable on task cards
    public DateTime UpdatedAt { get; set; }
}
