using FluentValidation;
using MediatR;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.AiChat.Services;
using Rafiq.Application.VoiceAgent.Extensions;
using System.Reflection;

namespace Rafiq.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(Common.Behaviors.ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(assembly);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        // HealthQueryContextBuilder is still used by GenerateHealthSummary — the AI-chat
        // send pipeline moved off it in the Chat/Voice unification (uses tool loop instead).
        services.AddScoped<IHealthQueryContextBuilder, HealthQueryContextBuilder>();

        services.AddVoiceAgent();

        return services;
    }
}
