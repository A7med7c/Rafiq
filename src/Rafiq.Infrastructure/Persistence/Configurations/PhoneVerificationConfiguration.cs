using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.User;
using Rafiq.Infrastructure.Persistence.Identity;

namespace Rafiq.Infrastructure.Persistence.Configurations
{
    public class PhoneVerificationConfiguration
     : IEntityTypeConfiguration<PhoneVerification>
    {
        public void Configure(EntityTypeBuilder<PhoneVerification> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CodeHash)
                   .IsRequired();

            builder.HasOne<ApplicationUser>()
                   .WithMany()
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
