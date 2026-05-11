using AlfaCore.Models;

namespace AlfaCore.Services;

public sealed class AppValidationException : InvalidOperationException
{
    public AppValidationException(string userMessage, ValidationResult validation)
        : base(userMessage)
    {
        UserMessage = userMessage;
        Validation = validation;
    }

    public string UserMessage { get; }
    public ValidationResult Validation { get; }
}
