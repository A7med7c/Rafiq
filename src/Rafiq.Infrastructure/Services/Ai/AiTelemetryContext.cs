using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Enums;

namespace Rafiq.Infrastructure.Services.Ai;

/// <summary>
/// Scoped ambient context carried through a single DI scope.
/// Handlers set Feature/UserId/ConversationId before invoking an AI service;
/// the instrumented decorators read it to tag telemetry rows.
/// </summary>
public sealed class AiTelemetryContext : IAiTelemetryContext
{
    public AiFeature? Feature        { get; set; }
    public Guid?      UserId         { get; set; }
    public Guid?      ConversationId { get; set; }
}
