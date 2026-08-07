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
using Rafiq.Infrastructure.Services.auth;
using Rafiq.Infrastructure.Services.Auth;
using Rafiq.Infrastructure.Services.Notifications;
using Rafiq.Infrastructure.Services.BackgroundJobs;
using Rafiq.Infrastructure.Services.MedicationReminders;
using Rafiq.Infrastructure.Services.AppointmentReminders;
using Rafiq.Infrastructure.Services.Ai;
using Rafiq.Infrastructure.Services.AiChat;
using Rafiq.Infrastructure.Services.Common;
using Rafiq.Infrastructure.Services.MedicalReport;


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
        services.AddScoped<IHealthSummaryCacheRepository, HealthSummaryCacheRepository>();
        services.AddScoped<IAllergyRepository, AllergyRepository>();
        services.AddScoped<IChronicDiseaseRepository, ChronicDiseaseRepository>();
        services.AddScoped<IHealthProfileAccessRepository, HealthProfileAccessRepository>();
        services.AddScoped<IHealthProfileAuthorizationService, HealthProfileAuthorizationService>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<INotificationsService, NotificationsService>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddSingleton<IConnectionManager, ConnectionManager>();
        services.AddSignalR();
        services.AddScoped<INotificationService, SignalRNotificationService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAdminAiService, AdminAiService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddHttpContextAccessor();
        services.AddScoped<IBackgroundUserContext, BackgroundUserContext>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITokenHasher, Sha256TokenHasher>();
        services.AddScoped<IOtpHasher, BCryptOtpHasher>();
        services.AddScoped<IOtpGenerator, OtpGenerator>();
        services.AddScoped<IOtpVerificationRepository, OtpVerificationRepository>();
        services.AddScoped<ITokenIssuingService, TokenIssuingService>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IResetTokenService, ResetTokenService>();
        services.AddScoped<IGeneralDocumentRepository, GeneralDocumentRepository>();

        // ── Telemetry ─────────────────────────────────────────────────────
        services.AddScoped<IAiTelemetryContext, AiTelemetryContext>();
        services.AddSingleton<IAiRequestLogWriter, AiRequestLogWriter>();

        // ── Bedrock ────────────────────────────────────────────────────────
        services.Configure<BedrockSettings>(configuration.GetSection("Bedrock"));
        services.AddHttpClient<BedrockService>();                         // concrete typed client
        services.AddScoped<IBedrockService, InstrumentedBedrockService>(); // decorator

        // ── AiChat ────────────────────────────────────────────────────────
        services.Configure<AiChatSettings>(configuration.GetSection("AiChat"));
        services.AddHttpClient<AiChatService>();                           // concrete typed client
        services.AddScoped<IAiChatService, InstrumentedAiChatService>();   // decorator

        // ── Medical Report ────────────────────────────────────────────────
        services.AddScoped<IMedicalReportPdfGenerator, MedicalReportPdfGenerator>();

        // ── Documents ─────────────────────────────────────────────────────
        services.AddScoped<ILabReportRepository, LabReportRepository>();
        services.AddScoped<IImagingReportRepository, ImagingReportRepository>();

        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IUserMedicineRepository, UserMedicineRepository>();
        services.AddScoped<IMedicineReminderRepository, MedicineReminderRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IEmergencyContactRepository, EmergencyContactRepository>();
        services.AddScoped<IAiConversationRepository, AiConversationRepository>();
        services.AddScoped<IAppReviewRepository, AppReviewRepository>();
        services.AddScoped<IUserNotificationRepository, UserNotificationRepository>();
        services.AddScoped<IMessageReactionRepository, MessageReactionRepository>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IDocumentUploadSessionRepository, DocumentUploadSessionRepository>();
        services.AddScoped<IDuplicateDocumentDetector, DuplicateDocumentDetector>();

        services.Configure<TwilioSettings>(configuration.GetSection("TwilioSettings"));
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        // ── WhatsApp ──────────────────────────────────────────────────────
        services.Configure<WhatsAppSettings>(configuration.GetSection(WhatsAppSettings.SectionName));
        services.AddHttpClient<IWhatsAppService, WhatsAppService>();

        // ── Medication Reminder Engine ─────────────────────────────────────
        services.Configure<MedicationReminderOptions>(
            configuration.GetSection(MedicationReminderOptions.SectionName));
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IMedicationReminderLogRepository, MedicationReminderLogRepository>();
        services.AddScoped<IMedicationReminderScheduler, MedicationReminderScheduler>();
        services.AddScoped<IMedicationSchedulingService, MedicationSchedulingService>();
        services.AddScoped<MedicationReminderJob>();
        services.AddScoped<DailyMedicationSchedulerJob>();

        // ── Appointment Reminder Engine ────────────────────────────────────
        services.AddScoped<IAppointmentReminderScheduler, AppointmentReminderScheduler>();
        services.AddScoped<AppointmentReminderJob>();

        // ── Chat async processor ──────────────────────────────────────────
        services.AddScoped<ChatMessageProcessorJob>();
        services.AddScoped<IChatBackgroundJobService, ChatBackgroundJobService>();

        // ── Usage Intelligence ────────────────────────────────────────────
        services.AddScoped<IAiRequestClassifier, AiRequestClassifier>();
        services.AddScoped<IUsageIntelligenceService, UsageIntelligenceService>();
        services.AddScoped<IUserStatusService, UserStatusService>();
        services.AddScoped<AiRequestClassificationJob>();
        services.AddScoped<IRequestClassificationJobService, RequestClassificationJobService>();

        // ── Async Document Analysis ───────────────────────────────────────
        services.AddScoped<DocumentAnalysisJob>();
        services.AddScoped<DocumentRecoveryJob>();
        services.AddScoped<IDocumentAnalysisJobService, DocumentAnalysisJobService>();

        // ── Medical Warnings ──────────────────────────────────────────────
        services.Configure<MedicalWarningSettings>(configuration.GetSection("MedicalWarning"));
        services.AddScoped<IMedicalWarningCalculator, MedicalWarningCalculator>();

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
        return services;
    }
}
