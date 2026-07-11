namespace Rafiq.Application.Common.Models;

public sealed class AiChatSettings
{
    public string ModelId { get; set; } = "qwen.qwen2.5-72b-instruct";
    public string MultimodalModelId { get; set; } = "qwen.qwen3-vl-235b-a22b";
    public string EndpointPath { get; set; } = "/student/chat";
    public string MultimodalEndpointPath { get; set; } = "/student/multimodal-chat";
    public int MaxOutputTokens { get; set; } = 1000;
    public int TimeoutSeconds { get; set; } = 25;
    public int MaxAcceptedResponseLength { get; set; } = 4000;
}