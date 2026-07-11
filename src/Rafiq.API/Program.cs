
using Hangfire;
using Rafiq.API.Extensions;
using Rafiq.API.Middleware;
using Rafiq.Application;
using Rafiq.Infrastructure;
<<<<<<< Updated upstream
=======
using Rafiq.Infrastructure.Services.auth;
using Rafiq.Infrastructure.Services.MedicationReminders;
>>>>>>> Stashed changes
using System.Text.Json.Serialization;

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
        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services.AddAuthorization();
        builder.Services.AddHealthChecks();

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
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health");

        app.UseHangfireDashboard("/hangfire");

        RecurringJob.AddOrUpdate<DailyMedicationSchedulerJob>(
            "daily-medication-scheduler",
            job => job.ScheduleAsync(),
            "5 0 * * *",   // 00:05 UTC every day
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        app.Run();
    }
}
