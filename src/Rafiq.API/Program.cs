
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Identity;
using Rafiq.API.Extensions;
using Rafiq.API.Middleware;
using Rafiq.Application;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Constants;
using Rafiq.Infrastructure;
using Rafiq.Infrastructure.Persistence.Identity;
using Rafiq.Infrastructure.Services.auth;
using Rafiq.Infrastructure.Services.MedicationReminders;
using Rafiq.Infrastructure.Services.BackgroundJobs;
using System.Text.Json.Serialization;
using Rafiq.Infrastructure.Services.Notifications;

namespace Rafiq.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.Converters.Add(new Rafiq.API.Converters.UtcDateTimeJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new Rafiq.API.Converters.UtcNullableDateTimeJsonConverter());
            });
        builder.Services.AddSwaggerDocumentation();
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddJwtAuthentication(builder.Configuration, builder.Environment);
        builder.Services.AddAuthorization();
        builder.Services.AddHealthChecks();
        builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();



        builder.Services.AddCors(options =>
            {
                options.AddPolicy("Angular", policy =>
                {
                    policy
                        .WithOrigins(
                            "http://localhost:4200",
                            "http://localhost",
                            "https://localhost"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

        var app = builder.Build();

        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Rafiq API v1");
                options.RoutePrefix = "swagger";
            });
        }

        // app.UseHttpsRedirection();
        app.UseCors("Angular");
        app.UseAuthentication();
        app.UseMiddleware<UserAccessMiddleware>();
        app.UseAuthorization();
        app.UseStaticFiles();
        app.MapControllers();
        app.MapHub<NotificationHub>("/hubs/notifications");
        app.MapHealthChecks("/health");

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
        });

        await SeedRolesAndAdminAsync(app);

        // Sweep runs at 00:05 in the reminder timezone, so "today" means the same thing
        // here as it does inside the scheduling service.
        var reminderTimeZone = app.Services
            .GetRequiredService<IDateTimeProvider>()
            .ReminderTimeZone;

        RecurringJob.AddOrUpdate<DailyMedicationSchedulerJob>(
            "daily-medication-scheduler",
            job => job.ScheduleAsync(),
            "5 0 * * *",
            new RecurringJobOptions { TimeZone = reminderTimeZone });

        // Recover documents that got stuck in Processing (e.g. server crash mid-job)
        RecurringJob.AddOrUpdate<DocumentRecoveryJob>(
            "document-recovery",
            job => job.ExecuteAsync(),
            "*/10 * * * *");   // every 10 minutes

        app.Run();
    }

    private static async Task SeedRolesAndAdminAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = app.Logger;

        foreach (var roleName in new[] { Roles.User, Roles.Admin })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        var adminUsers = await userManager.GetUsersInRoleAsync(Roles.Admin);
        if (adminUsers.Count > 0)
        {
            return;
        }

        const string adminEmail = "admin@rafiq.com";
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

        if (existingAdmin is not null)
        {
            await userManager.AddToRoleAsync(existingAdmin, Roles.Admin);
            return;
        }

        var adminPassword = $"Test_123";

        var adminUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = "Rafiq",
            LastName = "Admin",
            Email = adminEmail,
            UserName = adminEmail,
            PhoneNumber = "0100000000",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);

        if (!createResult.Succeeded)
        {
            logger.LogError(
                "Failed to seed admin account: {Errors}",
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(adminUser, Roles.Admin);

        logger.LogWarning(
            "Seeded admin account {Email} with temporary password {Password} — change it after first login.",
            adminEmail,
            adminPassword);
    }
}
