namespace AlfaCore.Configuration;

public sealed class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    public string VerifyToken { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
    public string BusinessAccountId { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "v22.0";
    public string WebhookPath { get; set; } = "/api/conversaciones/whatsapp/webhook";

    public bool IsConfiguredForSend =>
        !string.IsNullOrWhiteSpace(AccessToken) &&
        !string.IsNullOrWhiteSpace(PhoneNumberId);

    public bool IsConfiguredForVerify =>
        !string.IsNullOrWhiteSpace(VerifyToken);
}
