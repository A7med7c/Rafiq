//using FluentAssertions;
//using Moq;
//using Rafiq.Application.Common.Interfaces;
//using Rafiq.Application.Features.Auth.Commands.RefreshToken;
//using Rafiq.Application.Features.Auth.DTOs;
//using Rafiq.Domain.Entities;
//using Rafiq.Domain.Exceptions;
//using Rafiq.Domain.Repositories;

//namespace Rafiq.Application.Tests.Auth;

//public sealed class RefreshTokenCommandHandlerTests
//{
//    [Fact]
//    public async Task Handle_WhenTokenIsMissing_ThrowsUnauthorizedException()
//    {
//        var repository = new Mock<IRefreshTokenRepository>();
//        var tokenService = new Mock<ITokenService>();
//        tokenService.Setup(x => x.HashRefreshToken("missing")).Returns("hash");

//        var handler = new RefreshTokenCommandHandler(
//            repository.Object,
//            Mock.Of<IIdentityService>(),
//            tokenService.Object,
//            Mock.Of<IUnitOfWork>());

//        await handler.Invoking(x => x.Handle(new RefreshTokenCommand("missing"), CancellationToken.None))
//            .Should().ThrowAsync<UnauthorizedException>();
//    }

//    [Fact]
//    public async Task Handle_WhenTokenIsActive_RotatesRefreshToken()
//    {
//        var userId = Guid.NewGuid();
//        var user = new IdentityUserDto(userId, "patient@example.com", "+201001234567", "Patient");
//        var token = new RefreshToken("old-hash", "old-jti", userId, DateTime.UtcNow.AddDays(1), "web", "127.0.0.1");

//        var repository = new Mock<IRefreshTokenRepository>();
//        var identityService = new Mock<IIdentityService>();
//        var tokenService = new Mock<ITokenService>();
//        var unitOfWork = new Mock<IUnitOfWork>();
//        tokenService.Setup(x => x.HashRefreshToken("old-refresh")).Returns("old-hash");
//        tokenService.Setup(x => x.GenerateRefreshToken()).Returns("new-refresh");
//        tokenService.Setup(x => x.HashRefreshToken("new-refresh")).Returns("new-hash");
//        tokenService.Setup(x => x.GenerateAccessToken(user.UserId, user.Email, user.Role, It.IsAny<string>(), It.IsAny<DateTime>()))
//            .Returns("new-access");
//        repository.Setup(x => x.GetByTokenHashAsync("old-hash", It.IsAny<CancellationToken>())).ReturnsAsync(token);
//        identityService.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

//        var handler = new RefreshTokenCommandHandler(repository.Object, identityService.Object, tokenService.Object, unitOfWork.Object);

//        var response = await handler.Handle(new RefreshTokenCommand("old-refresh", "web", "127.0.0.1"), CancellationToken.None);

//        response.Success.Should().BeTrue();
//        response.Data!.AccessToken.Should().Be("new-access");
//        response.Data.RefreshToken.Should().Be("new-refresh");
//        token.IsRevoked.Should().BeTrue();
//        repository.Verify(x => x.AddAsync(It.Is<RefreshToken>(t => t.Token == "new-hash" && t.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
//        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
//    }
//}
