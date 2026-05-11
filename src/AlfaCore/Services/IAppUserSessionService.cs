using AlfaCore.Models;

namespace AlfaCore.Services;

public interface IAppUserSessionService
{
    event Action? StateChanged;

    bool IsAuthenticated { get; }
    AppUserSessionInfo? CurrentUser { get; }

    Task<AppUserSessionInfo> LoginAsync(string userName, string password, CancellationToken ct = default);
    void Logout();
    void HandleSqlSessionChanged();
    string GetCurrentUserName(string fallback = "");
}
