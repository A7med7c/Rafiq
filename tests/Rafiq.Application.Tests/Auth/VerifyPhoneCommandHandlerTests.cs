using FluentAssertions;
using Moq;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.Auth.Commands.PhoneNumber;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.Tests.Auth;

public sealed class VerifyPhoneCommandHandlerTests
{
    private static IdentityUserDto CreateUser(bool emailConfirmed = false) =>
        new(Guid.NewGuid(), "patient@example.com", "01001234567", "User", false, null, emailConfirmed);

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        var identityService = new Mock<IIdentityService>();
        identityService.Setup(x => x.GetByEmailAsync("missing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentityUserDto?)null);

        var handler = new VerifyPhoneCommandHandler(identityService.Object, Mock.Of<IOtpService>());
        var command = new VerifyPhoneCommand("missing@example.com", "123456", OtpPurpose.EmailVerification);

        await handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAlreadyVerified_ThrowsConflictException()
    {
        var user = CreateUser(emailConfirmed: true);
        var identityService = new Mock<IIdentityService>();
        identityService.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new VerifyPhoneCommandHandler(identityService.Object, Mock.Of<IOtpService>());
        var command = new VerifyPhoneCommand(user.Email, "123456", OtpPurpose.EmailVerification);

        await handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenOtpIsExpiredOrWrong_PropagatesValidationException()
    {
        var user = CreateUser();
        var identityService = new Mock<IIdentityService>();
        identityService.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var otpService = new Mock<IOtpService>();
        otpService.Setup(x => x.VerifyOtpAsync(user.UserId, "000000", OtpPurpose.EmailVerification, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("OTP has expired or maximum verification attempts have been reached."));

        var handler = new VerifyPhoneCommandHandler(identityService.Object, otpService.Object);
        var command = new VerifyPhoneCommand(user.Email, "000000", OtpPurpose.EmailVerification);

        await handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();

        identityService.Verify(x => x.ConfirmEmailAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOtpIsValid_ConfirmsEmail()
    {
        var user = CreateUser();
        var identityService = new Mock<IIdentityService>();
        identityService.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var otpService = new Mock<IOtpService>();
        otpService.Setup(x => x.VerifyOtpAsync(user.UserId, "123456", OtpPurpose.EmailVerification, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new VerifyPhoneCommandHandler(identityService.Object, otpService.Object);
        var command = new VerifyPhoneCommand(user.Email, "123456", OtpPurpose.EmailVerification);

        var response = await handler.Handle(command, CancellationToken.None);

        response.Success.Should().BeTrue();
        identityService.Verify(x => x.ConfirmEmailAsync(user.UserId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
