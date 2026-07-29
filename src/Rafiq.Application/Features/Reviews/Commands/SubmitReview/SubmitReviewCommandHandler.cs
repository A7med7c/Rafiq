using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Entities;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Reviews.Commands.SubmitReview;

public sealed class SubmitReviewCommandHandler : IRequestHandler<SubmitReviewCommand, ApiResponse<bool>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;
    private readonly IAppReviewRepository _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitReviewCommandHandler(
        ICurrentUserService currentUserService,
        IIdentityService identityService,
        IAppReviewRepository reviewRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _identityService = identityService;
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<bool>> Handle(SubmitReviewCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var account = await _identityService.GetAccountAsync(userId, cancellationToken);
        var displayName = account?.FirstName?.Trim() is { Length: > 0 } name ? name : "Anonymous";

        var review = new AppReview(userId, displayName, request.Stars, request.Comment);
        await _reviewRepository.AddAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "Thank you for your review!");
    }
}
