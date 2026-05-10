using AlfaCore.Models;

namespace AlfaCore.Services;

public interface IInterfacesConfigService
{
    Task<InterfacesUploadSettingsDto> GetUploadSettingsAsync(CancellationToken ct = default);
    Task SaveUploadSettingsAsync(InterfacesUploadSettingsDto settings, CancellationToken ct = default);
}
