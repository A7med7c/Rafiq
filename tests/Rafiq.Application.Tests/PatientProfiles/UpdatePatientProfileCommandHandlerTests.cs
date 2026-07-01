using FluentAssertions;
using Moq;
using Rafiq.Application.Features.PatientProfiles.Commands.UpdatePatientProfile;
using Rafiq.Domain.Entities;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Tests.PatientProfiles;

public sealed class UpdatePatientProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenProfileExists_UpdatesProfile()
    {
        var profile = new PatientProfile("Old Name", DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(-40)), Gender.Male, null, null, null, "Old Contact", "+201001234567", Guid.NewGuid());
        var repository = new Mock<IPatientProfileRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        repository.Setup(x => x.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var handler = new UpdatePatientProfileCommandHandler(repository.Object, unitOfWork.Object, TestMapperFactory.Create());
        var command = new UpdatePatientProfileCommand(profile.Id, "New Name", DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(-35)), "Female", "APositive", "None", "None", "New Contact", "+201009876543");

        var response = await handler.Handle(command, CancellationToken.None);

        response.Success.Should().BeTrue();
        response.Data!.FullName.Should().Be("New Name");
        response.Data.Gender.Should().Be("Female");
        profile.UpdatedAt.Should().NotBeNull();
        repository.Verify(x => x.Update(profile), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
