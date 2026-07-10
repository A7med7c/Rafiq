using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;

namespace Rafiq.Application.Features.PatientProfiles.Queries.GetProfileMembers;

public sealed record GetProfileMembersQuery(Guid ProfileId)
    : IRequest<ApiResponse<IReadOnlyList<ProfileMemberDto>>>;
