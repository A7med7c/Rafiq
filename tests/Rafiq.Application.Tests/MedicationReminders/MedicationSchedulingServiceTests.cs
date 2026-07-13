using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;
using Rafiq.Infrastructure.Services.MedicationReminders;

namespace Rafiq.Application.Tests.MedicationReminders;

public sealed class MedicationSchedulingServiceTests
{
    // ── Fixtures ─────────────────────────────────────────────────────────────

    private static readonly DateOnly Today      = new(2026, 7, 13);
    private static readonly Guid    ProfileId   = Guid.NewGuid();
    private static readonly Guid    MedicineId  = Guid.NewGuid();

    /// <summary>
    /// Creates a service wired to a fixed "now" that is well before the given reminder time,
    /// so all three attempt logs are scheduled for future delivery (none become Overdue).
    /// </summary>
    private static (MedicationSchedulingService Service,
                    List<MedicationReminderLog> CapturedLogs,
                    Mock<IMedicationReminderLogRepository> LogRepo)
        BuildService(
            TimeSpan reminderTime,
            MedicationReminderOptions? opts = null)
    {
        // Place UtcNow 1 hour before the reminder so the grace window is never hit.
        var utcNow   = new DateTime(2026, 7, 13, reminderTime.Hours - 1, 0, 0, DateTimeKind.Utc);
        var anchorUtc = new DateTime(2026, 7, 13, reminderTime.Hours, reminderTime.Minutes, 0, DateTimeKind.Utc);

        var dateTimeProv = new Mock<IDateTimeProvider>();
        dateTimeProv.Setup(d => d.Today).Returns(Today);
        dateTimeProv.Setup(d => d.UtcNow).Returns(utcNow);
        dateTimeProv.Setup(d => d.ToUtc(Today, reminderTime)).Returns(anchorUtc);

        var capturedLogs = new List<MedicationReminderLog>();
        var logRepo      = new Mock<IMedicationReminderLogRepository>();
        logRepo.Setup(r => r.ExistsForDateAsync(It.IsAny<Guid>(), Today, It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);
        logRepo.Setup(r => r.AddAsync(It.IsAny<MedicationReminderLog>(), It.IsAny<CancellationToken>()))
               .Callback<MedicationReminderLog, CancellationToken>((log, _) => capturedLogs.Add(log))
               .Returns(Task.CompletedTask);

        var userMedicineMock = new UserMedicine(ProfileId, "Paracetamol", "500mg", "Twice daily", "7 days", null, null, MedicineSource.Manual);
        var medRepo = new Mock<IUserMedicineRepository>();
        medRepo.Setup(r => r.GetByIdAsync(MedicineId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(userMedicineMock);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var jobClient = new Mock<IBackgroundJobClient>();
        jobClient.Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job-id");

        var options = Options.Create(opts ?? new MedicationReminderOptions { LateGraceMinutes = 5 });

        var service = new MedicationSchedulingService(
            logRepo.Object,
            medRepo.Object,
            unitOfWork.Object,
            jobClient.Object,
            dateTimeProv.Object,
            options,
            NullLogger<MedicationSchedulingService>.Instance);

        return (service, capturedLogs, logRepo);
    }

    private static MedicineReminder MakeReminder(TimeSpan reminderTime)
    {
        var today = Today;
        return new MedicineReminder(MedicineId, reminderTime, today, today.AddYears(1), RepeatType.Daily);
    }

    // ── Escalation timing ─────────────────────────────────────────────────────

    [Fact]
    public async Task ScheduleToday_Creates3Logs_WithCorrectEscalationOffsets()
    {
        var reminderTime = new TimeSpan(20, 0, 0); // 8:00 PM
        var (svc, logs, _) = BuildService(reminderTime);

        await svc.ScheduleTodayIfApplicableAsync(MakeReminder(reminderTime));

        logs.Should().HaveCount(3);

        // Attempt 1 fires exactly at the configured reminder time.
        logs[0].ReminderNumber.Should().Be(1);
        logs[0].ScheduledTime.Should().Be(new TimeSpan(20, 0, 0));

        // Attempt 2 fires 10 minutes later.
        logs[1].ReminderNumber.Should().Be(2);
        logs[1].ScheduledTime.Should().Be(new TimeSpan(20, 10, 0));

        // Attempt 3 fires 20 minutes after the configured time.
        logs[2].ReminderNumber.Should().Be(3);
        logs[2].ScheduledTime.Should().Be(new TimeSpan(20, 20, 0));
    }

    [Fact]
    public async Task ScheduleToday_Attempt1IsAtConfiguredReminderTime_NotBefore()
    {
        var reminderTime = new TimeSpan(8, 0, 0); // 8:00 AM
        var (svc, logs, _) = BuildService(reminderTime);

        await svc.ScheduleTodayIfApplicableAsync(MakeReminder(reminderTime));

        // Stage 1 must NOT fire before the user's configured time.
        logs[0].ScheduledTime.Should().Be(reminderTime);
        logs[0].ScheduledTime.Should().BeGreaterThanOrEqualTo(reminderTime);
    }

    [Fact]
    public async Task ScheduleToday_AllLogsHavePendingStatus_WhenAllFutureAttempts()
    {
        var reminderTime = new TimeSpan(20, 0, 0);
        var (svc, logs, _) = BuildService(reminderTime);

        await svc.ScheduleTodayIfApplicableAsync(MakeReminder(reminderTime));

        logs.Should().AllSatisfy(l => l.Status.Should().Be(MedicationReminderStatus.Pending));
    }

    [Fact]
    public async Task ScheduleToday_AssignsReminderNumbers1_2_3_InOrder()
    {
        var reminderTime = new TimeSpan(14, 0, 0);
        var (svc, logs, _) = BuildService(reminderTime);

        await svc.ScheduleTodayIfApplicableAsync(MakeReminder(reminderTime));

        logs.Select(l => l.ReminderNumber).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task ScheduleToday_AllLogsShareTheSameMedicineReminderId()
    {
        var reminderTime = new TimeSpan(9, 30, 0);
        var (svc, logs, _) = BuildService(reminderTime);
        var reminder = MakeReminder(reminderTime);

        await svc.ScheduleTodayIfApplicableAsync(reminder);

        logs.Should().AllSatisfy(l => l.MedicineReminderId.Should().Be(reminder.Id));
    }

    // ── Midnight boundary ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(23, 50)]   // 11:50 PM → Stage 3 at 00:10 wraps past midnight
    [InlineData(23, 55)]   // 11:55 PM → Stage 2 at 00:05, Stage 3 at 00:15
    [InlineData(23, 40)]   // 11:40 PM → Stage 2 at 23:50, Stage 3 at 00:00 (midnight)
    public async Task ScheduleToday_NearMidnight_ClampsScheduledTimeToEndOfDay(int hour, int minute)
    {
        var reminderTime = new TimeSpan(hour, minute, 0);
        var utcNow   = new DateTime(2026, 7, 13, hour - 1, 0, 0, DateTimeKind.Utc);
        var anchorUtc = new DateTime(2026, 7, 13, hour, minute, 0, DateTimeKind.Utc);

        var dateTimeProv = new Mock<IDateTimeProvider>();
        dateTimeProv.Setup(d => d.Today).Returns(Today);
        dateTimeProv.Setup(d => d.UtcNow).Returns(utcNow);
        dateTimeProv.Setup(d => d.ToUtc(Today, reminderTime)).Returns(anchorUtc);

        var capturedLogs = new List<MedicationReminderLog>();
        var logRepo      = new Mock<IMedicationReminderLogRepository>();
        logRepo.Setup(r => r.ExistsForDateAsync(It.IsAny<Guid>(), Today, It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);
        logRepo.Setup(r => r.AddAsync(It.IsAny<MedicationReminderLog>(), It.IsAny<CancellationToken>()))
               .Callback<MedicationReminderLog, CancellationToken>((log, _) => capturedLogs.Add(log))
               .Returns(Task.CompletedTask);

        var medRepo = new Mock<IUserMedicineRepository>();
        medRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new UserMedicine(ProfileId, "Med", "500mg", "Once", "7d", null, null, MedicineSource.Manual));

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var jobClient = new Mock<IBackgroundJobClient>();
        jobClient.Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job");

        var svc = new MedicationSchedulingService(
            logRepo.Object, medRepo.Object, unitOfWork.Object, jobClient.Object,
            dateTimeProv.Object,
            Options.Create(new MedicationReminderOptions { LateGraceMinutes = 5 }),
            NullLogger<MedicationSchedulingService>.Instance);

        await svc.ScheduleTodayIfApplicableAsync(MakeReminder(reminderTime));

        capturedLogs.Should().HaveCount(3);

        // No ScheduledTime should overflow past a valid day boundary.
        capturedLogs.Should().AllSatisfy(l =>
        {
            l.ScheduledTime.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
            l.ScheduledTime.Should().BeLessThan(TimeSpan.FromDays(1));
        });
    }

    // ── Grace window / Overdue behaviour ─────────────────────────────────────

    [Fact]
    public async Task ScheduleToday_WhenAttempt1IsWithinGrace_StillSchedulesNotification()
    {
        var reminderTime = new TimeSpan(20, 0, 0);
        // 3 minutes past Stage 1 — within the 5-min grace window
        var utcNow    = new DateTime(2026, 7, 13, 20, 3, 0, DateTimeKind.Utc);
        var anchorUtc = new DateTime(2026, 7, 13, 20, 0, 0, DateTimeKind.Utc);

        var dateTimeProv = new Mock<IDateTimeProvider>();
        dateTimeProv.Setup(d => d.Today).Returns(Today);
        dateTimeProv.Setup(d => d.UtcNow).Returns(utcNow);
        dateTimeProv.Setup(d => d.ToUtc(Today, reminderTime)).Returns(anchorUtc);

        var capturedLogs = new List<MedicationReminderLog>();
        var logRepo      = new Mock<IMedicationReminderLogRepository>();
        logRepo.Setup(r => r.ExistsForDateAsync(It.IsAny<Guid>(), Today, It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);
        logRepo.Setup(r => r.AddAsync(It.IsAny<MedicationReminderLog>(), It.IsAny<CancellationToken>()))
               .Callback<MedicationReminderLog, CancellationToken>((log, _) => capturedLogs.Add(log))
               .Returns(Task.CompletedTask);

        var medRepo = new Mock<IUserMedicineRepository>();
        medRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new UserMedicine(ProfileId, "Med", "500mg", "Once", "7d", null, null, MedicineSource.Manual));

        var jobClient  = new Mock<IBackgroundJobClient>();
        jobClient.Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job");
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var svc = new MedicationSchedulingService(
            logRepo.Object, medRepo.Object, unitOfWork.Object, jobClient.Object,
            dateTimeProv.Object,
            Options.Create(new MedicationReminderOptions { LateGraceMinutes = 5 }),
            NullLogger<MedicationSchedulingService>.Instance);

        await svc.ScheduleTodayIfApplicableAsync(MakeReminder(reminderTime));

        // Stage 1 is 3 min past — within grace — still Pending (fires immediately).
        capturedLogs[0].Status.Should().Be(MedicationReminderStatus.Pending);
        // Stages 2 and 3 are still in the future.
        capturedLogs[1].Status.Should().Be(MedicationReminderStatus.Pending);
        capturedLogs[2].Status.Should().Be(MedicationReminderStatus.Pending);
    }

    [Fact]
    public async Task ScheduleToday_WhenAttempt1IsBeyondGrace_MarksItOverdue()
    {
        var reminderTime = new TimeSpan(20, 0, 0);
        // 8 minutes past Stage 1 — beyond the 5-min grace window
        var utcNow    = new DateTime(2026, 7, 13, 20, 8, 0, DateTimeKind.Utc);
        var anchorUtc = new DateTime(2026, 7, 13, 20, 0, 0, DateTimeKind.Utc);

        var dateTimeProv = new Mock<IDateTimeProvider>();
        dateTimeProv.Setup(d => d.Today).Returns(Today);
        dateTimeProv.Setup(d => d.UtcNow).Returns(utcNow);
        dateTimeProv.Setup(d => d.ToUtc(Today, reminderTime)).Returns(anchorUtc);

        var capturedLogs = new List<MedicationReminderLog>();
        var logRepo      = new Mock<IMedicationReminderLogRepository>();
        logRepo.Setup(r => r.ExistsForDateAsync(It.IsAny<Guid>(), Today, It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);
        logRepo.Setup(r => r.AddAsync(It.IsAny<MedicationReminderLog>(), It.IsAny<CancellationToken>()))
               .Callback<MedicationReminderLog, CancellationToken>((log, _) => capturedLogs.Add(log))
               .Returns(Task.CompletedTask);

        var medRepo = new Mock<IUserMedicineRepository>();
        medRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new UserMedicine(ProfileId, "Med", "500mg", "Once", "7d", null, null, MedicineSource.Manual));

        var jobClient  = new Mock<IBackgroundJobClient>();
        jobClient.Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job");
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var svc = new MedicationSchedulingService(
            logRepo.Object, medRepo.Object, unitOfWork.Object, jobClient.Object,
            dateTimeProv.Object,
            Options.Create(new MedicationReminderOptions { LateGraceMinutes = 5 }),
            NullLogger<MedicationSchedulingService>.Instance);

        await svc.ScheduleTodayIfApplicableAsync(MakeReminder(reminderTime));

        // Stage 1 is 8 min past, beyond grace → Overdue, no job.
        capturedLogs[0].Status.Should().Be(MedicationReminderStatus.Overdue);
        // Stages 2 (at 20:10) and 3 (at 20:20) are still 2 and 12 min away → Pending.
        capturedLogs[1].Status.Should().Be(MedicationReminderStatus.Pending);
        capturedLogs[2].Status.Should().Be(MedicationReminderStatus.Pending);
    }

    // ── Duplicate guard ───────────────────────────────────────────────────────

    [Fact]
    public async Task ScheduleToday_WhenAlreadyScheduled_ReturnsFalseAndCreatesNoLogs()
    {
        var reminderTime = new TimeSpan(10, 0, 0);
        var (svc, logs, logRepo) = BuildService(reminderTime);

        logRepo.Setup(r => r.ExistsForDateAsync(It.IsAny<Guid>(), Today, It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

        var result = await svc.ScheduleTodayIfApplicableAsync(MakeReminder(reminderTime));

        result.Should().BeFalse();
        logs.Should().BeEmpty();
    }

    // ── Applicability checks ──────────────────────────────────────────────────

    [Fact]
    public async Task ScheduleToday_WhenReminderDisabled_ReturnsFalse()
    {
        var reminderTime = new TimeSpan(10, 0, 0);
        var (svc, logs, _) = BuildService(reminderTime);
        var reminder = MakeReminder(reminderTime);
        reminder.ToggleStatus(); // IsEnabled → false

        var result = await svc.ScheduleTodayIfApplicableAsync(reminder);

        result.Should().BeFalse();
        logs.Should().BeEmpty();
    }

    [Fact]
    public async Task ScheduleToday_WhenRepeatTypeOnce_OnlySchedulesOnStartDate()
    {
        var reminderTime = new TimeSpan(10, 0, 0);
        var (svc, logs, _) = BuildService(reminderTime);
        // Reminder set for a different start date than today
        var reminder = new MedicineReminder(
            MedicineId, reminderTime,
            Today.AddDays(1), Today.AddDays(1),
            RepeatType.Once);

        var result = await svc.ScheduleTodayIfApplicableAsync(reminder);

        result.Should().BeFalse();
        logs.Should().BeEmpty();
    }
}
