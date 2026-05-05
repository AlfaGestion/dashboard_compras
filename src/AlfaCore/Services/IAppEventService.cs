using AlfaCore.Models;

namespace AlfaCore.Services;

public interface IAppEventService
{
    Task<string> LogErrorAsync(
        string module,
        string action,
        Exception exception,
        string userMessage,
        object? data = null,
        AppEventSeverity severity = AppEventSeverity.Error,
        CancellationToken ct = default);

    Task<string> LogAuditAsync(
        string module,
        string action,
        string entityType,
        string entityId,
        string message,
        object? data = null,
        CancellationToken ct = default);
}
