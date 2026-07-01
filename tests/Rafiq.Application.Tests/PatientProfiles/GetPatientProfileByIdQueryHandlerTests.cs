using FluentAssertions;
using Moq;
using Rafiq.Application.Features.PatientProfiles.Queries.GetPatientProfileById;
using Rafiq.Domain.Entities;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Tests.PatientProfiles;

public sealed class GetPatientProfileByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenProfileExists_ReturnsProfile()
    {
        var profile = new PatientProfile("Ahmed Ragab", DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(-30)), Gender.Male, BloodType.OPositive, null, null, "Emergency", "+201001234567", Guid.NewGuid());
        var repository = new Mock<IPatientProfileRepository>();
        repository.Setup(x => x.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var handler = new GetPatientProfileByIdQueryHandler(repository.Object, TestMapperFactory.Create());

        var response = await handler.Handle(new GetPatientProfileByIdQuery(profile.Id), CancellationToken.None);

        response.Success.Should().BeTrue();
        response.Data!.Id.Should().Be(profile.Id);
    }
}
