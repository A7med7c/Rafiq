namespace Rafiq.Application.Common.Interfaces;

public interface IWhatsAppService
{
    /// <summary>
    /// Sends a WhatsApp template message via the Facebook Graph API.
    /// </summary>
    /// <param name="recipientPhoneNumber">Recipient in any Egyptian format; normalised internally.</param>
    /// <param name="templateName">Approved template name registered in WhatsApp Business Manager.</param>
    /// <param name="bodyParameters">Ordered list of text values that fill {{1}}, {{2}}, … body placeholders.</param>
    Task SendTemplateAsync(
        string recipientPhoneNumber,
        string templateName,
        List<string> bodyParameters,
        CancellationToken cancellationToken);
}
