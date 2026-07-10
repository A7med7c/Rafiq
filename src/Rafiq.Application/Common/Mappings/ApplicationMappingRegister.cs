using Mapster;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Entities.User;

namespace Rafiq.Application.Common.Mappings;

public sealed class ApplicationMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UserHealthProfile, PatientProfileDto>()
     .Map(dest => dest.Gender, src => src.Gender.ToString())
     .Map(dest => dest.BloodType, src => src.BloodType == null ? null : src.BloodType.ToString());

        config.NewConfig<Allergy, AllergyDto>()
            .Map(dest => dest.Severity, src => src.Severity.ToString());

        config.NewConfig<ChronicDisease, ChronicDiseaseDto>()
            .Map(dest => dest.Status, src => src.Status.ToString());
    }
}
