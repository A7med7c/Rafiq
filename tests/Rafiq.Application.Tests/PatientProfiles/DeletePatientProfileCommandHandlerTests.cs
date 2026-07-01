using FluentAssertions;
using Moq;
using Rafiq.Application.Features.PatientProfiles.Commands.DeletePatientProfile;
using Rafiq.Domain.Entities;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Tests.PatientProfiles;

public sealed class DeletePatientProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenProfileExists_RemovesProfileThroughRepository()
    {
        var profile = new PatientProfile("Ahmed Ragab", DateTime.UtcNow.Date.AddYears(-30), Gender.Male, null, null, null, "Emergency", "+201001234567", Guid.NewGuid());
        var repository = new Mock<IPatientProfileRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        repository.Setup(x => x.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var handler = new DeletePatientProfileCommandHandler(repository.Object, unitOfWork.Object);

        var response = await handler.Handle(new DeletePatientProfileCommand(profile.Id), CancellationToken.None);

        response.Success.Should().BeTrue();
        repository.Verify(x => x.Remove(profile), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
