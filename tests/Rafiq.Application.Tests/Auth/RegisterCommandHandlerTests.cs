using FluentAssertions;
using Moq;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.Auth.Commands.Register;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.Tests.Auth;

public sealed class RegisterCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenEmailExists_ThrowsConflictException()
    {
        var identityService = new Mock<IIdentityService>();
        identityService.Setup(x => x.EmailExistsAsync("patient@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new RegisterCommandHandler(identityService.Object);
        var command = new RegisterCommand("Patient", "User", "patient@example.com", "01001234567", "Password1!", "Password1!", "Patient");

        await handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesIdentityUserAndReturnsResponse()
    {
        var identityService = new Mock<IIdentityService>();
        var userId = Guid.NewGuid();
        identityService.Setup(x => x.RegisterAsync(
                "Patient",
                "User",
                "patient@example.com",
                "01001234567",
                "Password1!",
                "Patient",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisterResponseDto(userId, "patient@example.com", "01001234567", "Patient"));

        var handler = new RegisterCommandHandler(identityService.Object);
        var command = new RegisterCommand("Patient", "User", "patient@example.com", "01001234567", "Password1!", "Password1!", "Patient");

        var response = await handler.Handle(command, CancellationToken.None);

        response.Success.Should().BeTrue();
        response.Data!.UserId.Should().Be(userId);
        response.Data.Email.Should().Be("patient@example.com");
        response.Data.Role.Should().Be("Patient");
        identityService.Verify(x => x.RegisterAsync(command.FirstName, command.LastName, command.Email, command.PhoneNumber, command.Password, command.Role, It.IsAny<CancellationToken>()), Times.Once);
    }
}
