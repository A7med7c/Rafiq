using Mapster;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Entities;

namespace Rafiq.Application.Common.Mappings;

public sealed class ApplicationMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PatientProfile, PatientProfileDto>()
            .Map(dest => dest.Gender, src => src.Gender.ToString())
            .Map(dest => dest.BloodType, src => src.BloodType.HasValue ? src.BloodType.Value.ToString() : null);
    }
}
