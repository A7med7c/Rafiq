using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Auth.Commands.Account;

public sealed class DeleteMyAccountCommandHandler(
    ICurrentUserService currentUserService,
    IIdentityService identityService,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    IAiConversationRepository aiConversationRepository)
    : IRequestHandler<DeleteMyAccountCommand, ApiResponse<string>>
{
    public async Task<ApiResponse<string>> Handle(DeleteMyAccountCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is null)
            throw new AuthenticationException("User is not authenticated.");

        var userId = currentUserService.UserId.Value;

        // All three calls use ExecuteUpdateAsync / ExecuteDeleteAsync, which issue raw SQL
        // and bypass the soft-delete interceptor in SaveChangesAsync. Each call auto-commits.

        // Preserve others' access history while releasing the FK pointer to the deleted user.
        await healthProfileAccessRepository.NullifyInvitedByUserIdAsync(userId, cancellationToken);

        // Hard-delete AI conversations — their Restrict FK on UserHealthProfileId would block
        // the cascade from ApplicationUser → UserHealthProfile.
        await aiConversationRepository.HardDeleteAllByUserIdAsync(userId, cancellationToken);

        // Hard-delete all HealthProfileAccess rows where the user is the grantee.
        // This removes the Restrict FK blocker on ApplicationUser.
        await healthProfileAccessRepository.HardDeleteByGranteeUserIdAsync(userId, cancellationToken);

        // Delete the identity user. SQL Server cascades:
        //   ApplicationUser → UserHealthProfile (Cascade)
        //     → all profile data (Cascade): allergies, diseases, medications, appointments, etc.
        //     → HealthProfileAccess rows for other users who had access to this profile (Cascade)
        //   ApplicationUser → RefreshTokens (Cascade)
        await identityService.DeleteUserAsync(userId, cancellationToken);

        return ApiResponse<string>.SuccessResponse("Account deleted successfully.", "Account deleted successfully.");
    }
}
