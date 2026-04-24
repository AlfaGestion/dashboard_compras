using DashboardCompras.Models;
using System.Diagnostics;
using System.Text.Json;

namespace DashboardCompras.Services;

public sealed class AppEventService(
    IWebHostEnvironment env,
    IHttpContextAccessor httpContextAccessor,
    ISessionService sessionService,
    ILogger<AppEventService> logger) : IAppEventService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly string _logDirectory = Path.Combine(env.ContentRootPath, "App_Data", "diagnostics");
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<string> LogErrorAsync(
        string module,
        string action,
        Exception exception,
        string userMessage,
        object? data = null,
        AppEventSeverity severity = AppEventSeverity.Error,
        CancellationToken ct = default)
    {
        var eventId = Guid.NewGuid();
        var record = CreateBaseRecord(AppEventKind.Error, severity, module, action, userMessage, data, eventId);
        record.Message = exception.Message;
        record.ExceptionType = exception.GetType().FullName ?? exception.GetType().Name;
        record.ExceptionMessage = exception.ToString();
        record.StackTrace = exception.StackTrace ?? string.Empty;

        logger.LogError(exception, "[{EventId}] {Module}/{Action}: {UserMessage}", eventId, module, action, userMessage);
        await WriteAsync(record, ct);
        return eventId.ToString("N");
    }

    public async Task<string> LogAuditAsync(
        string module,
        string action,
        string entityType,
        string entityId,
        string message,
        object? data = null,
        CancellationToken ct = default)
    {
        var eventId = Guid.NewGuid();
        var record = CreateBaseRecord(AppEventKind.Audit, AppEventSeverity.Info, module, action, message, data, eventId);
        record.Message = message;
        record.EntityType = entityType;
        record.EntityId = entityId;

        logger.LogInformation("[{EventId}] AUDIT {Module}/{Action} {EntityType} {EntityId}: {Message}",
            eventId, module, action, entityType, entityId, message);
        await WriteAsync(record, ct);
        return eventId.ToString("N");
    }

    private AppEventRecord CreateBaseRecord(
        AppEventKind kind,
        AppEventSeverity severity,
        string module,
        string action,
        string userMessage,
        object? data,
        Guid eventId)
    {
        var http = httpContextAccessor.HttpContext;
        var session = sessionService.GetActiveSession();
        var userName = http?.User?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userName))
            userName = Environment.UserName;

        return new AppEventRecord
        {
            Id = eventId,
            Timestamp = DateTime.Now,
            Kind = kind,
            Severity = severity,
            Module = module,
            Action = action,
            UserName = userName ?? string.Empty,
            SessionServer = session?.Servidor ?? string.Empty,
            SessionDatabase = session?.BaseDatos ?? string.Empty,
            RequestPath = http?.Request.Path.Value ?? string.Empty,
            HttpMethod = http?.Request.Method ?? string.Empty,
            TraceId = Activity.Current?.TraceId.ToString() ?? http?.TraceIdentifier ?? string.Empty,
            CorrelationId = Activity.Current?.Id ?? http?.TraceIdentifier ?? eventId.ToString("N"),
            UserMessage = userMessage,
            DataJson = SerializeData(data)
        };
    }

    private async Task WriteAsync(AppEventRecord record, CancellationToken ct)
    {
        Directory.CreateDirectory(_logDirectory);
        var path = Path.Combine(_logDirectory, $"app-events-{DateTime.Now:yyyyMM}.jsonl");
        var line = JsonSerializer.Serialize(record, JsonOptions) + Environment.NewLine;

        await _gate.WaitAsync(ct);
        try
        {
            await File.AppendAllTextAsync(path, line, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string SerializeData(object? data)
    {
        if (data is null)
            return string.Empty;

        try
        {
            return JsonSerializer.Serialize(data, JsonOptions);
        }
        catch
        {
            return data.ToString() ?? string.Empty;
        }
    }
}
