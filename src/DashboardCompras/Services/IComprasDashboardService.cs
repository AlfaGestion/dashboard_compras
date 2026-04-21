using DashboardCompras.Models;

namespace DashboardCompras.Services;

public interface IComprasDashboardService
{
    Task<FilterOptionsDto> GetFilterOptionsAsync(CancellationToken cancellationToken = default);
    Task<DashboardSummaryDto> GetDashboardAsync(DashboardFilters filters, CancellationToken cancellationToken = default);
    Task<DashboardSummaryDto> GetKpiSummaryAsync(DashboardFilters filters, CancellationToken cancellationToken = default);
    Task<ActividadPageDto> GetActividadPageAsync(DashboardFilters filters, CancellationToken cancellationToken = default);
    Task<ActividadUsuarioDetalleDto?> GetActividadUsuarioDetalleAsync(string usuario, DashboardFilters filters, CancellationToken cancellationToken = default);
    Task<ProveedoresPageDto> GetProveedoresPageDataAsync(DashboardFilters filters, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProveedorResumenDto>> GetProveedoresAsync(DashboardFilters filters, CancellationToken cancellationToken = default);
    Task<ProveedorDetalleDto?> GetProveedorDetalleAsync(string cuenta, DashboardFilters filters, CancellationToken cancellationToken = default);
    Task<ComprobantesOverviewDto> GetComprobantesOverviewAsync(ComprobantesFilter filter, CancellationToken cancellationToken = default);
    Task<ComprobantesResultDto> GetComprobantesAsync(ComprobantesFilter filter, CancellationToken cancellationToken = default);
    Task<ComprobanteDetalleDto?> GetComprobanteDetalleAsync(string tc, string idComprobante, string cuenta, CancellationToken cancellationToken = default);
    Task<RubrosPageDto> GetRubrosPageDataAsync(DashboardFilters filters, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RubroResumenDto>> GetRubrosAsync(DashboardFilters filters, CancellationToken cancellationToken = default);
    Task<RubroDetalleDto?> GetRubroDetalleAsync(string rubro, DashboardFilters filters, CancellationToken cancellationToken = default);
    Task<FamiliasPageDto> GetFamiliasPageDataAsync(DashboardFilters filters, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FamiliaResumenDto>> GetFamiliasAsync(DashboardFilters filters, CancellationToken cancellationToken = default);
    Task<FamiliaDetalleDto?> GetFamiliaDetalleAsync(string familia, DashboardFilters filters, CancellationToken cancellationToken = default);
    Task<ArticulosPageDto> GetArticulosPageDataAsync(DashboardFilters filters, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ArticuloResumenDto>> GetArticulosAsync(DashboardFilters filters, CancellationToken cancellationToken = default);
    Task<ArticuloDetalleDto?> GetArticuloDetalleAsync(string idArticulo, DashboardFilters filters, CancellationToken cancellationToken = default);
}
