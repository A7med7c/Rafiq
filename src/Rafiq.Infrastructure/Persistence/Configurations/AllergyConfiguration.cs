using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.User;

namespace Rafiq.Infrastructure.Persistence.Configurations
{
    public class AllergyConfiguration : IEntityTypeConfiguration<Allergy>
    {
        public void Configure(EntityTypeBuilder<Allergy> builder)
        {
            builder.ToTable("Allergies");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();


            builder.Property(x => x.Severity)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.HasOne(x => x.UserHealthProfile)
                .WithMany(x => x.Allergies)
                .HasForeignKey(x => x.UserHealthProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserHealthProfileId);
        }
    }
}