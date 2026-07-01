namespace Rafiq.Domain.Exceptions;

public class AuthenticationException : Exception
{
    public AuthenticationException(string message = "Authentication failed.")
        : base(message) { }
}
