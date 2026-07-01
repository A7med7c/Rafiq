using Mapster;
using MapsterMapper;
using Rafiq.Application.Common.Mappings;

namespace Rafiq.Application.Tests;

internal static class TestMapperFactory
{
    public static IMapper Create()
    {
        var config = new TypeAdapterConfig();
        new ApplicationMappingRegister().Register(config);
        return new Mapper(config);
    }
}
