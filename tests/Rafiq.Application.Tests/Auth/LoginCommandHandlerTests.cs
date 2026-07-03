//using FluentAssertions;
//using Moq;
//using Rafiq.Application.Common.Interfaces;
//using Rafiq.Application.Features.Auth.Commands.Login;
//using Rafiq.Application.Features.Auth.DTOs;
//using Rafiq.Domain.Entities;
//using Rafiq.Domain.Repositories;
//using AuthenticationException = Rafiq.Domain.Exceptions.AuthenticationException;

//namespace Rafiq.Application.Tests.Auth;

//public sealed class LoginCommandHandlerTests
//{
//    [Fact]
//    public async Task Handle_WhenCredentialsAreInvalid_ThrowsAuthenticationException()
//    {
//        var identityService = new Mock<IIdentityService>();
//        identityService.Setup(x => x.ValidateCredentialsAsync("patient@example.com", "wrong", It.IsAny<CancellationToken>()))
//            .ReturnsAsync((IdentityUserDto?)null);

//        var handler = new LoginCommandHandler(
//            identityService.Object,
//            Mock.Of<IRefreshTokenRepository>(),
//            Mock.Of<ITokenService>(),
//            Mock.Of<IUnitOfWork>());

//        await handler.Invoking(x => x.Handle(new LoginCommand("patient@example.com", "wrong"), CancellationToken.None))
//            .Should().ThrowAsync<AuthenticationException>();
//    }

//    [Fact]
//    public async Task Handle_WhenCredentialsAreValid_IssuesTokens()
//    {
//        var userId = Guid.NewGuid();
//        var user = new IdentityUserDto(userId, "patient@example.com", "+201001234567", "Patient");
//        var identityService = new Mock<IIdentityService>();
//        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
//        var tokenService = new Mock<ITokenService>();
//        var unitOfWork = new Mock<IUnitOfWork>();

//        identityService.Setup(x => x.ValidateCredentialsAsync(user.Email, "Password1!", It.IsAny<CancellationToken>()))
//            .ReturnsAsync(user);
//        tokenService.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
//        tokenService.Setup(x => x.HashRefreshToken("refresh-token")).Returns("refresh-token-hash");
//        tokenService.Setup(x => x.GenerateAccessToken(user.UserId, user.Email, user.Role, It.IsAny<string>(), It.IsAny<DateTime>()))
//            .Returns("access-token");

//        var handler = new LoginCommandHandler(identityService.Object, refreshTokenRepository.Object, tokenService.Object, unitOfWork.Object);

//        var response = await handler.Handle(new LoginCommand(user.Email, "Password1!", "web", "127.0.0.1"), CancellationToken.None);

//        response.Success.Should().BeTrue();
//        response.Data!.AccessToken.Should().Be("access-token");
//        response.Data.RefreshToken.Should().Be("refresh-token");
//        refreshTokenRepository.Verify(x => x.AddAsync(It.Is<RefreshToken>(t => t.Token == "refresh-token-hash" && t.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
//        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
//    }
//}
