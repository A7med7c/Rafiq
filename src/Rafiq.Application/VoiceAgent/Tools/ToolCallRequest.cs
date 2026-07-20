using System.Text.Json;

namespace Rafiq.Application.VoiceAgent.Tools;

public sealed record ToolCallRequest(string ToolName, JsonElement Parameters);
