namespace WorkflowApprovalApi.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User CreatedByUser { get; set; } = null!;
    public ICollection<WorkflowTask> Tasks { get; set; } = new List<WorkflowTask>();
    public ICollection<UploadedFile> Files { get; set; } = new List<UploadedFile>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
}
