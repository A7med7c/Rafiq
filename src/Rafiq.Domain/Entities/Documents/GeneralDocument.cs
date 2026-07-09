using Rafiq.Domain.Common;

public sealed class GeneralDocument : BaseEntity
{
    public Guid UserId { get; private set; }

    public string Title { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public string? AiSummary { get; private set; }

    public string ImagePath { get; private set; } = null!;

    protected GeneralDocument() { }

    public GeneralDocument(
        Guid userId,
        string title,
        string description,
        string imagePath,
        string? aiSummary = null)
    {
        UserId = userId;
        Title = title;
        Description = description;
        ImagePath = imagePath;
        AiSummary = aiSummary;
    }

    public void Update(
        string title,
        string description,
        string? aiSummary)
    {
        Title = title;
        Description = description;
        AiSummary = aiSummary;
    }
}