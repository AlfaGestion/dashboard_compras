using DashboardCompras.Models;

namespace DashboardCompras.Services;

public sealed class GestionFilterStateService
{
    public VentasDashboardFilters Ventas { get; } = new();
    public StockDashboardFilters Stock { get; } = new();
    public CajaBancosDashboardFilters CajaBancos { get; } = new();
    public ContabilidadDashboardFilters Contabilidad { get; } = new();
}
