using Microsoft.EntityFrameworkCore;
using WorkflowApprovalApi.Models;

namespace WorkflowApprovalApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<WorkflowTask> Tasks => Set<WorkflowTask>();
    public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<Approval> Approvals => Set<Approval>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Role).HasConversion<string>();
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(p => p.Status).HasConversion<string>();
            entity.HasOne(p => p.CreatedByUser)
                .WithMany(u => u.CreatedProjects)
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkflowTask>(entity =>
        {
            entity.Property(t => t.Status).HasConversion<string>();
            entity.Property(t => t.Priority).HasConversion<string>();
            entity.HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(t => t.AssignedToUser)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.AssignedByUser)
                .WithMany(u => u.AssignedByTasks)
                .HasForeignKey(t => t.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UploadedFile>(entity =>
        {
            entity.HasOne(f => f.Project)
                .WithMany(p => p.Files)
                .HasForeignKey(f => f.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(f => f.UploadedByUser)
                .WithMany(u => u.UploadedFiles)
                .HasForeignKey(f => f.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasOne(c => c.Project)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TaskComment>(entity =>
        {
            entity.HasOne(tc => tc.Task)
                .WithMany(t => t.TaskComments)
                .HasForeignKey(tc => tc.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(tc => tc.User)
                .WithMany(u => u.TaskComments)
                .HasForeignKey(tc => tc.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Approval>(entity =>
        {
            entity.Property(a => a.Status).HasConversion<string>();
            entity.HasOne(a => a.Project)
                .WithMany(p => p.Approvals)
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.File)
                .WithMany(f => f.Approvals)
                .HasForeignKey(a => a.FileId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(a => a.RequestedByUser)
                .WithMany(u => u.RequestedApprovals)
                .HasForeignKey(a => a.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(a => a.ReviewedByUser)
                .WithMany(u => u.ReviewedApprovals)
                .HasForeignKey(a => a.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
