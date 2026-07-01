using FluentAssertions;
using Moq;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.PatientProfiles.Queries.GetMyPatientProfile;
using Rafiq.Domain.Entities;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Tests.PatientProfiles;

public sealed class GetMyPatientProfileQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenCurrentUserHasProfile_ReturnsProfile()
    {
        var userId = Guid.NewGuid();
        var profile = new PatientProfile("Ahmed Ragab", DateTime.UtcNow.Date.AddYears(-30), Gender.Male, null, null, null, "Emergency", "+201001234567", userId);
        var currentUserService = new Mock<ICurrentUserService>();
        var repository = new Mock<IPatientProfileRepository>();
        currentUserService.SetupGet(x => x.UserId).Returns(userId);
        repository.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var handler = new GetMyPatientProfileQueryHandler(currentUserService.Object, repository.Object, TestMapperFactory.Create());

        var response = await handler.Handle(new GetMyPatientProfileQuery(), CancellationToken.None);

        response.Success.Should().BeTrue();
        response.Data!.UserId.Should().Be(userId);
    }
}
