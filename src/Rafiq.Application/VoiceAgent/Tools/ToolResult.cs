using System.Text.Json.Serialization;
using Rafiq.Application.VoiceAgent.Domain;

namespace Rafiq.Application.VoiceAgent.Tools;

public sealed record ToolResult(
    bool Success,
    string Message,
    object? Data = null,

    // These outer-structure fields stay omitted when null (clean error/success payloads).
    // DO NOT add WhenWritingNull to Data — null values inside Data must be visible to the AI
    // so it can distinguish "field not recorded" from "field not included in response".
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ErrorCode = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? EntityType = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<CompletenessGap>? CompletenessGaps = null);
