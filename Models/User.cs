namespace WorkflowApprovalApi.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Project> CreatedProjects { get; set; } = new List<Project>();
    public ICollection<WorkflowTask> AssignedTasks { get; set; } = new List<WorkflowTask>();
    public ICollection<WorkflowTask> AssignedByTasks { get; set; } = new List<WorkflowTask>();
    public ICollection<UploadedFile> UploadedFiles { get; set; } = new List<UploadedFile>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Approval> RequestedApprovals { get; set; } = new List<Approval>();
    public ICollection<Approval> ReviewedApprovals { get; set; } = new List<Approval>();
}
