using AlfaCore.Models;

namespace AlfaCore.Services;

public interface IUsuariosService
{
    Task<IReadOnlyList<UsuarioGridItemDto>> SearchAsync(UsuariosFilters filters, CancellationToken ct = default);
    Task<UsuarioDetailDto?> GetByIdAsync(string nombre, CancellationToken ct = default);
    Task<string> SaveAsync(UsuarioSaveRequest request, CancellationToken ct = default);
    Task DeactivateAsync(string nombre, CancellationToken ct = default);
    Task<UsuarioPhotoServeDto?> GetPhotoForServeAsync(string nombre, CancellationToken ct = default);
}
