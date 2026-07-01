using FluentAssertions;
using Moq;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.Auth.Commands.RevokeToken;
using Rafiq.Domain.Entities;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Tests.Auth;

public sealed class RevokeTokenCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenTokenBelongsToCurrentUser_RevokesToken()
    {
        var userId = Guid.NewGuid();
        var token = new RefreshToken("token-hash", "jti", userId, DateTime.UtcNow.AddDays(1), "web", "127.0.0.1");
        var repository = new Mock<IRefreshTokenRepository>();
        var tokenService = new Mock<ITokenService>();
        var currentUserService = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();

        currentUserService.SetupGet(x => x.UserId).Returns(userId);
        tokenService.Setup(x => x.HashRefreshToken("refresh")).Returns("token-hash");
        repository.Setup(x => x.GetByTokenHashAsync("token-hash", It.IsAny<CancellationToken>())).ReturnsAsync(token);

        var handler = new RevokeTokenCommandHandler(repository.Object, tokenService.Object, currentUserService.Object, unitOfWork.Object);

        var response = await handler.Handle(new RevokeTokenCommand("refresh", "127.0.0.1"), CancellationToken.None);

        response.Success.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
