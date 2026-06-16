// ── Namespace ─────────────────────────────────────────────────────────────────
// Keeps the Project entity under WorkflowApprovalApi.Models alongside other EF entities
// Without this: Project is unknown to AppDbContext and service layer imports fail
namespace WorkflowApprovalApi.Models;

// ── Project Entity — maps to the Projects table ───────────────────────────────
// Top-level container for tasks, files, comments, and approvals in the workflow system
// Without this class: the entire project-centric workflow has no database representation
public class Project
{
    // Primary key — unique identifier for each project row
    // Without this: EF Core cannot persist or retrieve individual projects
    public int Id { get; set; }

    // Short human-readable title shown in project lists and headers
    // Without this: projects are anonymous — UI and API responses have no label
    public string Name { get; set; } = string.Empty;

    // Longer explanation of scope, goals, or deliverables for the project
    // Without this: detail pages and create forms have nowhere to store description text
    public string Description { get; set; } = string.Empty;

    // Current lifecycle stage (Draft, InProgress, InReview, etc.) — defaults to Draft on create
    // Without this: project state is unknown — status filters and workflow gates break
    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;

    // ── Audit: Creation ──────────────────────────────────────────────────────
    // The user who originally created this project.
    // Populated from the JWT claim on POST /api/projects.
    // Foreign key to Users.Id — identifies who clicked "Create Project"
    // Without this: audit trail cannot record project ownership — "Created by" is blank
    public int CreatedByUserId { get; set; }

    // Timestamp when the project row was first inserted
    // Without this: "Created at" dates are missing from ProjectResponse and the UI
    public DateTime CreatedAt { get; set; }

    // ── Audit: Last Update ───────────────────────────────────────────────────
    // Nullable — NULL means the project has NEVER been updated since creation.
    // Set to the current user's ID every time UpdateStatusAsync is called.
    // Used by the UI to decide whether to show "Created by" or "Last updated by".
    // Foreign key to Users.Id — null when only the initial create happened
    // Without this: you cannot tell WHO last changed the project status
    public int? UpdatedByUserId { get; set; }

    // Timestamp of the most recent status update (or default/min when never updated)
    // Without this: "Last updated at" cannot be shown on project detail screens
    public DateTime UpdatedAt { get; set; }

    // ── Navigation Properties ─────────────────────────────────────────────────
    // EF Core uses these to JOIN the Users table when .Include() is called.
    // Required navigation to the User who created this project — loaded via CreatedByUserId
    // Without this: .Include(p => p.CreatedByUser) fails — CreatedByName cannot be resolved
    public User CreatedByUser { get; set; } = null!;

    // Nullable because UpdatedByUserId is nullable — no updater exists on new records.
    // Optional navigation to the User who last updated status — null until first edit
    // Without this: UpdatedByName requires a manual join instead of EF Include
    public User? UpdatedByUser { get; set; }

    // All workflow tasks belonging to this project — one project has many tasks
    // Without this: tasks cannot be loaded as a collection when querying a project
    public ICollection<WorkflowTask> Tasks { get; set; } = new List<WorkflowTask>();

    // All uploaded files attached to this project (design assets, documents, etc.)
    // Without this: file lists for a project require separate queries without navigation
    public ICollection<UploadedFile> Files { get; set; } = new List<UploadedFile>();

    // Project-level discussion comments (not tied to a specific task)
    // Without this: Comment.ProjectId navigation back to Project is one-way only
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    // Approval requests submitted against this project (optionally linked to a file)
    // Without this: approval history cannot be eager-loaded with the project entity
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
}
