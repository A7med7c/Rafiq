using System.Text.Json;

namespace Rafiq.Application.VoiceAgent.Tools;

/// <summary>
/// Convenience helpers so tool implementations can write p.GetString("name")
/// instead of p.TryGetProperty("name", out var v) ? v.GetString() : null.
/// </summary>
public static class JsonElementExtensions
{
    public static string? GetString(this JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : null;

    public static int? GetInt32OrNull(this JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop)) return null;
        return prop.ValueKind == JsonValueKind.Number ? prop.GetInt32() : null;
    }

    public static bool GetBool(this JsonElement element, string propertyName, bool defaultValue = false)
    {
        if (!element.TryGetProperty(propertyName, out var prop)) return defaultValue;
        return prop.ValueKind == JsonValueKind.True;
    }
}
