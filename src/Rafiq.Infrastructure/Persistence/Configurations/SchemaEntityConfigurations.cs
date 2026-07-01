using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities;
using Rafiq.Domain.Enums;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class CaregiverLinkConfiguration : IEntityTypeConfiguration<CaregiverLink>
{
    public void Configure(EntityTypeBuilder<CaregiverLink> builder)
    {
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.PermissionLevel).HasConversion<string>().HasMaxLength(50);
        builder.HasIndex(x => new { x.PatientProfileId, x.CaregiverUserId }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(x => x.PatientProfile).WithMany(x => x.CaregiverLinks).HasForeignKey(x => x.PatientProfileId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.BlobUrl).IsRequired().HasMaxLength(2048);
        builder.Property(x => x.ContainerName).IsRequired().HasMaxLength(256);
        builder.Property(x => x.BlobName).IsRequired().HasMaxLength(1024);
        builder.Property(x => x.MimeType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.OcrStatus).HasConversion<string>().HasMaxLength(50);
        builder.HasOne(x => x.PatientProfile).WithMany(x => x.Documents).HasForeignKey(x => x.PatientProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Provider).WithMany(x => x.Documents).HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class ExtractedEntityConfiguration : IEntityTypeConfiguration<ExtractedEntity>
{
    public void Configure(EntityTypeBuilder<ExtractedEntity> builder)
    {
        builder.Property(x => x.EntityType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.EntityValue).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Confidence).HasPrecision(5, 4);
        builder.HasOne(x => x.Document).WithMany(x => x.ExtractedEntities).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class MedicationConfiguration : IEntityTypeConfiguration<Medication>
{
    public void Configure(EntityTypeBuilder<Medication> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Dosage).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Frequency).IsRequired().HasMaxLength(100);
        builder.HasOne(x => x.PatientProfile).WithMany(x => x.Medications).HasForeignKey(x => x.PatientProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SourceDocument).WithMany().HasForeignKey(x => x.SourceDocumentId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class MedicationScheduleConfiguration : IEntityTypeConfiguration<MedicationSchedule>
{
    public void Configure(EntityTypeBuilder<MedicationSchedule> builder)
    {
        builder.Property(x => x.DoseStatus).HasConversion<string>().HasMaxLength(50);
        builder.HasOne(x => x.Medication).WithMany(x => x.MedicationSchedules).HasForeignKey(x => x.MedicationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.HasOne(x => x.PatientProfile).WithMany(x => x.Appointments).HasForeignKey(x => x.PatientProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Provider).WithMany(x => x.Appointments).HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class LabResultConfiguration : IEntityTypeConfiguration<LabResult>
{
    public void Configure(EntityTypeBuilder<LabResult> builder)
    {
        builder.Property(x => x.TestName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ResultValue).IsRequired().HasMaxLength(200);
        builder.HasOne(x => x.PatientProfile).WithMany(x => x.LabResults).HasForeignKey(x => x.PatientProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Document).WithMany(x => x.LabResults).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class HealthcareProviderConfiguration : IEntityTypeConfiguration<HealthcareProvider>
{
    public void Configure(EntityTypeBuilder<HealthcareProvider> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Specialty).IsRequired().HasMaxLength(200);
    }
}

public sealed class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.HasOne(x => x.PatientProfile).WithMany(x => x.ChatSessions).HasForeignKey(x => x.PatientProfileId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.Property(x => x.Sender).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Content).IsRequired();
        builder.HasOne(x => x.ChatSession).WithMany(x => x.ChatMessages).HasForeignKey(x => x.ChatSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class KnowledgeSourceConfiguration : IEntityTypeConfiguration<KnowledgeSource>
{
    public void Configure(EntityTypeBuilder<KnowledgeSource> builder)
    {
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(50);
    }
}

public sealed class ChatMessageCitationConfiguration : IEntityTypeConfiguration<ChatMessageCitation>
{
    public void Configure(EntityTypeBuilder<ChatMessageCitation> builder)
    {
        builder.Property(x => x.ClaimText).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Locator).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ConfidenceScore).HasPrecision(5, 4);
        builder.HasOne(x => x.ChatMessage).WithMany(x => x.Citations).HasForeignKey(x => x.ChatMessageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.KnowledgeSource).WithMany(x => x.Citations).HasForeignKey(x => x.KnowledgeSourceId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Body).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.NotificationType).HasConversion<string>().HasMaxLength(50);
        builder.HasIndex(x => x.UserId);
    }
}

public sealed class ConsentConfiguration : IEntityTypeConfiguration<Consent>
{
    public void Configure(EntityTypeBuilder<Consent> builder)
    {
        builder.Property(x => x.ConsentType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.HasOne(x => x.PatientProfile).WithMany(x => x.Consents).HasForeignKey(x => x.PatientProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.PatientProfileId, x.ConsentType, x.Status });
    }
}
