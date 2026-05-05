namespace AlfaCore.Models;

public sealed class AuxErrEntry
{
    public string Process { get; init; } = string.Empty;
    public int ErrorCode { get; init; }
    public string Description { get; init; } = string.Empty;
    public string SqlDetail { get; init; } = string.Empty;
    public string Pc { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
}
