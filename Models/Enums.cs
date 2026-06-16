// ── Namespace ─────────────────────────────────────────────────────────────────
// Groups all shared enum types under WorkflowApprovalApi.Models
// Without this: enum names collide with other projects and using statements break
namespace WorkflowApprovalApi.Models;

// ── UserRole — who a person is in the system ──────────────────────────────────
// Stored on the User table and checked by authorization logic (Admin-only routes etc.)
// Without this enum: roles would be raw strings — typos cause silent auth bugs
public enum UserRole
{
    // Full system access — can manage users, projects, and all settings
    // Without Admin: no one can perform privileged admin-only operations
    Admin,

    // Can create projects, assign tasks, and request approvals
    // Without Manager: no one can orchestrate workflow at the project level
    Manager,

    // Does the creative/production work assigned via tasks
    // Without Designer: the primary "doer" role has no typed identity
    Designer,

    // Reviews and approves/rejects submitted work and files
    // Without Reviewer: approval workflows lose their reviewer identity
    Reviewer,

    // External stakeholder who views progress and leaves comments
    // Without Client: client-facing access cannot be distinguished from internal roles
    Client
}

// ── ProjectStatus — lifecycle stage of a project ──────────────────────────────
// Drives UI badges, filtering, and which actions are allowed on a project
// Without this enum: project state is an unvalidated string — invalid transitions slip through
public enum ProjectStatus
{
    // Just created, not yet actively being worked on
    // Without Draft: new projects have no safe starting state
    Draft,

    // Active work is underway (tasks being completed)
    // Without InProgress: the system cannot distinguish idle drafts from active work
    InProgress,

    // Submitted for review — waiting on reviewer action
    // Without InReview: approval gates cannot be tied to a project state
    InReview,

    // Reviewer accepted the project deliverables
    // Without Approved: successful review outcomes cannot be recorded
    Approved,

    // Reviewer rejected the project deliverables
    // Without Rejected: failed review outcomes cannot be recorded
    Rejected,

    // All work finished and the project is closed successfully
    // Without Completed: finished projects look the same as in-progress ones
    Completed,

    // Project was abandoned or terminated before completion
    // Without Cancelled: cancelled projects cannot be distinguished from active ones
    Cancelled
}

// ── TaskStatus — progress of an individual workflow task ──────────────────────
// Used on the WorkflowTask entity and returned to the UI as a string in TaskResponse
// Without this enum: task boards cannot reliably filter or color-code by status
public enum TaskStatus
{
    // Created but not yet started by the assignee
    // Without Pending: new tasks have no default "waiting" state
    Pending,

    // Assignee is actively working on the task
    // Without InProgress: you cannot tell started tasks from untouched ones
    InProgress,

    // Assignee finished the task successfully
    // Without Completed: done tasks look the same as pending ones
    Completed,

    // Assignee cannot proceed — waiting on dependency or input
    // Without Blocked: stalled tasks cannot be flagged separately from Pending
    Blocked
}

// ── ApprovalStatus — outcome of a review request ────────────────────────────
// Tracks whether an approval is still open or has been decided by a reviewer
// Without this enum: approval records cannot express Pending vs Approved vs Rejected
public enum ApprovalStatus
{
    // Submitted and waiting for a reviewer to act
    // Without Pending: new approval requests have no initial state
    Pending,

    // Reviewer accepted the submission
    // Without Approved: successful approvals cannot be recorded
    Approved,

    // Reviewer rejected the submission outright
    // Without Rejected: failed approvals cannot be recorded
    Rejected,

    // Reviewer wants revisions before approving
    // Without ChangesRequested: "send back for fixes" has no distinct status
    ChangesRequested
}

// ── Priority — urgency level for a workflow task ──────────────────────────────
// Helps assignees and managers sort tasks by importance on dashboards
// Without this enum: priority is a free-form string — sorting and filtering break
public enum Priority
{
    // Can wait — handle when higher-priority work is done
    // Without Low: tasks cannot be marked as non-urgent
    Low,

    // Normal default urgency for most tasks
    // Without Medium: there is no standard baseline priority level
    Medium,

    // Needs immediate attention — should be done first
    // Without High: urgent tasks cannot be distinguished from routine ones
    High
}
