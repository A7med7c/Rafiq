using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Rafiq.Domain.Common;
using Rafiq.Domain.Entities;
using Rafiq.Domain.Repositories;
using Rafiq.Infrastructure.Persistence.Identity;
using System.Linq.Expressions;

namespace Rafiq.Infrastructure.Persistence;

public sealed class RafiqDbContext : IdentityDbContext<
    ApplicationUser,
    IdentityRole<Guid>,
    Guid,
    IdentityUserClaim<Guid>,
    IdentityUserRole<Guid>,
    IdentityUserLogin<Guid>,
    IdentityRoleClaim<Guid>,
    IdentityUserToken<Guid>,
    IdentityUserPasskey<Guid>>, IUnitOfWork
{
    public RafiqDbContext(DbContextOptions<RafiqDbContext> options) : base(options) { }

    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<CaregiverLink> CaregiverLinks => Set<CaregiverLink>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ExtractedEntity> ExtractedEntities => Set<ExtractedEntity>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<MedicationSchedule> MedicationSchedules => Set<MedicationSchedule>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<LabResult> LabResults => Set<LabResult>();
    public DbSet<HealthcareProvider> HealthcareProviders => Set<HealthcareProvider>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<KnowledgeSource> KnowledgeSources => Set<KnowledgeSource>();
    public DbSet<ChatMessageCitation> ChatMessageCitations => Set<ChatMessageCitation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Consent> Consents => Set<Consent>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RafiqDbContext).Assembly);
        ApplySoftDeleteFilters(modelBuilder);
        ApplyDateTimeConventions(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.SoftDelete();
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.MarkUpdated();
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) || entityType.ClrType == typeof(AuditLog))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            var filter = Expression.Lambda(Expression.Equal(property, Expression.Constant(false)), parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }

    private static void ApplyDateTimeConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties()
                         .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
            {
                property.SetColumnType("datetime2(7)");
            }
        }
    }
}
