using MapsterMapper;
using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.Allergies.CreateAllergy;

public sealed class CreateAllergyCommandHandler(
    IAllergyRepository allergyRepository,
    IUserMedicineRepository userMedicineRepository,
    IHealthProfileAuthorizationService authorizationService,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IHealthSummaryCacheRepository summaryCache)
    : IRequestHandler<CreateAllergyCommand, ApiResponse<AllergyDto>>
{
    public async Task<ApiResponse<AllergyDto>> Handle(
        CreateAllergyCommand request,
        CancellationToken cancellationToken)
    {
        await authorizationService.EnsureCanWriteAsync(request.PatientProfileId, cancellationToken);

        var duplicate = await allergyRepository.ExistsByNameForProfileAsync(
            request.PatientProfileId, request.Name, excludeId: null, cancellationToken);

        if (duplicate)
            throw new ConflictException($"An allergy named '{request.Name}' already exists for this profile.");

        var allergy = new Allergy(request.Name, request.Severity)
        {
            UserHealthProfileId = request.PatientProfileId
        };

        await allergyRepository.AddAsync(allergy, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await summaryCache.MarkNeedsRefreshAsync(request.PatientProfileId, cancellationToken);

        var userMedicines = await userMedicineRepository.GetAllByProfileIdAsync(request.PatientProfileId, cancellationToken);
        var conflictingMedicines = AllergyConflictChecker.GetConflictingMedicines(request.Name, userMedicines.Select(m => m.MedicineName));

        var message = "Allergy created successfully.";
        if (conflictingMedicines.Count > 0)
        {
            message = $"خلي بالك، في أدوية عندك بتتعارض مع الحساسية دي: {string.Join("، ", conflictingMedicines)}. كمرجع أولي، مع ملاحظة إن القرار الطبي النهائي يعتمد على تقييم الطبيب أو الصيدلي.";
        }

        return ApiResponse<AllergyDto>.SuccessResponse(
            mapper.Map<AllergyDto>(allergy),
            message);
    }
}
