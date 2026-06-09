namespace WorkflowApprovalApi.Models;

public enum UserRole
{
    Admin,
    Manager,
    Designer,
    Reviewer,
    Client
}

public enum ProjectStatus
{
    Draft,
    InProgress,
    InReview,
    Approved,
    Rejected,
    Completed,
    Cancelled
}

public enum TaskStatus
{
    Pending,
    InProgress,
    Completed,
    Blocked
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    ChangesRequested
}

public enum Priority
{
    Low,
    Medium,
    High
}
