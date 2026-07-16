namespace Rafiq.Infrastructure.Services.Notifications.EmailTemplates
{
    /// <summary>
    /// Builds inline-CSS, table-based HTML emails for maximum client compatibility
    /// (Gmail, Outlook, Apple Mail, Yahoo Mail). All Rafiq emails share the same
    /// branded layout via <see cref="Layout"/>; callers only supply the body content.
    /// </summary>
    internal static class EmailTemplateBuilder
    {
        private const string PrimaryColor = "#4E89B8";
        private const string AccentColor = "#49d7ff";
        private const string BackgroundColor = "#eef8ff";
        private const string CardColor = "#ffffff";
        private const string TextColor = "#08264a";
        private const string MutedTextColor = "#6b7c93";

        /// <summary>
        /// Wraps arbitrary body HTML in the shared Rafiq branded email shell
        /// (header, card, footer). Reusable for OTP emails today, and for
        /// future welcome / appointment / medication-reminder emails.
        /// </summary>
        public static string Layout(string previewText, string bodyHtml) => $"""
            <!doctype html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Rafiq</title>
            </head>
            <body style="margin:0;padding:0;background-color:{BackgroundColor};font-family:'Segoe UI',Tahoma,Arial,sans-serif;">
                <div style="display:none;max-height:0;overflow:hidden;opacity:0;">{previewText}</div>
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:{BackgroundColor};padding:32px 16px;">
                    <tr>
                        <td align="center">
                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:480px;background-color:{CardColor};border-radius:20px;overflow:hidden;box-shadow:0 24px 60px rgba(8,38,74,.08);">
                                <tr>
                                    <td align="center" style="background:linear-gradient(135deg,{PrimaryColor} 0%,{AccentColor} 100%);padding:28px 24px;">
                                        <span style="font-size:24px;font-weight:700;color:#ffffff;letter-spacing:.5px;">Rafiq</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:32px 32px 24px 32px;color:{TextColor};font-size:15px;line-height:1.6;">
                                        {bodyHtml}
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:20px 32px;background-color:{BackgroundColor};text-align:center;color:{MutedTextColor};font-size:12px;line-height:1.5;">
                                        &copy; {DateTime.UtcNow.Year} Rafiq. All rights reserved.<br>
                                        This is an automated message, please do not reply.
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;

        /// <summary>
        /// Body content for OTP-based emails (email verification, password reset).
        /// </summary>
        public static string OtpBody(string displayName, string introText, string otpCode, int expirationMinutes) => $"""
            <p style="margin:0 0 12px 0;">Hello {displayName},</p>
            <p style="margin:0 0 20px 0;">{introText}</p>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:0 0 20px 0;">
                <tr>
                    <td align="center" style="background-color:{BackgroundColor};border:1px dashed {PrimaryColor};border-radius:14px;padding:20px;">
                        <span style="font-size:32px;font-weight:700;letter-spacing:8px;color:{PrimaryColor};">{otpCode}</span>
                    </td>
                </tr>
            </table>
            <p style="margin:0 0 8px 0;color:{MutedTextColor};font-size:13px;">
                This code expires in {expirationMinutes} minutes.
            </p>
            <p style="margin:0;color:{MutedTextColor};font-size:13px;">
                If you didn't request this, you can safely ignore this email.
            </p>
            <p style="margin:24px 0 0 0;">Thank you,<br>Rafiq Team</p>
            """;
    }
}
