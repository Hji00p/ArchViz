using ArchViz.Domain.Communication;
using ArchViz.Domain.Orders;
using ArchViz.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ArchViz.Domain.Scenes;
using ArchViz.Domain.Estimation;

namespace ArchViz.Infrastructure.Persistence;

public sealed class ArchVizDbContext : IdentityDbContext<AppUser>
{
    public ArchVizDbContext(DbContextOptions<ArchVizDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Scene> Scenes => Set<Scene>();
    public DbSet<SceneAssignment> SceneAssignments
    => Set<SceneAssignment>();
    public DbSet<SceneRequirementsRevision> SceneRequirementsRevisions
    => Set<SceneRequirementsRevision>();
    public DbSet<MLModelVersion> MLModelVersions => Set<MLModelVersion>();
    public DbSet<MLPrediction> MLPredictions => Set<MLPrediction>();
    public DbSet<ScenePlanApproval> ScenePlanApprovals
    => Set<ScenePlanApproval>();

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<TimeEntryChange> TimeEntryChanges => Set<TimeEntryChange>();

    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<RenderVersion> RenderVersions => Set<RenderVersion>();
    public DbSet<RevisionRequest> RevisionRequests => Set<RevisionRequest>();

    public DbSet<OrderActionRequest> OrderActionRequests => Set<OrderActionRequest>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Scene>(scene =>
        {
            scene.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            scene.HasOne<Order>()
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SceneAssignment>(assignment =>
        {
            assignment.Property(x => x.VisualizerId)
                .IsRequired();

            assignment.HasOne<Scene>()
                .WithMany()
                .HasForeignKey(x => x.SceneId)
                .OnDelete(DeleteBehavior.Restrict);

            assignment.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.VisualizerId)
                .OnDelete(DeleteBehavior.Restrict);

            assignment.HasIndex(x => x.SceneId)
                .IsUnique()
                .HasFilter("\"EndedAt\" IS NULL");
        });

        builder.Entity<SceneRequirementsRevision>(revision =>
        {
            revision.Property(x => x.RequirementsSnapshot)
                .HasMaxLength(10000)
                .IsRequired();

            revision.Property(x => x.CreatedById)
                .IsRequired();

            revision.HasOne<Scene>()
                .WithMany()
                .HasForeignKey(x => x.SceneId)
                .OnDelete(DeleteBehavior.Restrict);

            revision.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            revision.HasIndex(x => new
            {
                x.SceneId,
                x.VersionNumber
            })
                .IsUnique();
        });

        builder.Entity<SceneAssessment>(assessment =>
        {
            assessment.HasOne<SceneRequirementsRevision>()
                .WithMany()
                .HasForeignKey(x => x.RequirementsRevisionId)
                .OnDelete(DeleteBehavior.Restrict);

            assessment.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.AssessedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MLModelVersion>(model =>
        {
            model.HasIndex(x => x.Version).IsUnique();
        });

        builder.Entity<MLPrediction>(prediction =>
        {
            prediction.HasOne<SceneAssessment>()
                .WithMany()
                .HasForeignKey(x => x.AssessmentId)
                .OnDelete(DeleteBehavior.Restrict);

            prediction.HasOne<MLModelVersion>()
                .WithMany()
                .HasForeignKey(x => x.ModelVersionId)
                .OnDelete(DeleteBehavior.Restrict);

            prediction.HasIndex(x => new { x.AssessmentId, x.ModelVersionId })
                .IsUnique();
        });

        builder.Entity<OrderActionRequest>(request =>
        {
            request.HasOne<Order>().WithMany()
                .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            request.Property(x => x.Reason).HasMaxLength(2000);
            request.HasIndex(x => new { x.OrderId, x.Kind }).IsUnique()
                .HasFilter("\"Status\" = 0");
        });

        builder.Entity<ChatMessage>(message =>
        {
            message.ToTable(table => table.HasCheckConstraint(
                "CK_ChatMessages_SingleContext", "(\"OrderId\" IS NULL) <> (\"SceneId\" IS NULL)"));
            message.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
            message.HasOne<Scene>().WithMany().HasForeignKey(x => x.SceneId)
                .OnDelete(DeleteBehavior.Restrict);
            message.HasOne<AppUser>().WithMany().HasForeignKey(x => x.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            message.Property(x => x.Content).HasMaxLength(4000);
        });

        builder.Entity<Notification>(notification =>
        {
            notification.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            notification.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
            notification.HasOne<Scene>().WithMany().HasForeignKey(x => x.SceneId)
                .OnDelete(DeleteBehavior.Restrict);
            notification.HasIndex(x => new { x.UserId, x.Id });
            notification.Property(x => x.Title).HasMaxLength(200);
        });

        builder.Entity<Attachment>(file =>
        {
            file.HasOne<Order>().WithMany()
                .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            file.HasOne<AppUser>().WithMany()
                .HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.Restrict);
            file.Property(x => x.FileName).HasMaxLength(200);
        });

        builder.Entity<RenderVersion>(render =>
        {
            render.HasOne<Scene>().WithMany()
                .HasForeignKey(x => x.SceneId).OnDelete(DeleteBehavior.Restrict);
            render.HasOne<AppUser>().WithMany()
                .HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.Restrict);
            render.HasIndex(x => new { x.SceneId, x.VersionNumber }).IsUnique();
            render.Property(x => x.FileName).HasMaxLength(200);
        });

        builder.Entity<RevisionRequest>(request =>
        {
            request.HasOne<RenderVersion>().WithMany()
                .HasForeignKey(x => x.RenderVersionId).OnDelete(DeleteBehavior.Restrict);
            request.HasOne<AppUser>().WithMany()
                .HasForeignKey(x => x.RequestedById).OnDelete(DeleteBehavior.Restrict);
            request.HasOne<SceneRequirementsRevision>().WithMany()
                .HasForeignKey(x => x.RequirementsRevisionId).OnDelete(DeleteBehavior.Restrict);
            request.Property(x => x.Comment).HasMaxLength(2000);
            request.Property(x => x.ClassificationReason).HasMaxLength(2000);
        });

        builder.Entity<TimeEntry>(entry =>
        {
            entry.HasOne<Scene>().WithMany()
                .HasForeignKey(x => x.SceneId).OnDelete(DeleteBehavior.Restrict);
            entry.HasOne<AppUser>().WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TimeEntryChange>(change =>
        {
            change.HasOne<TimeEntry>().WithMany()
                .HasForeignKey(x => x.TimeEntryId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ScenePlanApproval>(approval =>
        {
            approval.HasOne<MLPrediction>()
                .WithMany()
                .HasForeignKey(x => x.PredictionId)
                .OnDelete(DeleteBehavior.Restrict);

            approval.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}