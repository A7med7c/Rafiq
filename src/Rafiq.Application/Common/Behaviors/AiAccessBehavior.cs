using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that blocks AI commands when the user's AI access
/// has been restricted by an administrator.
/// </summary>
public sealed class AiAccessBehavior<TRequest, TResponse>(
    ICurrentUserService currentUserService,
    IUserStatusService userStatusService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IAiCommand)
            return await next();

        var userId = currentUserService.UserId;
        if (userId is null)
            return await next();

        var status = await userStatusService.GetStatusAsync(userId.Value, cancellationToken);
        if (status is { IsAiRestricted: true })
            throw new ForbiddenException("Your AI access has been restricted by an administrator. Please contact support for assistance.");

        return await next();
    }
}
