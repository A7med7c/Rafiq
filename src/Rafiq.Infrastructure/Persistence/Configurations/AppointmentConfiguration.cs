using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Documents;

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

        builder.HasOne(x => x.UserHealthProfile)
            .WithMany()
            .HasForeignKey(x => x.UserHealthProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserHealthProfileId);
        builder.HasIndex(x => new { x.UserHealthProfileId, x.AppointmentDateTime });
        builder.HasIndex(x => new { x.UserHealthProfileId, x.Status, x.AppointmentDateTime });
    }
}
