using AlfaCore.Models;
using Microsoft.Data.SqlClient;

namespace AlfaCore.Services;

public sealed partial class ComprasDashboardService
{
    public async Task<FilterOptionsDto> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await MeasureAsync("GetFilterOptions", null, async () =>
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);

            return new FilterOptionsDto
            {
                Proveedores = await ReadDistinctAsync(connection, "SELECT DISTINCT TOP (50) RAZON_SOCIAL FROM vw_compras_cabecera_dashboard WHERE RAZON_SOCIAL <> '' ORDER BY RAZON_SOCIAL", cancellationToken),
                Articulos = await ReadDistinctAsync(connection, "SELECT DISTINCT TOP (50) DESCRIPCION_ARTICULO FROM vw_compras_detalle_dashboard WHERE DESCRIPCION_ARTICULO <> '' ORDER BY DESCRIPCION_ARTICULO", cancellationToken),
                Rubros = await ReadDistinctAsync(connection, "SELECT DISTINCT RUBRO FROM vw_compras_detalle_dashboard WHERE RUBRO <> '' ORDER BY RUBRO", cancellationToken),
                Familias = await ReadDistinctAsync(connection, "SELECT DISTINCT FAMILIA FROM vw_compras_detalle_dashboard WHERE FAMILIA <> '' ORDER BY FAMILIA", cancellationToken),
                Usuarios = await ReadDistinctAsync(connection, "SELECT DISTINCT USUARIO FROM vw_compras_cabecera_dashboard WHERE USUARIO <> '' ORDER BY USUARIO", cancellationToken),
                Sucursales = await ReadDistinctAsync(connection, "SELECT DISTINCT SUCURSAL FROM vw_compras_cabecera_dashboard WHERE SUCURSAL <> '' ORDER BY SUCURSAL", cancellationToken),
                Depositos = await ReadDistinctAsync(connection, "SELECT DISTINCT CONVERT(varchar(20), IdDeposito) FROM vw_compras_cabecera_dashboard WHERE IdDeposito IS NOT NULL ORDER BY CONVERT(varchar(20), IdDeposito)", cancellationToken),
                Estados = await ReadDistinctAsync(connection, "SELECT DISTINCT EstadoComprobante FROM vw_compras_cabecera_dashboard ORDER BY EstadoComprobante", cancellationToken),
                TiposComprobante = await ReadDistinctAsync(connection, "SELECT DISTINCT TC FROM vw_compras_cabecera_dashboard ORDER BY TC", cancellationToken)
            };
        });
    }

    public async Task<DashboardSummaryDto> GetKpiSummaryAsync(DashboardFilters filters, CancellationToken cancellationToken = default)
    {
        return await MeasureAsync("GetKpiSummary", filters, async () =>
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            return await ReadKpiSummaryAsync(connection, filters, cancellationToken);
        });
    }

    public async Task<DashboardSummaryDto> GetDashboardAsync(DashboardFilters filters, CancellationToken cancellationToken = default)
    {
        return await MeasureAsync("GetDashboard", filters, async () =>
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);

            var baseResumen = await ReadKpiSummaryAsync(connection, filters, cancellationToken);

            var total = baseResumen.TotalComprado == 0 ? 1 : baseResumen.TotalComprado;

            return new DashboardSummaryDto
            {
                TotalComprado = baseResumen.TotalComprado,
                TicketPromedio = baseResumen.TicketPromedio,
                CantidadComprobantes = baseResumen.CantidadComprobantes,
                ProveedoresActivos = baseResumen.ProveedoresActivos,
                CantidadArticulos = baseResumen.CantidadArticulos,
                NetoTotal = baseResumen.NetoTotal,
                IvaTotal = baseResumen.IvaTotal,
                EvolucionMensual = await GetMonthlySeriesAsync(connection, filters.WithoutDates(), "SUM(c.ImporteDashboard)", HeaderFromClause, "c.FECHA", cancellationToken),
                TopProveedores = await GetCategoryTotalsAsync(connection, filters, $"""
                    SELECT TOP (7)
                        COALESCE(NULLIF(c.RAZON_SOCIAL, ''), c.CUENTA) AS Categoria,
                        c.CUENTA AS Codigo,
                        SUM(c.ImporteDashboard) AS Total
                    {HeaderFromClause}
                    GROUP BY c.CUENTA, c.RAZON_SOCIAL
                    ORDER BY SUM(c.ImporteDashboard) DESC;
                    """, total, cancellationToken),
                TopRubros = await GetCategoryTotalsAsync(connection, filters, $"""
                    SELECT TOP (7)
                        COALESCE(NULLIF(d.RUBRO, ''), 'Sin rubro') AS Categoria,
                        d.RUBRO AS Codigo,
                        SUM(d.TotalDashboard) AS Total
                    {DetailFromClause}
                    GROUP BY d.RUBRO
                    ORDER BY SUM(d.TotalDashboard) DESC;
                    """, total, cancellationToken),
                TopArticulos = await GetCategoryTotalsAsync(connection, filters, $"""
                    SELECT TOP (7)
                        COALESCE(NULLIF(d.DESCRIPCION_ARTICULO, ''), d.IDARTICULO) AS Categoria,
                        d.IDARTICULO AS Codigo,
                        SUM(d.TotalDashboard) AS Total
                    {DetailFromClause}
                    GROUP BY d.IDARTICULO, d.DESCRIPCION_ARTICULO
                    ORDER BY SUM(d.TotalDashboard) DESC;
                    """, total, cancellationToken),
                Estados = await GetStatusMetricsAsync(connection, filters, cancellationToken)
            };
        });
    }

    private async Task<IReadOnlyList<StatusMetricDto>> GetStatusMetricsAsync(
        SqlConnection connection,
        DashboardFilters filters,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                c.EstadoComprobante AS Estado,
                COUNT(*) AS Cantidad,
                SUM(c.ImporteDashboard) AS Total
            {HeaderFromClause}
            GROUP BY c.EstadoComprobante
            ORDER BY COUNT(*) DESC;
            """;

        return await ReadListAsync(connection, sql, filters, reader => new StatusMetricDto
        {
            Estado = reader.SafeGetString("Estado"),
            Cantidad = reader.GetInt32("Cantidad"),
            Total = reader.GetDecimal("Total")
        }, cancellationToken);
    }

    private async Task<DashboardSummaryDto> ReadKpiSummaryAsync(
        SqlConnection connection,
        DashboardFilters filters,
        CancellationToken cancellationToken)
    {
        var sqlHeader = $"""
            SELECT
                ISNULL(SUM(c.ImporteDashboard), 0) AS TotalComprado,
                ISNULL(AVG(CAST(c.ImporteDashboard AS decimal(18,2))), 0) AS TicketPromedio,
                COUNT(*) AS CantidadComprobantes,
                COUNT(DISTINCT c.CUENTA) AS ProveedoresActivos,
                ISNULL(SUM(c.NetoDashboard), 0) AS NetoTotal,
                ISNULL(SUM(c.IvaDashboard), 0) AS IvaTotal
            {HeaderFromClause};
            """;

        var sqlArticulos = $"""
            SELECT ISNULL(COUNT(DISTINCT d.IDARTICULO), 0) AS CantidadArticulos
            {DetailFromClause};
            """;

        var header = await ReadSingleAsync(connection, sqlHeader, filters, reader => new
        {
            TotalComprado     = reader.GetDecimal("TotalComprado"),
            TicketPromedio    = reader.GetDecimal("TicketPromedio"),
            CantidadComp      = reader.GetInt32("CantidadComprobantes"),
            ProveedoresActivos = reader.GetInt32("ProveedoresActivos"),
            NetoTotal         = reader.GetDecimal("NetoTotal"),
            IvaTotal          = reader.GetDecimal("IvaTotal"),
        }, cancellationToken) ?? new
        {
            TotalComprado = 0m,
            TicketPromedio = 0m,
            CantidadComp = 0,
            ProveedoresActivos = 0,
            NetoTotal = 0m,
            IvaTotal = 0m
        };

        var cantidadArticulos = await ReadSingleAsync(connection, sqlArticulos, filters,
            reader => reader.GetInt32("CantidadArticulos"), cancellationToken);

        return new DashboardSummaryDto
        {
            TotalComprado = header.TotalComprado,
            TicketPromedio = header.TicketPromedio,
            CantidadComprobantes = header.CantidadComp,
            ProveedoresActivos = header.ProveedoresActivos,
            CantidadArticulos = cantidadArticulos,
            NetoTotal = header.NetoTotal,
            IvaTotal = header.IvaTotal
        };
    }
}
