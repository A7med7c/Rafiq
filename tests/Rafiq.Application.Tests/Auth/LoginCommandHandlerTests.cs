using FluentAssertions;
using Moq;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.Auth.Commands.Login;
using Rafiq.Application.Features.Auth.DTOs;
using AuthenticationException = Rafiq.Domain.Exceptions.AuthenticationException;

namespace Rafiq.Application.Tests.Auth;

public sealed class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCredentialsAreInvalid_ThrowsAuthenticationException()
    {
        var identityService = new Mock<IIdentityService>();
        identityService.Setup(x => x.ValidateCredentialsAsync("patient@example.com", "wrong", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentityUserDto?)null);

        var handler = new LoginCommandHandler(identityService.Object, Mock.Of<ITokenIssuingService>());

        await handler.Invoking(x => x.Handle(new LoginCommand("patient@example.com", "wrong"), CancellationToken.None))
            .Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task Handle_WhenEmailNotConfirmed_ThrowsAuthenticationException()
    {
        var user = new IdentityUserDto(Guid.NewGuid(), "patient@example.com", "01001234567", "User", false, null, EmailConfirmed: false);
        var identityService = new Mock<IIdentityService>();
        identityService.Setup(x => x.ValidateCredentialsAsync(user.Email, "Password1!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new LoginCommandHandler(identityService.Object, Mock.Of<ITokenIssuingService>());

        await handler.Invoking(x => x.Handle(new LoginCommand(user.Email, "Password1!"), CancellationToken.None))
            .Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*verify your email*");
    }

    [Fact]
    public async Task Handle_WhenEmailConfirmed_IssuesTokens()
    {
        var user = new IdentityUserDto(Guid.NewGuid(), "patient@example.com", "01001234567", "User", false, null, EmailConfirmed: true);
        var identityService = new Mock<IIdentityService>();
        identityService.Setup(x => x.ValidateCredentialsAsync(user.Email, "Password1!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var authResponse = new AuthResponseDto("access-token", "refresh-token", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddDays(7), false, null);
        var tokenIssuingService = new Mock<ITokenIssuingService>();
        tokenIssuingService.Setup(x => x.IssueTokensAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResponse);

        var handler = new LoginCommandHandler(identityService.Object, tokenIssuingService.Object);

        var response = await handler.Handle(new LoginCommand(user.Email, "Password1!"), CancellationToken.None);

        response.Success.Should().BeTrue();
        response.Data!.AccessToken.Should().Be("access-token");
    }
}
