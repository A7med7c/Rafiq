namespace Rafiq.Domain.Exceptions;

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "You are not authorized to perform this action.")
        : base(message) { }
}
