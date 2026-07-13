using FluentAssertions;
using Moq;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.MedicationReminderEngine.Commands.ConfirmMedicationReminder;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Tests.MedicationReminders;

public sealed class ConfirmMedicationReminderCommandHandlerTests
{
    private static readonly Guid ProfileId    = Guid.NewGuid();
    private static readonly Guid ReminderId   = Guid.NewGuid();
    private static readonly DateOnly Today    = new(2026, 7, 13);

    private static MedicationReminderLog MakeLog(int number, MedicationReminderStatus status, string? jobId = null)
    {
        var log = new MedicationReminderLog(
            ReminderId,
            ProfileId,
            Today,
            TimeSpan.FromHours(20) + TimeSpan.FromMinutes((number - 1) * 10),
            number);

        switch (status)
        {
            case MedicationReminderStatus.Sent:      log.MarkAsSent();      break;
            case MedicationReminderStatus.Confirmed: log.MarkAsConfirmed(); break;
            case MedicationReminderStatus.Overdue:   log.MarkAsOverdue();   break;
            case MedicationReminderStatus.Cancelled: log.Cancel();          break;
        }

        if (jobId is not null)
            log.SetNextJobId(jobId);

        return log;
    }

    private static (ConfirmMedicationReminderCommandHandler Handler,
                    Mock<IMedicationReminderLogRepository> LogRepo,
                    Mock<IMedicationReminderScheduler> Scheduler,
                    Mock<IUnitOfWork> UnitOfWork)
        BuildHandler(MedicationReminderLog targetLog, List<MedicationReminderLog> pendingOthers)
    {
        var logRepo    = new Mock<IMedicationReminderLogRepository>();
        var authSvc    = new Mock<IHealthProfileAuthorizationService>();
        var scheduler  = new Mock<IMedicationReminderScheduler>();
        var unitOfWork = new Mock<IUnitOfWork>();

        logRepo.Setup(r => r.GetByIdAsync(targetLog.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(targetLog);

        logRepo.Setup(r => r.GetPendingOtherLogsAsync(
                   ReminderId, Today, targetLog.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(pendingOthers);

        var accessCtx = new HealthProfileAccessContext(ProfileId, Guid.NewGuid(), Guid.NewGuid(), AccessRole.Owner, AccessStatus.Active);
        authSvc.Setup(a => a.EnsureCanWriteAsync(ProfileId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(accessCtx);

        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(1);

        var handler = new ConfirmMedicationReminderCommandHandler(
            logRepo.Object, authSvc.Object, scheduler.Object, unitOfWork.Object);

        return (handler, logRepo, scheduler, unitOfWork);
    }

    // ── Happy-path confirmation ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_ConfirmsPendingLog_AndSavesChanges()
    {
        var log = MakeLog(1, MedicationReminderStatus.Pending);
        var (handler, logRepo, _, unitOfWork) = BuildHandler(log, []);

        var result = await handler.Handle(new ConfirmMedicationReminderCommand(log.Id), default);

        result.Success.Should().BeTrue();
        log.Status.Should().Be(MedicationReminderStatus.Confirmed);
        log.ConfirmedAt.Should().NotBeNull();
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ConfirmsSentLog_AndSavesChanges()
    {
        var log = MakeLog(2, MedicationReminderStatus.Sent);
        var (handler, _, _, unitOfWork) = BuildHandler(log, []);

        var result = await handler.Handle(new ConfirmMedicationReminderCommand(log.Id), default);

        result.Success.Should().BeTrue();
        log.Status.Should().Be(MedicationReminderStatus.Confirmed);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ConfirmsOverdueLog_AndSavesChanges()
    {
        var log = MakeLog(1, MedicationReminderStatus.Overdue);
        var (handler, _, _, _) = BuildHandler(log, []);

        var result = await handler.Handle(new ConfirmMedicationReminderCommand(log.Id), default);

        result.Success.Should().BeTrue();
        log.Status.Should().Be(MedicationReminderStatus.Confirmed);
    }

    // ── Pending sibling cancellation ──────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenStage1Confirmed_CancelsPendingStage2AndStage3()
    {
        var stage1 = MakeLog(1, MedicationReminderStatus.Pending, "job-1");
        var stage2 = MakeLog(2, MedicationReminderStatus.Pending, "job-2");
        var stage3 = MakeLog(3, MedicationReminderStatus.Pending, "job-3");
        var (handler, _, scheduler, _) = BuildHandler(stage1, [stage2, stage3]);

        await handler.Handle(new ConfirmMedicationReminderCommand(stage1.Id), default);

        stage2.Status.Should().Be(MedicationReminderStatus.Cancelled);
        stage3.Status.Should().Be(MedicationReminderStatus.Cancelled);
        scheduler.Verify(s => s.CancelJob("job-1"), Times.Once);
        scheduler.Verify(s => s.CancelJob("job-2"), Times.Once);
        scheduler.Verify(s => s.CancelJob("job-3"), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStage3Confirmed_CancelsPendingStage1AndStage2()
    {
        // Critical regression test: confirming the last stage must cancel earlier stages
        // so their Hangfire jobs do not fire and send spurious notifications.
        var stage1 = MakeLog(1, MedicationReminderStatus.Pending, "job-1");
        var stage2 = MakeLog(2, MedicationReminderStatus.Pending, "job-2");
        var stage3 = MakeLog(3, MedicationReminderStatus.Pending, "job-3");
        var (handler, _, scheduler, _) = BuildHandler(stage3, [stage1, stage2]);

        await handler.Handle(new ConfirmMedicationReminderCommand(stage3.Id), default);

        stage3.Status.Should().Be(MedicationReminderStatus.Confirmed);
        stage1.Status.Should().Be(MedicationReminderStatus.Cancelled);
        stage2.Status.Should().Be(MedicationReminderStatus.Cancelled);
        scheduler.Verify(s => s.CancelJob("job-1"), Times.Once);
        scheduler.Verify(s => s.CancelJob("job-2"), Times.Once);
        scheduler.Verify(s => s.CancelJob("job-3"), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStage2Confirmed_OnlyCancelsPendingStage3_LeavesStage1AsSent()
    {
        // Stage 1 is already Sent (notification delivered) — not in pendingOthers,
        // so it is not cancelled.  Its historical Sent status is preserved.
        var stage2 = MakeLog(2, MedicationReminderStatus.Sent, "job-2");
        var stage3 = MakeLog(3, MedicationReminderStatus.Pending, "job-3");
        var (handler, _, scheduler, _) = BuildHandler(stage2, [stage3]);

        await handler.Handle(new ConfirmMedicationReminderCommand(stage2.Id), default);

        stage2.Status.Should().Be(MedicationReminderStatus.Confirmed);
        stage3.Status.Should().Be(MedicationReminderStatus.Cancelled);
        scheduler.Verify(s => s.CancelJob("job-3"), Times.Once);
        // job-2's own job is cancelled in case it was confirmed before it fired
        scheduler.Verify(s => s.CancelJob("job-2"), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoPendingOthers_OnlyCancelsOwnJob()
    {
        var log = MakeLog(3, MedicationReminderStatus.Pending, "job-3");
        var (handler, _, scheduler, _) = BuildHandler(log, []);

        await handler.Handle(new ConfirmMedicationReminderCommand(log.Id), default);

        log.Status.Should().Be(MedicationReminderStatus.Confirmed);
        scheduler.Verify(s => s.CancelJob("job-3"), Times.Once);
        scheduler.Verify(s => s.CancelJob(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLogHasNoNextJobId_DoesNotCallCancelJob()
    {
        var log = MakeLog(1, MedicationReminderStatus.Pending); // no job ID
        var (handler, _, scheduler, _) = BuildHandler(log, []);

        await handler.Handle(new ConfirmMedicationReminderCommand(log.Id), default);

        scheduler.Verify(s => s.CancelJob(It.IsAny<string>()), Times.Never);
    }

    // ── Idempotent guards ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenAlreadyConfirmed_ReturnsFailureWithoutMutation()
    {
        var log = MakeLog(1, MedicationReminderStatus.Confirmed);
        var (handler, _, _, unitOfWork) = BuildHandler(log, []);

        var result = await handler.Handle(new ConfirmMedicationReminderCommand(log.Id), default);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already");
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCancelled_ReturnsFailureWithoutMutation()
    {
        var log = MakeLog(2, MedicationReminderStatus.Cancelled);
        var (handler, _, _, unitOfWork) = BuildHandler(log, []);

        var result = await handler.Handle(new ConfirmMedicationReminderCommand(log.Id), default);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no longer active");
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Not found ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenLogDoesNotExist_ThrowsNotFoundException()
    {
        var logRepo   = new Mock<IMedicationReminderLogRepository>();
        var authSvc   = new Mock<IHealthProfileAuthorizationService>();
        var scheduler = new Mock<IMedicationReminderScheduler>();
        var unitOfWork = new Mock<IUnitOfWork>();

        logRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((MedicationReminderLog?)null);

        var handler = new ConfirmMedicationReminderCommandHandler(
            logRepo.Object, authSvc.Object, scheduler.Object, unitOfWork.Object);

        await handler.Invoking(h => h.Handle(new ConfirmMedicationReminderCommand(Guid.NewGuid()), default))
                     .Should().ThrowAsync<NotFoundException>();
    }
}
