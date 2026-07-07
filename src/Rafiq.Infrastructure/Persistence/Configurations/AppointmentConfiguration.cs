using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Infrastructure.Persistence.Identity;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AppointmentType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CustomType)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Title)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.Provider)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.AppointmentDateTime)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasColumnType("nvarchar(max)");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.AppointmentDateTime });
        builder.HasIndex(x => new { x.UserId, x.Status, x.AppointmentDateTime });
    }
}
