using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Repositories;
using Rafiq.Infrastructure.Persistence;
using Rafiq.Infrastructure.Persistence.Identity;
using Rafiq.Infrastructure.Persistence.Repositories;
using Rafiq.Infrastructure.Services.auth;
using Rafiq.Infrastructure.Services.Notifications;

namespace Rafiq.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<RafiqDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<RafiqDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<RafiqDbContext>());
        services.AddScoped<IPatientProfileRepository, UserHealthProfileRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<INotificationsService, NotificationsService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITokenHasher, Sha256TokenHasher>();
        services.AddScoped<IOtpHasher, BCryptOtpHasher>();
        services.AddScoped<IOtpGenerator, OtpGenerator>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();


        services.AddScoped<IPhoneVerificationRepository, PhoneVerificationRepository>();

        services.Configure<TwilioSettings>(configuration.GetSection("TwilioSettings"));
        return services;
    }
}
