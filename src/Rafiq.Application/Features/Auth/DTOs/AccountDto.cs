namespace Rafiq.Application.Features.Auth.DTOs
{
    public sealed record AccountDto(
        Guid UserId,
        string FirstName,
        string LastName,
        string Email,
        string PhoneNumber,
        bool PhoneNumberConfirmed,
        string Role);
}
