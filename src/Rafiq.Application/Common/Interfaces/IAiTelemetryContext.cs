using Rafiq.Domain.Enums;

namespace Rafiq.Application.Common.Interfaces;

/// <summary>
/// Scoped ambient context set by MediatR handlers before calling AI services.
/// The instrumented AI service decorators read from this to tag telemetry logs.
/// </summary>
public interface IAiTelemetryContext
{
    AiFeature? Feature        { get; set; }
    Guid?      UserId         { get; set; }
    Guid?      ConversationId { get; set; }
}
