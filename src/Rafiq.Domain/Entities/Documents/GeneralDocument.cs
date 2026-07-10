using Rafiq.Domain.Common;
using Rafiq.Domain.Entities.User;

public sealed class GeneralDocument : BaseEntity
{
    public Guid UserHealthProfileId { get; private set; }

    public UserHealthProfile UserHealthProfile { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public string? AiSummary { get; private set; }

    public string ImagePath { get; private set; } = null!;

    protected GeneralDocument() { }

    public GeneralDocument(
        Guid userHealthProfileId,
        string title,
        string description,
        string imagePath,
        string? aiSummary = null)
    {
        UserHealthProfileId = userHealthProfileId;
        Title = title;
        Description = description;
        ImagePath = imagePath;
        AiSummary = aiSummary;
    }

    public void Update(
        string title,
        string description,
        string? aiSummary,
        string? imagePath)
    {
        Title = title;
        Description = description;
        AiSummary = aiSummary;
        if (!string.IsNullOrWhiteSpace(imagePath))
            ImagePath = imagePath;

        MarkUpdated();
    }
}