using DashboardCompras.Models;

namespace DashboardCompras.Services;

public interface IAuditoriaService
{
    Task<AuditoriaResumenDto> GetResumenAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AuditoriaErrorRowDto>> SearchErrorsAsync(AuditoriaErrorFilterDto filter, CancellationToken ct = default);
    Task<AuditoriaErrorRowDto?> GetErrorByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetUsuariosAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetProcesosAsync(CancellationToken ct = default);
}
