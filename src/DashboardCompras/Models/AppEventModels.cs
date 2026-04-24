namespace DashboardCompras.Models;

public enum AppEventKind
{
    Error,
    Audit
}

public enum AppEventSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public sealed class AppEventRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public AppEventKind Kind { get; set; }
    public AppEventSeverity Severity { get; set; } = AppEventSeverity.Info;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string SessionServer { get; set; } = string.Empty;
    public string SessionDatabase { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string ExceptionMessage { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public string DataJson { get; set; } = string.Empty;
}
