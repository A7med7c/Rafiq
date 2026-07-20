namespace Rafiq.Application.VoiceAgent.Agent;

public abstract record AgentOutput;

public sealed record FinalAnswer(string Response, string? NavigateTo) : AgentOutput;

public sealed record AskUser(string Question) : AgentOutput;

public sealed record ToolCall(string ToolName, System.Text.Json.JsonElement Parameters) : AgentOutput;
