namespace Rafiq.Application.Common.Interfaces
{
    public interface ITokenHasher
    {
        string Hash(string token);
    }
}