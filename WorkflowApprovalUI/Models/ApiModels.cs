namespace WorkflowApprovalUI.Models;

// ── Auth ──────────────────────────────────────────────
public class LoginViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Designer";
}

public class AuthResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

// ── Projects ──────────────────────────────────────────
public class ProjectResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProjectCreateViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

// ── Tasks ─────────────────────────────────────────────
public class TaskResponse
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int AssignedToUserId { get; set; }
    public int AssignedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TaskCreateViewModel
{
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AssignedToUserId { get; set; }
    public string Priority { get; set; } = "Medium";
}

// ── Approvals ─────────────────────────────────────────
public class ApprovalResponse
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? FileId { get; set; }
    public int RequestedByUserId { get; set; }
    public int? ReviewedByUserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class ApprovalCreateViewModel
{
    public int ProjectId { get; set; }
    public int? FileId { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

// ── Comments ──────────────────────────────────────────
public class CommentResponse
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ── Files ─────────────────────────────────────────────
public class FileListResponse
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public int UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; }
}

// ── Project Detail (composite) ────────────────────────
public class ProjectDetailViewModel
{
    public ProjectResponse Project { get; set; } = null!;
    public List<TaskResponse> Tasks { get; set; } = new();
    public List<ApprovalResponse> Approvals { get; set; } = new();
    public List<CommentResponse> Comments { get; set; } = new();
    public List<FileListResponse> Files { get; set; } = new();
}
