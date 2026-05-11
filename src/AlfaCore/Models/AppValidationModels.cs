namespace AlfaCore.Models;

public sealed class ValidationIssue
{
    public string FieldKey { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public sealed class ValidationResult
{
    private readonly List<ValidationIssue> _issues = [];

    public IReadOnlyList<ValidationIssue> Issues => _issues;
    public bool IsValid => _issues.Count == 0;

    public void Add(string fieldKey, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        _issues.Add(new ValidationIssue
        {
            FieldKey = fieldKey?.Trim() ?? string.Empty,
            Message = message.Trim()
        });
    }
}
