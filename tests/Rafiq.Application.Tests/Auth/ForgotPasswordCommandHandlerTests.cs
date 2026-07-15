using FluentAssertions;
using Moq;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.Auth.Commands.ForgetPassword;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Tests.Auth;

public sealed class ForgotPasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsSuccessWithoutSendingOtp()
    {
        var identityService = new Mock<IIdentityService>();
        identityService.Setup(x => x.GetByEmailAsync("missing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentityUserDto?)null);

        var otpService = new Mock<IOtpService>();
        var handler = new ForgotPasswordCommandHandler(identityService.Object, otpService.Object);

        var response = await handler.Handle(new ForgotPasswordCommand("missing@example.com"), CancellationToken.None);

        response.Success.Should().BeTrue();
        otpService.Verify(
            x => x.SendOtpAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<OtpPurpose>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserExists_SendsPasswordResetOtpToEmail()
    {
        var user = new IdentityUserDto(Guid.NewGuid(), "patient@example.com", "01001234567", "User", false, null, EmailConfirmed: true);
        var identityService = new Mock<IIdentityService>();
        identityService.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var otpService = new Mock<IOtpService>();
        var handler = new ForgotPasswordCommandHandler(identityService.Object, otpService.Object);

        var response = await handler.Handle(new ForgotPasswordCommand(user.Email), CancellationToken.None);

        response.Success.Should().BeTrue();
        otpService.Verify(
            x => x.SendOtpAsync(user.UserId, user.Email, OtpPurpose.PasswordReset, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
