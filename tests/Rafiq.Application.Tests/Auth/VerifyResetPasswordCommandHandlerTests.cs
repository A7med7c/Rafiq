using FluentAssertions;
using Moq;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.Auth.Commands.ResetPassword;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.Tests.Auth;

public sealed class VerifyResetPasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        var identityService = new Mock<IIdentityService>();
        identityService.Setup(x => x.GetByEmailAsync("missing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentityUserDto?)null);

        var handler = new VerifyResetPasswordCommandHandler(
            identityService.Object,
            Mock.Of<IOtpService>(),
            Mock.Of<IResetTokenService>());

        await handler.Invoking(x => x.Handle(new VerifyResetPasswordCommand("missing@example.com", "123456"), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenOtpIsExpiredOrWrong_PropagatesValidationException()
    {
        var user = new IdentityUserDto(Guid.NewGuid(), "patient@example.com", "01001234567", "User", false, null, EmailConfirmed: true);
        var identityService = new Mock<IIdentityService>();
        identityService.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var otpService = new Mock<IOtpService>();
        otpService.Setup(x => x.VerifyOtpAsync(user.UserId, "000000", OtpPurpose.PasswordReset, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Invalid OTP."));

        var handler = new VerifyResetPasswordCommandHandler(
            identityService.Object,
            otpService.Object,
            Mock.Of<IResetTokenService>());

        await handler.Invoking(x => x.Handle(new VerifyResetPasswordCommand(user.Email, "000000"), CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_WhenOtpIsValid_ReturnsResetToken()
    {
        var user = new IdentityUserDto(Guid.NewGuid(), "patient@example.com", "01001234567", "User", false, null, EmailConfirmed: true);
        var identityService = new Mock<IIdentityService>();
        identityService.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var otpService = new Mock<IOtpService>();
        otpService.Setup(x => x.VerifyOtpAsync(user.UserId, "123456", OtpPurpose.PasswordReset, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var resetTokenService = new Mock<IResetTokenService>();
        resetTokenService.Setup(x => x.GenerateResetToken(user.UserId)).Returns("reset-token");

        var handler = new VerifyResetPasswordCommandHandler(identityService.Object, otpService.Object, resetTokenService.Object);

        var response = await handler.Handle(new VerifyResetPasswordCommand(user.Email, "123456"), CancellationToken.None);

        response.Success.Should().BeTrue();
        response.Data!.ResetToken.Should().Be("reset-token");
    }
}
