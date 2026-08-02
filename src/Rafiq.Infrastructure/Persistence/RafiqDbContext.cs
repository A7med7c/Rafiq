using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Rafiq.Domain.Common;
using Rafiq.Domain.Entities;
using Rafiq.Domain.Entities.Ai;
using Rafiq.Domain.Entities.Chat;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Entities.User;
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
    public RafiqDbContext(DbContextOptions<RafiqDbContext> options)
        : base(options)
    {
    }

    #region User

    public DbSet<UserHealthProfile> UserHealthProfiles => Set<UserHealthProfile>();

    public DbSet<HealthSummaryCache> HealthSummaryCaches => Set<HealthSummaryCache>();

    public DbSet<HealthProfileAccess> HealthProfileAccesses => Set<HealthProfileAccess>();

    public DbSet<Allergy> Allergies => Set<Allergy>();
    public DbSet<OtpVerification> PhoneVerifications => Set<OtpVerification>();

    public DbSet<ChronicDisease> ChronicDiseases => Set<ChronicDisease>();
    public DbSet<Otp> Otps => Set<Otp>();
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();

    #endregion

    #region Medical Documents

    public DbSet<Prescription> Prescriptions => Set<Prescription>();

    public DbSet<PrescriptionMedicine> PrescriptionMedicines => Set<PrescriptionMedicine>();

    public DbSet<UserMedicine> UserMedicines => Set<UserMedicine>();

    public DbSet<MedicineReminder> MedicineReminders => Set<MedicineReminder>();

    public DbSet<MedicationReminderLog> MedicationReminderLogs => Set<MedicationReminderLog>();

    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<LabReport> LabReports => Set<LabReport>();

    public DbSet<LabResult> LabResults => Set<LabResult>();

    public DbSet<ImagingReport> ImagingReports => Set<ImagingReport>();
    public DbSet<GeneralDocument> GeneralDocuments => Set<GeneralDocument>();

    #endregion

    #region Identity

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    #endregion

    #region Audit Logs

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    #endregion

    #region Reviews

    public DbSet<AppReview> AppReviews => Set<AppReview>();

    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    #endregion

    #region AI Chat

    public DbSet<AiConversation> AiConversations => Set<AiConversation>();

    public DbSet<AiMessage> AiMessages => Set<AiMessage>();

    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();

    public DbSet<AiRequestLog> AiRequestLogs => Set<AiRequestLog>();

    public DbSet<AiFlaggedRequest> AiFlaggedRequests => Set<AiFlaggedRequest>();

    public DbSet<AiUsageAction> AiUsageActions => Set<AiUsageAction>();

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RafiqDbContext).Assembly);

        // Use Table-Per-Type for MedicalDocument inheritance

        ApplySoftDeleteFilters(modelBuilder);

        ApplyDateTimeConventions(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        // 1. Detect all changes first so EF has a complete picture of what changed.
        ChangeTracker.DetectChanges();

        var now = DateTime.UtcNow;

        // 2. Process each tracked BaseEntity entry.
        foreach (var entry in ChangeTracker.Entries<BaseEntity>().ToList())
        {
            if (entry.State == EntityState.Deleted)
            {
                // Convert hard-delete to soft-delete by staying in Modified state
                // and only stamping the three soft-delete columns via property accessors.
                // This keeps EF's internal tracking of which properties actually changed
                // intact and avoids generating spurious WHERE conditions that cause
                // DbUpdateConcurrencyException.
                entry.State = EntityState.Modified;

                // Mark only the soft-delete columns as changed
                entry.Property(nameof(BaseEntity.IsDeleted)).CurrentValue   = true;
                entry.Property(nameof(BaseEntity.DeletedAt)).CurrentValue   = now;
                entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue   = now;
                entry.Property(nameof(BaseEntity.IsDeleted)).IsModified     = true;
                entry.Property(nameof(BaseEntity.DeletedAt)).IsModified     = true;
                entry.Property(nameof(BaseEntity.UpdatedAt)).IsModified     = true;
            }
            else if (entry.State == EntityState.Modified)
            {
                // Stamp UpdatedAt on every legitimately modified entity.
                entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = now;
                entry.Property(nameof(BaseEntity.UpdatedAt)).IsModified   = true;
            }
        }

        // 3. Disable auto-detect during the actual save to prevent EF's internal
        //    second-pass DetectChanges() from resetting any of our manual state edits.
        var originalAutoDetect = ChangeTracker.AutoDetectChangesEnabled;
        ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            return base.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            ChangeTracker.AutoDetectChangesEnabled = originalAutoDetect;
        }
    }


    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            if (entityType.BaseType != null)
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");

            var property = Expression.Property(
                parameter,
                nameof(BaseEntity.IsDeleted));

            var body = Expression.Equal(
                property,
                Expression.Constant(false));

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }
    private static void ApplyDateTimeConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties()
                         .Where(p =>
                             p.ClrType == typeof(DateTime) ||
                             p.ClrType == typeof(DateTime?)))
            {
                property.SetColumnType("datetime2(7)");
            }
        }
    }
}
