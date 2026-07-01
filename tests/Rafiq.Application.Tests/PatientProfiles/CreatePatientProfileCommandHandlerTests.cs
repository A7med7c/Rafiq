using FluentAssertions;
using Moq;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.PatientProfiles.Commands.CreatePatientProfile;
using Rafiq.Domain.Entities;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Tests.PatientProfiles;

public sealed class CreatePatientProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenValid_CreatesProfileForCurrentUser()
    {
        var userId = Guid.NewGuid();
        var currentUserService = new Mock<ICurrentUserService>();
        var repository = new Mock<IPatientProfileRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        currentUserService.SetupGet(x => x.UserId).Returns(userId);

        PatientProfile? createdProfile = null;
        repository.Setup(x => x.AddAsync(It.IsAny<PatientProfile>(), It.IsAny<CancellationToken>()))
            .Callback<PatientProfile, CancellationToken>((profile, _) => createdProfile = profile)
            .Returns(Task.CompletedTask);

        var handler = new CreatePatientProfileCommandHandler(currentUserService.Object, repository.Object, unitOfWork.Object, TestMapperFactory.Create());
        var command = new CreatePatientProfileCommand("Ahmed Ragab", DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(-30)), "Male", "OPositive", null, null, "Emergency", "+201001234567");

        var response = await handler.Handle(command, CancellationToken.None);

        response.Success.Should().BeTrue();
        response.Data!.UserId.Should().Be(userId);
        createdProfile.Should().NotBeNull();
        createdProfile!.UserId.Should().Be(userId);
        command.EntityId.Should().Be(createdProfile.Id);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
