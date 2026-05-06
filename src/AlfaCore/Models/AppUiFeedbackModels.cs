namespace AlfaCore.Models;

public enum AppUiFeedbackSeverity
{
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3
}

public sealed class AppUiMessage
{
    public AppUiFeedbackSeverity Severity { get; init; } = AppUiFeedbackSeverity.Info;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Suggestion { get; init; } = string.Empty;

    public bool HasCode => !string.IsNullOrWhiteSpace(Code);
    public bool HasSuggestion => !string.IsNullOrWhiteSpace(Suggestion);

    public static AppUiMessage Success(string title, string message)
        => new()
        {
            Severity = AppUiFeedbackSeverity.Success,
            Title = title,
            Message = message
        };
}

public sealed class AppUiOperationResult
{
    public bool Success { get; init; }
    public AppUiMessage? Feedback { get; init; }

    public static AppUiOperationResult Ok()
        => new() { Success = true };

    public static AppUiOperationResult Fail(AppUiMessage feedback)
        => new() { Success = false, Feedback = feedback };
}

public sealed class AppUiOperationResult<T>
{
    public bool Success { get; init; }
    public T? Value { get; init; }
    public AppUiMessage? Feedback { get; init; }

    public static AppUiOperationResult<T> Ok(T value)
        => new() { Success = true, Value = value };

    public static AppUiOperationResult<T> Fail(AppUiMessage feedback)
        => new() { Success = false, Feedback = feedback };
}
