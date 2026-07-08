namespace Rafiq.Domain.Exceptions;

public class ExternalServiceException : Exception
{
    public ExternalServiceException(string serviceName, string message, Exception? innerException = null)
        : base($"{serviceName} service error: {message}", innerException)
    {
        ServiceName = serviceName;
    }

    public string ServiceName { get; }
}
