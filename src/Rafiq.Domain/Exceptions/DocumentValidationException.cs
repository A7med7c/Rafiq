namespace Rafiq.Domain.Exceptions;

public class DocumentValidationException : BadRequestException
{
    public string ErrorCode { get; }

    public DocumentValidationException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
