// ── Imports ───────────────────────────────────────────────────────────────────

// Without this: DbContext, DbContextOptions, DbSet, ModelBuilder don't exist — EF Core setup fails to compile
using Microsoft.EntityFrameworkCore;

// Without this: User, Project, WorkflowTask etc. are unknown — DbSet<> declarations fail to compile
using WorkflowApprovalApi.Models;

namespace WorkflowApprovalApi.Data;

// AppDbContext is the single EF Core database gateway for the entire API
// Every service that reads or writes data gets this injected via DI (registered in Program.cs)
// Without this class: no table mapping, no queries, no migrations target — the database layer is gone
public class AppDbContext : DbContext
{
    // Constructor receives DbContextOptions from DI — contains the MySQL connection string and provider
    // base(options) passes those options up to DbContext so EF knows how to connect
    // Without this constructor: DI can't create AppDbContext — every service that needs the DB crashes on startup
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // ── DbSet Properties — one per database table ─────────────────────────────

    // Maps to the Users table — login, registration, and role checks all query this
    // Without this: no way to read or write users — auth and every user lookup fails
    public DbSet<User> Users => Set<User>();

    // Maps to the Projects table — project CRUD and workflow status live here
    // Without this: project endpoints have no data source — all project operations fail
    public DbSet<Project> Projects => Set<Project>();

    // Maps to the Tasks table (WorkflowTask model — "Tasks" avoids clash with System.Threading.Tasks)
    // Without this: task assignment and status updates have nowhere to persist
    public DbSet<WorkflowTask> Tasks => Set<WorkflowTask>();

    // Maps to the UploadedFiles table — file metadata (path, name, uploader) stored here; bytes live on disk
    // Without this: file upload/download endpoints can't save or query file records
    public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();

    // Maps to the Comments table — project-level discussion threads
    // Without this: comment endpoints can't load or save project comments
    public DbSet<Comment> Comments => Set<Comment>();

    // Maps to the TaskComments table — comments tied to a specific task, not the whole project
    // Without this: task comment features have no persistence layer
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();

    // Maps to the Approvals table — approval requests and reviewer decisions
    // Without this: the approval workflow can't be stored or queried
    public DbSet<Approval> Approvals => Set<Approval>();

    // ── Model Configuration — relationships, indexes, enum storage ──────────
    // Called once when EF builds the model — defines how C# classes map to MySQL tables
    // Without this override: defaults apply — wrong delete behavior, duplicate emails, enums stored as ints
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Runs DbContext's built-in configuration first (keys, conventions, etc.)
        // Without this: base setup is skipped — some EF defaults may not apply correctly
        base.OnModelCreating(modelBuilder);

        // ── User Entity ───────────────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            // Unique index on Email — prevents two accounts with the same email
            // Without this: duplicate emails can be inserted — login ambiguity and data corruption
            entity.HasIndex(u => u.Email).IsUnique();

            // Stores UserRole enum as string ("Designer") in MySQL instead of integer (2)
            // Without this: enum values are stored as numbers — harder to read in SQL and can mismatch JSON
            entity.Property(u => u.Role).HasConversion<string>();
        });

        // ── Project Entity ──────────────────────────────────────────────────────
        modelBuilder.Entity<Project>(entity =>
        {
            // Stores ProjectStatus enum as string in the database
            // Without this: status is stored as int — inconsistent with API JSON that sends "Active" etc.
            entity.Property(p => p.Status).HasConversion<string>();

            // CreatedByUser — every project must have a creator; Restrict prevents deleting a user who created projects
            // Without this: EF may infer wrong FK or use Cascade — deleting a user could wipe all their projects
            entity.HasOne(p => p.CreatedByUser)
                .WithMany(u => u.CreatedProjects)
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // UpdatedByUser is nullable — not every project has been edited after creation.
            // HasForeignKey points to the UpdatedByUserId column added in the migration.
            // IsRequired(false) allows NULL when nobody has updated the project yet
            // Restrict prevents deleting a user from silently breaking project history
            // Without this: UpdatedByUserId has no relationship — joins and Include() for editor name fail
            entity.HasOne(p => p.UpdatedByUser)
                .WithMany()
                .HasForeignKey(p => p.UpdatedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── WorkflowTask Entity ─────────────────────────────────────────────────
        modelBuilder.Entity<WorkflowTask>(entity =>
        {
            // Task status enum stored as string ("InProgress", "Done", etc.)
            // Without this: status values in DB are opaque integers
            entity.Property(t => t.Status).HasConversion<string>();

            // Priority enum stored as string ("High", "Medium", "Low")
            // Without this: priority is stored as int — same readability problem as status
            entity.Property(t => t.Priority).HasConversion<string>();

            // Task belongs to one Project; deleting a project deletes all its tasks (Cascade)
            // Without this: orphaned tasks could remain when a project is deleted
            entity.HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // AssignedToUser — who must do the work; Restrict so deleting a user doesn't delete tasks
            // Without this: assignee navigation and user-task lists break
            entity.HasOne(t => t.AssignedToUser)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // AssignedByUser — who created/assigned the task; separate from assignee for audit
            // Without this: "assigned by" history is lost in queries
            entity.HasOne(t => t.AssignedByUser)
                .WithMany(u => u.AssignedByTasks)
                .HasForeignKey(t => t.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // UpdatedByUser is nullable — brand-new tasks have never been status-updated.
            // Without this: last editor on a task can't be loaded via navigation property
            entity.HasOne(t => t.UpdatedByUser)
                .WithMany()
                .HasForeignKey(t => t.UpdatedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── UploadedFile Entity ─────────────────────────────────────────────────
        modelBuilder.Entity<UploadedFile>(entity =>
        {
            // File belongs to a project; deleting project removes file records (Cascade)
            // Physical files on disk are handled separately in FileService — this is metadata only
            // Without this: file rows orphan when project is deleted
            entity.HasOne(f => f.Project)
                .WithMany(p => p.Files)
                .HasForeignKey(f => f.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Who uploaded the file — Restrict so user deletion doesn't wipe upload history unexpectedly
            // Without this: uploader name/path queries fail
            entity.HasOne(f => f.UploadedByUser)
                .WithMany(u => u.UploadedFiles)
                .HasForeignKey(f => f.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Comment Entity (project-level) ──────────────────────────────────────
        modelBuilder.Entity<Comment>(entity =>
        {
            // Comment on a project — Cascade when project deleted
            // Without this: comments survive after project deletion — stale data
            entity.HasOne(c => c.Project)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Author of the comment — Restrict on user delete
            // Without this: can't Include(c => c.User) for display names
            entity.HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── TaskComment Entity ────────────────────────────────────────────────
        modelBuilder.Entity<TaskComment>(entity =>
        {
            // Comment tied to a specific task — Cascade when task deleted
            // Without this: task comments orphan or wrong FK behavior
            entity.HasOne(tc => tc.Task)
                .WithMany(t => t.TaskComments)
                .HasForeignKey(tc => tc.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Author — same pattern as project Comment
            // Without this: task comment author can't be loaded
            entity.HasOne(tc => tc.User)
                .WithMany(u => u.TaskComments)
                .HasForeignKey(tc => tc.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Approval Entity ───────────────────────────────────────────────────
        modelBuilder.Entity<Approval>(entity =>
        {
            // ApprovalStatus enum as string ("Pending", "Approved", "Rejected")
            // Without this: approval status stored as int in MySQL
            entity.Property(a => a.Status).HasConversion<string>();

            // Approval belongs to a project — Cascade on project delete
            // Without this: approval rows left behind when project removed
            entity.HasOne(a => a.Project)
                .WithMany(p => p.Approvals)
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Optional link to a specific file being approved — SetNull if file deleted, approval row stays
            // Without this: deleting a file might delete approvals or break FK constraints
            entity.HasOne(a => a.File)
                .WithMany(f => f.Approvals)
                .HasForeignKey(a => a.FileId)
                .OnDelete(DeleteBehavior.SetNull);

            // Who requested the approval — Restrict on user delete
            // Without this: requester info missing from approval DTOs
            entity.HasOne(a => a.RequestedByUser)
                .WithMany(u => u.RequestedApprovals)
                .HasForeignKey(a => a.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Reviewer — nullable until someone approves/rejects; Restrict on user delete
            // Without this: reviewer name and audit trail break
            entity.HasOne(a => a.ReviewedByUser)
                .WithMany(u => u.ReviewedApprovals)
                .HasForeignKey(a => a.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
