using AlfaCore.Models;

namespace AlfaCore.Services;

/// <summary>
/// Servicio scoped que persiste el último filtro usado durante la sesión del usuario.
/// Por defecto usa el mes actual para evitar cargar todo el historial.
/// </summary>
public sealed class FilterStateService
{
    public DashboardFilters Current { get; } = CreateDefault();

    private static DashboardFilters CreateDefault()
    {
        var today = DateTime.Today;
        return new DashboardFilters
        {
            FechaDesde = new DateTime(today.Year, today.Month, 1),
            FechaHasta = today
        };
    }
}
