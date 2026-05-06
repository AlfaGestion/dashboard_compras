using AlfaCore.Models;

namespace AlfaCore.Services;

public interface IAppUiOperationService
{
    Task<AppUiOperationResult> RunAsync(Func<Task> operation, string fallbackTitle, CancellationToken ct = default);
    Task<AppUiOperationResult<T>> RunAsync<T>(Func<Task<T>> operation, string fallbackTitle, CancellationToken ct = default);
    AppUiMessage BuildMessage(Exception exception, string fallbackTitle);
}
