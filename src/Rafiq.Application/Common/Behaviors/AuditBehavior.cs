using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Entities;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Common.Behaviors;

public sealed class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AuditBehavior(
        ICurrentUserService currentUserService,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is IAuditableRequest auditable && _currentUserService.UserId is Guid actorUserId && auditable.EntityId is Guid entityId)
        {
            var auditLog = new AuditLog(
                actorUserId,
                auditable.PatientProfileId,
                auditable.AuditAction,
                auditable.EntityType,
                entityId,
                _currentUserService.IpAddress);

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return response;
    }
}
