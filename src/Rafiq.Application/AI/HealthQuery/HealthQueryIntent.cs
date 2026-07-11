namespace Rafiq.Application.AI.HealthQuery;

/// <summary>
/// The raw shape the AI model's intent-classification JSON is deserialized into.
/// This is UNTRUSTED input: every field is free-form text from the model and must be
/// validated against the fixed allowlists in <see cref="HealthQueryCategory"/>,
/// <see cref="HealthQueryOperation"/>, and <see cref="HealthQueryTimeframe"/> by
/// <see cref="HealthQueryIntentParser"/> before it can influence any query.
/// Unknown JSON properties (e.g. "requestedDetails") are ignored by the deserializer.
/// </summary>
public sealed class HealthQueryIntent
{
    public List<string> Categories { get; set; } = new();

    public string? Operation { get; set; }

    public string? SearchTerm { get; set; }

    public string? Timeframe { get; set; }
}
