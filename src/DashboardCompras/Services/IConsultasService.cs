using DashboardCompras.Models;

namespace DashboardCompras.Services;

public interface IConsultasService
{
    Task<IReadOnlyList<GrupoConsultasDto>> GetGruposAsync(CancellationToken ct = default);
    Task<IReadOnlyList<NodoArbolDto>> GetArbolAsync(CancellationToken ct = default);
    Task<ConsultaGuardadaDto?> GetConsultaAsync(int id, CancellationToken ct = default);
    Task<ConsultaResultadoDto> EjecutarAsync(EjecutarConsultaRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetGruposNombresAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetVistasDisponiblesAsync(string? filtro = null, CancellationToken ct = default);
    Task<IReadOnlyList<ColumnaDto>> GetColumnasAsync(string tabla, CancellationToken ct = default);
    Task<int> GuardarConsultaAsync(GuardarConsultaRequest request, CancellationToken ct = default);
    Task EliminarConsultaAsync(int id, CancellationToken ct = default);
    Task<string> GetSiguienteClaveAsync(string? parentClave, CancellationToken ct = default);
}
