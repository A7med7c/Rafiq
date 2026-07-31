namespace Rafiq.Application.Common.Interfaces;

/// <summary>
/// Marker interface for MediatR commands that invoke AI features.
/// Commands implementing this will have AI access checked by AiAccessBehavior.
/// </summary>
public interface IAiCommand { }
