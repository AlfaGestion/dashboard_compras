namespace AlfaCore.Services;

public sealed class AppUserFacingException : InvalidOperationException
{
    public AppUserFacingException(string userMessage, string errorCode, Exception? innerException = null)
        : base(BuildMessage(userMessage, errorCode), innerException)
    {
        UserMessage = userMessage;
        ErrorCode = errorCode;
    }

    public string UserMessage { get; }
    public string ErrorCode { get; }

    private static string BuildMessage(string userMessage, string errorCode)
        => string.IsNullOrWhiteSpace(errorCode)
            ? userMessage
            : $"{userMessage} Código: {errorCode}";
}
