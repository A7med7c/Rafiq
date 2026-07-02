using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.User;

namespace Rafiq.Infrastructure.Persistence.Configurations
{
    public class ChronicDiseaseConfiguration : IEntityTypeConfiguration<ChronicDisease>
    {
        public void Configure(EntityTypeBuilder<ChronicDisease> builder)
        {
            builder.ToTable("ChronicDiseases");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();


            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.HasOne(x => x.UserHealthProfile)
                .WithMany(x => x.ChronicDiseases)
                .HasForeignKey(x => x.UserHealthProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserHealthProfileId);
        }
    }
}