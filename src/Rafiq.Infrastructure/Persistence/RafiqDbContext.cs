using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Common;
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

    public DbSet<Allergy> Allergies => Set<Allergy>();
    public DbSet<PhoneVerification> PhoneVerifications => Set<PhoneVerification>();

    public DbSet<ChronicDisease> ChronicDiseases => Set<ChronicDisease>();

    #endregion

    #region Medical Documents

    public DbSet<Prescription> Prescriptions => Set<Prescription>();

    public DbSet<PrescriptionMedicine> PrescriptionMedicines => Set<PrescriptionMedicine>();

    public DbSet<UserMedicine> UserMedicines => Set<UserMedicine>();

    public DbSet<LabReport> LabReports => Set<LabReport>();

    public DbSet<LabResult> LabResults => Set<LabResult>();

    public DbSet<ImagingReport> ImagingReports => Set<ImagingReport>();

    #endregion

    #region Identity

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.SoftDelete();
                    break;

                case EntityState.Modified:
                    entry.Entity.MarkUpdated();
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
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