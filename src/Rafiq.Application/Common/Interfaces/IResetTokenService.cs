namespace Rafiq.Application.Common.Interfaces
{
    public interface IResetTokenService
    {
        string GenerateResetToken(Guid userId);

        Guid ValidateResetToken(string token);
    }
}
