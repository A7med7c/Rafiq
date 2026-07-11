using Hangfire;
using Hangfire.SqlServer;
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
using Rafiq.Infrastructure.Services;
<<<<<<< Updated upstream
=======
using Rafiq.Infrastructure.Services.auth;
using Rafiq.Infrastructure.Services.Auth;
using Rafiq.Infrastructure.Services.Notifications;
using Rafiq.Infrastructure.Services.BackgroundJobs;
using Rafiq.Infrastructure.Services.MedicationReminders;
>>>>>>> Stashed changes

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


        services.AddScoped<IPhoneVerificationRepository, PhoneVerificationRepository>();

        services.Configure<TwilioSettings>(configuration.GetSection("TwilioSettings"));
<<<<<<< Updated upstream
=======

        services.AddHostedService<MissedAppointmentsBackgroundService>();

        // ── Medication Reminder Engine ─────────────────────────────────────
        services.AddScoped<IMedicationReminderLogRepository, MedicationReminderLogRepository>();
        services.AddScoped<IMedicationReminderScheduler, MedicationReminderScheduler>();
        services.AddScoped<MedicationReminderJob>();
        services.AddScoped<DailyMedicationSchedulerJob>();

        // ── Hangfire ──────────────────────────────────────────────────────
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));

        services.AddHangfireServer();

>>>>>>> Stashed changes
        return services;
    }
}
