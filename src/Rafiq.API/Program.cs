
using Hangfire;
using Hangfire.Dashboard;
using Rafiq.API.Extensions;
using Rafiq.API.Middleware;
using Rafiq.Application;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Infrastructure;
using Rafiq.Infrastructure.Services.auth;
using Rafiq.Infrastructure.Services.MedicationReminders;
using System.Text.Json.Serialization;
using Rafiq.Infrastructure.Services.Notifications;

namespace Rafiq.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter());
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
                    .WithOrigins("http://localhost:4200")
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

        app.UseHttpsRedirection();
        app.UseCors("Angular");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseStaticFiles();
        app.MapControllers();
        app.MapHub<NotificationHub>("/hubs/notifications");
        app.MapHealthChecks("/health");

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
        });

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

        app.Run();
    }
}
