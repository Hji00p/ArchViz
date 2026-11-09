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