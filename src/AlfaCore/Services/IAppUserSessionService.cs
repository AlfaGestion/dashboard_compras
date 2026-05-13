using AlfaCore.Models;

namespace AlfaCore.Services;

public interface IAppUserSessionService
{
    event Action? StateChanged;

    bool IsAuthenticated { get; }
    AppUserSessionInfo? CurrentUser { get; }

    string? CurrentToken { get; }

    Task<AppUserSessionInfo> LoginAsync(string userName, string password, CancellationToken ct = default);
    bool TryRestoreFromToken(string token);
    void Logout();
    void HandleSqlSessionChanged();
    string GetCurrentUserName(string fallback = "");
}
