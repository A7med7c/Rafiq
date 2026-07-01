using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Repositories;
using UnauthorizedException = Rafiq.Domain.Exceptions.UnauthorizedException;

namespace Rafiq.Application.Common.Behaviors;

public sealed class PatientAccessBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPatientProfileRepository _patientProfileRepository;

    public PatientAccessBehavior(ICurrentUserService currentUserService, IPatientProfileRepository patientProfileRepository)
    {
        _currentUserService = currentUserService;
        _patientProfileRepository = patientProfileRepository;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IPatientOwnedRequest patientRequest)
        {
            return await next();
        }

        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        if (_currentUserService.IsInRole("Admin"))
        {
            return await next();
        }

        var profile = await _patientProfileRepository.GetByIdAsync(patientRequest.PatientProfileId, cancellationToken);
        if (profile?.UserId != currentUserId)
        {
            throw new UnauthorizedException("You do not have permission to access this patient profile.");
        }

        return await next();
    }
}
