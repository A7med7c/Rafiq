using System.Security.Cryptography;
using System.Text;
using Rafiq.Application.Common.Interfaces;

namespace Rafiq.Infrastructure.Services.auth
{
    public sealed class Sha256TokenHasher : ITokenHasher
    {
        public string Hash(string token)
        {
            var bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(token));

            return Convert.ToHexString(bytes);
        }
    }
}