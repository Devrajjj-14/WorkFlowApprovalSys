// ── Namespace ─────────────────────────────────────────────────────────────────
// Places the User entity in the Models folder namespace so EF Core and services can find it
// Without this: User class is not in WorkflowApprovalApi.Models — DbSet<User> and imports fail
namespace WorkflowApprovalApi.Models;

// ── User Entity — maps to the Users table in MySQL ────────────────────────────
// Represents a registered person who can log in, own projects, and perform workflow actions
// Without this class: there is no user table shape — auth, JWT claims, and FK relationships break
public class User
{
    // Primary key — auto-generated integer identity column in the database
    // Without this: EF Core cannot uniquely identify rows — joins and updates fail
    public int Id { get; set; }

    // Display name shown in the UI (e.g. "Jane Smith" on audit labels)
    // Without this: responses and comments have no human-readable author name
    public string FullName { get; set; } = string.Empty;

    // Login identifier — must be unique; used for JWT subject and login lookup
    // Without this: users cannot sign in — AuthService has nothing to match against
    public string Email { get; set; } = string.Empty;

    // BCrypt hash of the password — never store or return plain text
    // Without this: login cannot verify credentials — every login attempt fails
    public string PasswordHash { get; set; } = string.Empty;

    // Role enum (Admin, Manager, Designer, Reviewer, Client) — drives authorization checks
    // Without this: every user has the same permissions — role-based access control breaks
    public UserRole Role { get; set; }

    // UTC/local timestamp when the account was registered
    // Without this: you cannot audit when accounts were created or sort users by signup date
    public DateTime CreatedAt { get; set; }

    // ── Navigation: Projects this user created ────────────────────────────────
    // One-to-many — EF Core uses this for .Include(u => u.CreatedProjects) queries
    // Without this: you cannot load all projects owned by a user from the User side
    public ICollection<Project> CreatedProjects { get; set; } = new List<Project>();

    // ── Navigation: Tasks assigned TO this user (they do the work) ────────────
    // Populated when a manager assigns a task via AssignedToUserId
    // Without this: "my tasks" queries cannot traverse from User → assigned WorkflowTasks
    public ICollection<WorkflowTask> AssignedTasks { get; set; } = new List<WorkflowTask>();

    // ── Navigation: Tasks this user CREATED and assigned to others ────────────
    // Populated from AssignedByUserId on WorkflowTask — audit trail of who delegated work
    // Without this: you cannot list tasks a manager has assigned from the User entity
    public ICollection<WorkflowTask> AssignedByTasks { get; set; } = new List<WorkflowTask>();

    // ── Navigation: Files this user uploaded to projects ───────────────────────
    // Links UploadedFile.UploadedByUserId back to this User
    // Without this: file upload history cannot be loaded via User navigation
    public ICollection<UploadedFile> UploadedFiles { get; set; } = new List<UploadedFile>();

    // ── Navigation: Project-level comments authored by this user ──────────────
    // Links Comment.UserId to this User for project discussion threads
    // Without this: EF cannot .Include() comment authors when loading from User
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    // ── Navigation: Task-level comments authored by this user ──────────────────
    // Links TaskComment.UserId to this User for task discussion threads
    // Without this: task comment author names require manual joins instead of navigation
    public ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();

    // ── Navigation: Approvals this user REQUESTED ─────────────────────────────
    // Links Approval.RequestedByUserId — who submitted work for review
    // Without this: "approvals I requested" cannot be queried from the User side
    public ICollection<Approval> RequestedApprovals { get; set; } = new List<Approval>();

    // ── Navigation: Approvals this user REVIEWED ──────────────────────────────
    // Links Approval.ReviewedByUserId — who approved/rejected/changes-requested
    // Without this: reviewer history cannot be loaded via User navigation properties
    public ICollection<Approval> ReviewedApprovals { get; set; } = new List<Approval>();
}
