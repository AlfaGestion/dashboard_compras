using AlfaCore.Models;

namespace AlfaCore.Services;

public interface IUsuariosService
{
    Task<PagedResult<UsuarioGridItemDto>> SearchAsync(UsuariosFilters filters, CancellationToken ct = default);
    Task<UsuarioDetailDto?> GetByIdAsync(string nombre, CancellationToken ct = default);
    Task<string> SaveAsync(UsuarioSaveRequest request, CancellationToken ct = default);
    Task DeactivateAsync(string nombre, CancellationToken ct = default);
    Task<UsuarioPhotoServeDto?> GetPhotoForServeAsync(string nombre, CancellationToken ct = default);
    Task<UsuariosViewSettingsDto> GetViewSettingsAsync(string userName, CancellationToken ct = default);
    Task SaveViewSettingsAsync(string userName, UsuariosViewSettingsDto settings, CancellationToken ct = default);
}
