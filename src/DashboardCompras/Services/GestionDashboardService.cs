using DashboardCompras.Models;
using Microsoft.Data.SqlClient;
using System.Globalization;

namespace DashboardCompras.Services;

public sealed class GestionDashboardService(
    IConfiguration configuration,
    ISessionService sessionService,
    IAppEventService appEvents) : IGestionDashboardService
{
    private const string VentasDocumentosWhere = """
        AND (
            c.TC LIKE 'FC%'
            OR c.TC LIKE 'NC%'
            OR c.TC LIKE 'ND%'
            OR c.TC LIKE 'FP%'
        )
        """;

    private string ConnectionString => sessionService.GetConnectionString().Length > 0
        ? sessionService.GetConnectionString()
        : configuration.GetConnectionString("AlfaGestion")
          ?? throw new InvalidOperationException("No se configuró la cadena de conexión 'ConnectionStrings:AlfaGestion'.");

    public Task<VentasFilterOptionsDto> GetVentasFilterOptionsAsync(CancellationToken ct = default)
        => ExecuteLoggedAsync("Ventas", "GetFilterOptions", async token =>
        {
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);

            return new VentasFilterOptionsDto
            {
                Usuarios = await QueryDistinctAsync("""
                    SELECT DISTINCT TOP (100) LTRIM(RTRIM(c.Usuario))
                    FROM dbo.V_MV_Cpte c
                    INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                    WHERE UPPER(LTRIM(RTRIM(tc.SISTEMA))) = 'VENTAS'
                      AND (
                          c.TC LIKE 'FC%'
                          OR c.TC LIKE 'NC%'
                          OR c.TC LIKE 'ND%'
                          OR c.TC LIKE 'FP%'
                      )
                      AND ISNULL(LTRIM(RTRIM(c.Usuario)), '') <> ''
                    ORDER BY LTRIM(RTRIM(c.Usuario))
                    """, cn, token),
                Sucursales = await QueryDistinctAsync("""
                    SELECT DISTINCT TOP (100) CONVERT(varchar(50), c.UNEGOCIO)
                    FROM dbo.V_MV_Cpte c
                    INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                    WHERE UPPER(LTRIM(RTRIM(tc.SISTEMA))) = 'VENTAS'
                      AND (
                          c.TC LIKE 'FC%'
                          OR c.TC LIKE 'NC%'
                          OR c.TC LIKE 'ND%'
                          OR c.TC LIKE 'FP%'
                      )
                      AND c.UNEGOCIO IS NOT NULL
                    ORDER BY CONVERT(varchar(50), c.UNEGOCIO)
                    """, cn, token),
                Depositos = await QueryDistinctAsync("""
                    SELECT DISTINCT TOP (100) CONVERT(varchar(50), c.IdDeposito)
                    FROM dbo.V_MV_Cpte c
                    INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                    WHERE UPPER(LTRIM(RTRIM(tc.SISTEMA))) = 'VENTAS'
                      AND (
                          c.TC LIKE 'FC%'
                          OR c.TC LIKE 'NC%'
                          OR c.TC LIKE 'ND%'
                          OR c.TC LIKE 'FP%'
                      )
                      AND c.IdDeposito IS NOT NULL
                    ORDER BY CONVERT(varchar(50), c.IdDeposito)
                    """, cn, token),
                TiposComprobante = await QueryDistinctAsync("""
                    SELECT DISTINCT TOP (100) c.TC
                    FROM dbo.V_MV_Cpte c
                    INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                    WHERE UPPER(LTRIM(RTRIM(tc.SISTEMA))) = 'VENTAS'
                      AND (
                          c.TC LIKE 'FC%'
                          OR c.TC LIKE 'NC%'
                          OR c.TC LIKE 'ND%'
                          OR c.TC LIKE 'FP%'
                      )
                      AND ISNULL(LTRIM(RTRIM(c.TC)), '') <> ''
                    ORDER BY c.TC
                    """, cn, token)
            };
        }, "No se pudieron cargar los filtros de ventas.", ct);

    public Task<StockFilterOptionsDto> GetStockFilterOptionsAsync(CancellationToken ct = default)
        => ExecuteLoggedAsync("Stock", "GetFilterOptions", async token =>
        {
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);

            return new StockFilterOptionsDto
            {
                Rubros = await QueryDistinctAsync("""
                    SELECT DISTINCT TOP (100) CONVERT(varchar(50), a.IDRUBRO)
                    FROM dbo.V_MA_ARTICULOS a
                    WHERE a.IDRUBRO IS NOT NULL
                    ORDER BY CONVERT(varchar(50), a.IDRUBRO)
                    """, cn, token),
                Familias = await QueryDistinctAsync("""
                    SELECT DISTINCT TOP (100) CONVERT(varchar(50), a.IdFamilia)
                    FROM dbo.V_MA_ARTICULOS a
                    WHERE a.IdFamilia IS NOT NULL
                    ORDER BY CONVERT(varchar(50), a.IdFamilia)
                    """, cn, token),
                Depositos = await QueryDistinctAsync("""
                    SELECT DISTINCT TOP (100) CONVERT(varchar(50), s.IdDeposito)
                    FROM dbo.V_MV_STOCK s
                    WHERE s.IdDeposito IS NOT NULL
                    ORDER BY CONVERT(varchar(50), s.IdDeposito)
                    """, cn, token),
                Sucursales = await QueryDistinctAsync("""
                    SELECT DISTINCT TOP (100) CONVERT(varchar(50), s.UNEGOCIO)
                    FROM dbo.V_MV_STOCK s
                    WHERE s.UNEGOCIO IS NOT NULL
                    ORDER BY CONVERT(varchar(50), s.UNEGOCIO)
                    """, cn, token),
                Estados = await QueryDistinctAsync("""
                    SELECT DISTINCT TOP (100) LTRIM(RTRIM(s.Estado))
                    FROM dbo.V_MV_STOCK s
                    WHERE ISNULL(LTRIM(RTRIM(s.Estado)), '') <> ''
                    ORDER BY LTRIM(RTRIM(s.Estado))
                    """, cn, token)
            };
        }, "No se pudieron cargar los filtros de stock.", ct);

    public Task<CajaBancosFilterOptionsDto> GetCajaBancosFilterOptionsAsync(CancellationToken ct = default)
        => ExecuteLoggedAsync("CajaBancos", "GetFilterOptions", async token =>
        {
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);

            return new CajaBancosFilterOptionsDto
            {
                Cajas = await QueryDistinctAsync("""
                    SELECT DISTINCT TOP (100) ISNULL(NULLIF(LTRIM(RTRIM(c.Descripcion)), ''), CONVERT(varchar(50), c.IdCajas))
                    FROM dbo.VT_CONSOLIDADO_CAJA c
                    ORDER BY ISNULL(NULLIF(LTRIM(RTRIM(c.Descripcion)), ''), CONVERT(varchar(50), c.IdCajas))
                    """, cn, token),
                Bancos = await QueryDistinctAsync("""
                    SELECT DISTINCT TOP (100) LTRIM(RTRIM(b.CUENTA))
                    FROM dbo.V_EstadoBancario b
                    WHERE UPPER(LTRIM(RTRIM(b.TipoVista))) = 'BC'
                      AND ISNULL(LTRIM(RTRIM(b.CUENTA)), '') <> ''
                    ORDER BY LTRIM(RTRIM(b.CUENTA))
                    """, cn, token)
            };
        }, "No se pudieron cargar los filtros de caja y bancos.", ct);

    public Task<ContabilidadFilterOptionsDto> GetContabilidadFilterOptionsAsync(CancellationToken ct = default)
        => ExecuteLoggedAsync("Contabilidad", "GetFilterOptions", async token =>
        {
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);

            return new ContabilidadFilterOptionsDto
            {
                Usuarios = await QueryDistinctAsync("""
                    SELECT DISTINCT TOP (100) LTRIM(RTRIM(a.USUARIO_LOGEADO))
                    FROM dbo.MV_ASIENTOS a
                    WHERE ISNULL(LTRIM(RTRIM(a.USUARIO_LOGEADO)), '') <> ''
                    ORDER BY LTRIM(RTRIM(a.USUARIO_LOGEADO))
                    """, cn, token),
                Sucursales = await QueryDistinctAsync("""
                    SELECT DISTINCT TOP (100) CONVERT(varchar(50), a.UNEGOCIO)
                    FROM dbo.MV_ASIENTOS a
                    WHERE a.UNEGOCIO IS NOT NULL
                    ORDER BY CONVERT(varchar(50), a.UNEGOCIO)
                    """, cn, token),
                Tipos = await QueryDistinctAsync("""
                    SELECT DISTINCT TOP (10) LTRIM(RTRIM(a.[DEBE-HABER]))
                    FROM dbo.MV_ASIENTOS a
                    WHERE ISNULL(LTRIM(RTRIM(a.[DEBE-HABER])), '') <> ''
                    ORDER BY LTRIM(RTRIM(a.[DEBE-HABER]))
                    """, cn, token)
            };
        }, "No se pudieron cargar los filtros de contabilidad.", ct);

    public async Task<VentasDashboardDto> GetVentasAsync(VentasDashboardFilters filters, CancellationToken ct = default)
    {
        filters ??= new VentasDashboardFilters();

        return await ExecuteLoggedAsync("Ventas", "GetDashboard", async token =>
        {
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);

            const string ventasWhere = """
                WHERE UPPER(LTRIM(RTRIM(tc.SISTEMA))) = 'VENTAS'
                  AND (
                      c.TC LIKE 'FC%'
                      OR c.TC LIKE 'NC%'
                      OR c.TC LIKE 'ND%'
                      OR c.TC LIKE 'FP%'
                  )
                  AND (@FechaDesde IS NULL OR c.FECHA >= @FechaDesde)
                  AND (@FechaHastaExclusive IS NULL OR c.FECHA < @FechaHastaExclusive)
                  AND (@ClienteLike IS NULL OR c.CUENTA LIKE @ClienteLike OR c.NOMBRE LIKE @ClienteLike)
                  AND (@Usuario IS NULL OR c.Usuario = @Usuario)
                  AND (@Sucursal IS NULL OR CONVERT(varchar(50), c.UNEGOCIO) = @Sucursal)
                  AND (@Deposito IS NULL OR CONVERT(varchar(50), c.IdDeposito) = @Deposito)
                  AND (@TipoComprobante IS NULL OR c.TC = @TipoComprobante)
                """;

            var kpis = await QuerySingleAsync($"""
                SELECT
                    ISNULL(SUM(c.IMPORTE),0) AS TotalFacturado,
                    ISNULL(AVG(c.IMPORTE),0) AS TicketPromedio,
                    COUNT(*) AS Comprobantes,
                    COUNT(DISTINCT c.CUENTA) AS ClientesActivos
                FROM dbo.V_MV_Cpte c
                INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                {ventasWhere}
                """, cn, r => new VentasDashboardDto
                {
                    TotalFacturadoMes = GetDecimal(r, 0),
                    TicketPromedioMes = GetDecimal(r, 1),
                    ComprobantesMes = GetInt(r, 2),
                    ClientesActivos = GetInt(r, 3)
                }, cmd => BindVentasFilters(cmd, filters), token) ?? new VentasDashboardDto();

            var evolucion = await QueryMonthlyAsync("""
                WITH Meses AS (
                    SELECT DATEADD(month, -11, DATEFROMPARTS(YEAR(DATEADD(day, -1, ISNULL(@FechaHastaExclusive, DATEADD(day, 1, GETDATE())))), MONTH(DATEADD(day, -1, ISNULL(@FechaHastaExclusive, DATEADD(day, 1, GETDATE())))), 1)) AS MesInicio
                    UNION ALL
                    SELECT DATEADD(month, 1, MesInicio)
                    FROM Meses
                    WHERE MesInicio < DATEFROMPARTS(YEAR(DATEADD(day, -1, ISNULL(@FechaHastaExclusive, DATEADD(day, 1, GETDATE())))), MONTH(DATEADD(day, -1, ISNULL(@FechaHastaExclusive, DATEADD(day, 1, GETDATE())))), 1)
                ),
                VentasMensuales AS (
                    SELECT
                        DATEFROMPARTS(YEAR(c.FECHA), MONTH(c.FECHA), 1) AS MesInicio,
                        SUM(c.IMPORTE) AS Total
                    FROM dbo.V_MV_Cpte c
                    INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                    WHERE UPPER(LTRIM(RTRIM(tc.SISTEMA))) = 'VENTAS'
                      AND (
                          c.TC LIKE 'FC%'
                          OR c.TC LIKE 'NC%'
                          OR c.TC LIKE 'ND%'
                          OR c.TC LIKE 'FP%'
                      )
                      AND c.FECHA >= DATEADD(month, -11, DATEFROMPARTS(YEAR(DATEADD(day, -1, ISNULL(@FechaHastaExclusive, DATEADD(day, 1, GETDATE())))), MONTH(DATEADD(day, -1, ISNULL(@FechaHastaExclusive, DATEADD(day, 1, GETDATE())))), 1))
                      AND c.FECHA < ISNULL(@FechaHastaExclusive, DATEADD(day, 1, GETDATE()))
                      AND (@ClienteLike IS NULL OR c.CUENTA LIKE @ClienteLike OR c.NOMBRE LIKE @ClienteLike)
                      AND (@Usuario IS NULL OR c.Usuario = @Usuario)
                      AND (@Sucursal IS NULL OR CONVERT(varchar(50), c.UNEGOCIO) = @Sucursal)
                      AND (@Deposito IS NULL OR CONVERT(varchar(50), c.IdDeposito) = @Deposito)
                      AND (@TipoComprobante IS NULL OR c.TC = @TipoComprobante)
                    GROUP BY DATEFROMPARTS(YEAR(c.FECHA), MONTH(c.FECHA), 1)
                )
                SELECT
                    CONVERT(varchar(7), m.MesInicio, 120) AS Periodo,
                    ISNULL(v.Total, 0) AS Total
                FROM Meses m
                LEFT JOIN VentasMensuales v ON v.MesInicio = m.MesInicio
                ORDER BY m.MesInicio
                OPTION (MAXRECURSION 12)
                """, cn, cmd => BindVentasFilters(cmd, filters), token);

            var topClientes = await QueryCategoryAsync($"""
                SELECT TOP (8)
                    ISNULL(NULLIF(LTRIM(RTRIM(c.NOMBRE)), ''), c.CUENTA) AS Categoria,
                    CONVERT(varchar(50), c.CUENTA) AS Codigo,
                    SUM(c.IMPORTE) AS Total
                FROM dbo.V_MV_Cpte c
                INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                {ventasWhere}
                GROUP BY c.CUENTA, c.NOMBRE
                ORDER BY SUM(c.IMPORTE) DESC
                """, cn, cmd => BindVentasFilters(cmd, filters), token);

            var topArticulos = await QueryCategoryAsync($"""
                SELECT TOP (8)
                    i.DESCRIPCION AS Categoria,
                    CONVERT(varchar(50), i.IDARTICULO) AS Codigo,
                    SUM(i.TOTAL) AS Total
                FROM dbo.V_MV_CpteInsumos i
                INNER JOIN dbo.V_MV_Cpte c
                    ON c.TC = i.TC
                   AND c.IDCOMPROBANTE = i.IDCOMPROBANTE
                   AND c.IDCOMPLEMENTO = i.IDCOMPLEMENTO
                INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                {ventasWhere}
                GROUP BY i.IDARTICULO, i.DESCRIPCION
                ORDER BY SUM(i.TOTAL) DESC
                """, cn, cmd => BindVentasFilters(cmd, filters), token);

            var ultimos = await QueryMovementsAsync($"""
                SELECT TOP (10)
                    c.FECHA,
                    c.TC,
                    ISNULL(NULLIF(LTRIM(RTRIM(c.NOMBRE)), ''), c.CUENTA) AS Nombre,
                    CONVERT(varchar(50), c.IDCOMPROBANTE),
                    c.IMPORTE
                FROM dbo.V_MV_Cpte c
                INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                {ventasWhere}
                ORDER BY c.FECHA DESC, c.ID DESC
                """, cn, cmd => BindVentasFilters(cmd, filters), token);

            return new VentasDashboardDto
            {
                TotalFacturadoMes = kpis.TotalFacturadoMes,
                TicketPromedioMes = kpis.TicketPromedioMes,
                ComprobantesMes = kpis.ComprobantesMes,
                ClientesActivos = kpis.ClientesActivos,
                EvolucionMensual = evolucion,
                TopClientes = topClientes,
                TopArticulos = topArticulos,
                UltimosComprobantes = ultimos
            };
        }, "No se pudo cargar el dashboard de ventas.", ct);
    }

    public async Task<VentasClientesPageDto> GetVentasClientesAsync(VentasDashboardFilters filters, CancellationToken ct = default)
    {
        filters ??= new VentasDashboardFilters();

        return await ExecuteLoggedAsync("Ventas", "GetClientesPage", async token =>
        {
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);

            const string ventasWhere = """
                WHERE UPPER(LTRIM(RTRIM(tc.SISTEMA))) = 'VENTAS'
                  AND (
                      c.TC LIKE 'FC%'
                      OR c.TC LIKE 'NC%'
                      OR c.TC LIKE 'ND%'
                      OR c.TC LIKE 'FP%'
                  )
                  AND (@FechaDesde IS NULL OR c.FECHA >= @FechaDesde)
                  AND (@FechaHastaExclusive IS NULL OR c.FECHA < @FechaHastaExclusive)
                  AND (@ClienteLike IS NULL OR c.CUENTA LIKE @ClienteLike OR c.NOMBRE LIKE @ClienteLike)
                  AND (@Usuario IS NULL OR c.Usuario = @Usuario)
                  AND (@Sucursal IS NULL OR CONVERT(varchar(50), c.UNEGOCIO) = @Sucursal)
                  AND (@Deposito IS NULL OR CONVERT(varchar(50), c.IdDeposito) = @Deposito)
                  AND (@TipoComprobante IS NULL OR c.TC = @TipoComprobante)
                """;

            var kpis = await QuerySingleAsync($"""
                SELECT
                    ISNULL(SUM(c.IMPORTE), 0),
                    COUNT(DISTINCT c.CUENTA),
                    ISNULL(AVG(c.IMPORTE), 0)
                FROM dbo.V_MV_Cpte c
                INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                {ventasWhere}
                """, cn, r => new VentasClientesPageDto
                {
                    TotalFacturado = GetDecimal(r, 0),
                    ClientesActivos = GetInt(r, 1),
                    TicketPromedio = GetDecimal(r, 2)
                }, cmd => BindVentasFilters(cmd, filters), token) ?? new VentasClientesPageDto();

            var top = await QueryCategoryAsync($"""
                SELECT TOP (10)
                    ISNULL(NULLIF(LTRIM(RTRIM(c.NOMBRE)), ''), CONVERT(varchar(50), c.CUENTA)) AS Categoria,
                    CONVERT(varchar(50), c.CUENTA) AS Codigo,
                    SUM(c.IMPORTE) AS Total
                FROM dbo.V_MV_Cpte c
                INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                {ventasWhere}
                GROUP BY c.CUENTA, c.NOMBRE
                ORDER BY SUM(c.IMPORTE) DESC
                """, cn, cmd => BindVentasFilters(cmd, filters), token);

            var clientes = await QueryVentasClientesAsync($"""
                SELECT
                    CONVERT(varchar(50), c.CUENTA) AS Cuenta,
                    ISNULL(NULLIF(LTRIM(RTRIM(c.NOMBRE)), ''), CONVERT(varchar(50), c.CUENTA)) AS Cliente,
                    SUM(c.IMPORTE) AS TotalFacturado,
                    COUNT(*) AS CantidadComprobantes,
                    ISNULL(AVG(c.IMPORTE), 0) AS TicketPromedio,
                    MAX(c.FECHA) AS UltimaVenta
                FROM dbo.V_MV_Cpte c
                INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                {ventasWhere}
                GROUP BY c.CUENTA, c.NOMBRE
                ORDER BY SUM(c.IMPORTE) DESC
                """, cn, cmd => BindVentasFilters(cmd, filters), token);

            var total = clientes.Sum(x => x.TotalFacturado);
            var normalized = clientes.Select(x => WithParticipacion(x, total)).ToList();

            return new VentasClientesPageDto
            {
                TotalFacturado = kpis.TotalFacturado,
                ClientesActivos = kpis.ClientesActivos,
                TicketPromedio = kpis.TicketPromedio,
                TopClientes = top,
                Clientes = normalized
            };
        }, "No se pudo cargar la página de clientes de ventas.", ct);
    }

    public async Task<VentasRubrosPageDto> GetVentasRubrosAsync(VentasDashboardFilters filters, CancellationToken ct = default)
    {
        filters ??= new VentasDashboardFilters();

        return await ExecuteLoggedAsync("Ventas", "GetRubrosPage", async token =>
        {
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);

            var kpis = await QuerySingleAsync("""
                SELECT
                    ISNULL(SUM(i.TOTAL), 0),
                    COUNT(DISTINCT a.IDRUBRO)
                FROM dbo.V_MV_CpteInsumos i
                INNER JOIN dbo.V_MV_Cpte c
                    ON c.TC = i.TC
                   AND c.IDCOMPROBANTE = i.IDCOMPROBANTE
                   AND c.IDCOMPLEMENTO = i.IDCOMPLEMENTO
                INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                INNER JOIN dbo.V_MA_ARTICULOS a ON a.IDARTICULO = i.IDARTICULO
                WHERE UPPER(LTRIM(RTRIM(tc.SISTEMA))) = 'VENTAS'
                  AND (
                      c.TC LIKE 'FC%'
                      OR c.TC LIKE 'NC%'
                      OR c.TC LIKE 'ND%'
                      OR c.TC LIKE 'FP%'
                  )
                  AND (@FechaDesde IS NULL OR c.FECHA >= @FechaDesde)
                  AND (@FechaHastaExclusive IS NULL OR c.FECHA < @FechaHastaExclusive)
                  AND (@ClienteLike IS NULL OR c.CUENTA LIKE @ClienteLike OR c.NOMBRE LIKE @ClienteLike)
                  AND (@Usuario IS NULL OR c.Usuario = @Usuario)
                  AND (@Sucursal IS NULL OR CONVERT(varchar(50), c.UNEGOCIO) = @Sucursal)
                  AND (@Deposito IS NULL OR CONVERT(varchar(50), i.IdDeposito) = @Deposito)
                  AND (@TipoComprobante IS NULL OR c.TC = @TipoComprobante)
                """, cn, r => new VentasRubrosPageDto
                {
                    TotalVendido = GetDecimal(r, 0),
                    RubrosActivos = GetInt(r, 1)
                }, cmd => BindVentasFilters(cmd, filters), token) ?? new VentasRubrosPageDto();

            var top = await QueryCategoryAsync("""
                SELECT TOP (10)
                    CONVERT(varchar(50), a.IDRUBRO) AS Categoria,
                    CONVERT(varchar(50), a.IDRUBRO) AS Codigo,
                    SUM(i.TOTAL) AS Total
                FROM dbo.V_MV_CpteInsumos i
                INNER JOIN dbo.V_MV_Cpte c
                    ON c.TC = i.TC
                   AND c.IDCOMPROBANTE = i.IDCOMPROBANTE
                   AND c.IDCOMPLEMENTO = i.IDCOMPLEMENTO
                INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                INNER JOIN dbo.V_MA_ARTICULOS a ON a.IDARTICULO = i.IDARTICULO
                WHERE UPPER(LTRIM(RTRIM(tc.SISTEMA))) = 'VENTAS'
                  AND (
                      c.TC LIKE 'FC%'
                      OR c.TC LIKE 'NC%'
                      OR c.TC LIKE 'ND%'
                      OR c.TC LIKE 'FP%'
                  )
                  AND (@FechaDesde IS NULL OR c.FECHA >= @FechaDesde)
                  AND (@FechaHastaExclusive IS NULL OR c.FECHA < @FechaHastaExclusive)
                  AND (@ClienteLike IS NULL OR c.CUENTA LIKE @ClienteLike OR c.NOMBRE LIKE @ClienteLike)
                  AND (@Usuario IS NULL OR c.Usuario = @Usuario)
                  AND (@Sucursal IS NULL OR CONVERT(varchar(50), c.UNEGOCIO) = @Sucursal)
                  AND (@Deposito IS NULL OR CONVERT(varchar(50), i.IdDeposito) = @Deposito)
                  AND (@TipoComprobante IS NULL OR c.TC = @TipoComprobante)
                GROUP BY a.IDRUBRO
                ORDER BY SUM(i.TOTAL) DESC
                """, cn, cmd => BindVentasFilters(cmd, filters), token);

            var rubros = await QueryVentasRubrosAsync("""
                SELECT
                    CONVERT(varchar(50), a.IDRUBRO) AS Rubro,
                    SUM(i.TOTAL) AS TotalVendido,
                    COUNT(DISTINCT i.IDARTICULO) AS CantidadArticulos,
                    COUNT(DISTINCT CONCAT(CONVERT(varchar(50), c.TC), '|', CONVERT(varchar(50), c.IDCOMPROBANTE), '|', CONVERT(varchar(50), c.IDCOMPLEMENTO))) AS CantidadComprobantes
                FROM dbo.V_MV_CpteInsumos i
                INNER JOIN dbo.V_MV_Cpte c
                    ON c.TC = i.TC
                   AND c.IDCOMPROBANTE = i.IDCOMPROBANTE
                   AND c.IDCOMPLEMENTO = i.IDCOMPLEMENTO
                INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                INNER JOIN dbo.V_MA_ARTICULOS a ON a.IDARTICULO = i.IDARTICULO
                WHERE UPPER(LTRIM(RTRIM(tc.SISTEMA))) = 'VENTAS'
                  AND (
                      c.TC LIKE 'FC%'
                      OR c.TC LIKE 'NC%'
                      OR c.TC LIKE 'ND%'
                      OR c.TC LIKE 'FP%'
                  )
                  AND (@FechaDesde IS NULL OR c.FECHA >= @FechaDesde)
                  AND (@FechaHastaExclusive IS NULL OR c.FECHA < @FechaHastaExclusive)
                  AND (@ClienteLike IS NULL OR c.CUENTA LIKE @ClienteLike OR c.NOMBRE LIKE @ClienteLike)
                  AND (@Usuario IS NULL OR c.Usuario = @Usuario)
                  AND (@Sucursal IS NULL OR CONVERT(varchar(50), c.UNEGOCIO) = @Sucursal)
                  AND (@Deposito IS NULL OR CONVERT(varchar(50), i.IdDeposito) = @Deposito)
                  AND (@TipoComprobante IS NULL OR c.TC = @TipoComprobante)
                GROUP BY a.IDRUBRO
                ORDER BY SUM(i.TOTAL) DESC
                """, cn, cmd => BindVentasFilters(cmd, filters), token);

            var total = rubros.Sum(x => x.TotalVendido);
            var normalized = rubros.Select(x => WithParticipacion(x, total)).ToList();

            return new VentasRubrosPageDto
            {
                TotalVendido = kpis.TotalVendido,
                RubrosActivos = kpis.RubrosActivos,
                TopRubros = top,
                Rubros = normalized
            };
        }, "No se pudo cargar la página de rubros de ventas.", ct);
    }

    public async Task<VentasFamiliasPageDto> GetVentasFamiliasAsync(VentasDashboardFilters filters, CancellationToken ct = default)
    {
        filters ??= new VentasDashboardFilters();

        return await ExecuteLoggedAsync("Ventas", "GetFamiliasPage", async token =>
        {
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);

            var kpis = await QuerySingleAsync("""
                SELECT
                    ISNULL(SUM(i.TOTAL), 0),
                    COUNT(DISTINCT a.IdFamilia)
                FROM dbo.V_MV_CpteInsumos i
                INNER JOIN dbo.V_MV_Cpte c
                    ON c.TC = i.TC
                   AND c.IDCOMPROBANTE = i.IDCOMPROBANTE
                   AND c.IDCOMPLEMENTO = i.IDCOMPLEMENTO
                INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                INNER JOIN dbo.V_MA_ARTICULOS a ON a.IDARTICULO = i.IDARTICULO
                WHERE UPPER(LTRIM(RTRIM(tc.SISTEMA))) = 'VENTAS'
                  AND (
                      c.TC LIKE 'FC%'
                      OR c.TC LIKE 'NC%'
                      OR c.TC LIKE 'ND%'
                      OR c.TC LIKE 'FP%'
                  )
                  AND (@FechaDesde IS NULL OR c.FECHA >= @FechaDesde)
                  AND (@FechaHastaExclusive IS NULL OR c.FECHA < @FechaHastaExclusive)
                  AND (@ClienteLike IS NULL OR c.CUENTA LIKE @ClienteLike OR c.NOMBRE LIKE @ClienteLike)
                  AND (@Usuario IS NULL OR c.Usuario = @Usuario)
                  AND (@Sucursal IS NULL OR CONVERT(varchar(50), c.UNEGOCIO) = @Sucursal)
                  AND (@Deposito IS NULL OR CONVERT(varchar(50), i.IdDeposito) = @Deposito)
                  AND (@TipoComprobante IS NULL OR c.TC = @TipoComprobante)
                """, cn, r => new VentasFamiliasPageDto
                {
                    TotalVendido = GetDecimal(r, 0),
                    FamiliasActivas = GetInt(r, 1)
                }, cmd => BindVentasFilters(cmd, filters), token) ?? new VentasFamiliasPageDto();

            var top = await QueryCategoryAsync("""
                SELECT TOP (10)
                    CONVERT(varchar(50), a.IdFamilia) AS Categoria,
                    CONVERT(varchar(50), a.IdFamilia) AS Codigo,
                    SUM(i.TOTAL) AS Total
                FROM dbo.V_MV_CpteInsumos i
                INNER JOIN dbo.V_MV_Cpte c
                    ON c.TC = i.TC
                   AND c.IDCOMPROBANTE = i.IDCOMPROBANTE
                   AND c.IDCOMPLEMENTO = i.IDCOMPLEMENTO
                INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                INNER JOIN dbo.V_MA_ARTICULOS a ON a.IDARTICULO = i.IDARTICULO
                WHERE UPPER(LTRIM(RTRIM(tc.SISTEMA))) = 'VENTAS'
                  AND (
                      c.TC LIKE 'FC%'
                      OR c.TC LIKE 'NC%'
                      OR c.TC LIKE 'ND%'
                      OR c.TC LIKE 'FP%'
                  )
                  AND (@FechaDesde IS NULL OR c.FECHA >= @FechaDesde)
                  AND (@FechaHastaExclusive IS NULL OR c.FECHA < @FechaHastaExclusive)
                  AND (@ClienteLike IS NULL OR c.CUENTA LIKE @ClienteLike OR c.NOMBRE LIKE @ClienteLike)
                  AND (@Usuario IS NULL OR c.Usuario = @Usuario)
                  AND (@Sucursal IS NULL OR CONVERT(varchar(50), c.UNEGOCIO) = @Sucursal)
                  AND (@Deposito IS NULL OR CONVERT(varchar(50), i.IdDeposito) = @Deposito)
                  AND (@TipoComprobante IS NULL OR c.TC = @TipoComprobante)
                GROUP BY a.IdFamilia
                ORDER BY SUM(i.TOTAL) DESC
                """, cn, cmd => BindVentasFilters(cmd, filters), token);

            var familias = await QueryVentasFamiliasAsync("""
                SELECT
                    CONVERT(varchar(50), a.IdFamilia) AS Familia,
                    SUM(i.TOTAL) AS TotalVendido,
                    COUNT(DISTINCT i.IDARTICULO) AS CantidadArticulos,
                    COUNT(DISTINCT CONCAT(CONVERT(varchar(50), c.TC), '|', CONVERT(varchar(50), c.IDCOMPROBANTE), '|', CONVERT(varchar(50), c.IDCOMPLEMENTO))) AS CantidadComprobantes
                FROM dbo.V_MV_CpteInsumos i
                INNER JOIN dbo.V_MV_Cpte c
                    ON c.TC = i.TC
                   AND c.IDCOMPROBANTE = i.IDCOMPROBANTE
                   AND c.IDCOMPLEMENTO = i.IDCOMPLEMENTO
                INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                INNER JOIN dbo.V_MA_ARTICULOS a ON a.IDARTICULO = i.IDARTICULO
                WHERE UPPER(LTRIM(RTRIM(tc.SISTEMA))) = 'VENTAS'
                  AND (
                      c.TC LIKE 'FC%'
                      OR c.TC LIKE 'NC%'
                      OR c.TC LIKE 'ND%'
                      OR c.TC LIKE 'FP%'
                  )
                  AND (@FechaDesde IS NULL OR c.FECHA >= @FechaDesde)
                  AND (@FechaHastaExclusive IS NULL OR c.FECHA < @FechaHastaExclusive)
                  AND (@ClienteLike IS NULL OR c.CUENTA LIKE @ClienteLike OR c.NOMBRE LIKE @ClienteLike)
                  AND (@Usuario IS NULL OR c.Usuario = @Usuario)
                  AND (@Sucursal IS NULL OR CONVERT(varchar(50), c.UNEGOCIO) = @Sucursal)
                  AND (@Deposito IS NULL OR CONVERT(varchar(50), i.IdDeposito) = @Deposito)
                  AND (@TipoComprobante IS NULL OR c.TC = @TipoComprobante)
                GROUP BY a.IdFamilia
                ORDER BY SUM(i.TOTAL) DESC
                """, cn, cmd => BindVentasFilters(cmd, filters), token);

            var total = familias.Sum(x => x.TotalVendido);
            var normalized = familias.Select(x => WithParticipacion(x, total)).ToList();

            return new VentasFamiliasPageDto
            {
                TotalVendido = kpis.TotalVendido,
                FamiliasActivas = kpis.FamiliasActivas,
                TopFamilias = top,
                Familias = normalized
            };
        }, "No se pudo cargar la página de familias de ventas.", ct);
    }

    public async Task<VentasArticulosPageDto> GetVentasArticulosAsync(VentasDashboardFilters filters, CancellationToken ct = default)
    {
        filters ??= new VentasDashboardFilters();

        return await ExecuteLoggedAsync("Ventas", "GetArticulosPage", async token =>
        {
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);

            var kpis = await QuerySingleAsync("""
                SELECT
                    ISNULL(SUM(i.TOTAL), 0),
                    COUNT(DISTINCT i.IDARTICULO),
                    ISNULL(SUM(i.CANTIDAD), 0)
                FROM dbo.V_MV_CpteInsumos i
                INNER JOIN dbo.V_MV_Cpte c
                    ON c.TC = i.TC
                   AND c.IDCOMPROBANTE = i.IDCOMPROBANTE
                   AND c.IDCOMPLEMENTO = i.IDCOMPLEMENTO
                INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                WHERE UPPER(LTRIM(RTRIM(tc.SISTEMA))) = 'VENTAS'
                  AND (
                      c.TC LIKE 'FC%'
                      OR c.TC LIKE 'NC%'
                      OR c.TC LIKE 'ND%'
                      OR c.TC LIKE 'FP%'
                  )
                  AND (@FechaDesde IS NULL OR c.FECHA >= @FechaDesde)
                  AND (@FechaHastaExclusive IS NULL OR c.FECHA < @FechaHastaExclusive)
                  AND (@ClienteLike IS NULL OR c.CUENTA LIKE @ClienteLike OR c.NOMBRE LIKE @ClienteLike)
                  AND (@Usuario IS NULL OR c.Usuario = @Usuario)
                  AND (@Sucursal IS NULL OR CONVERT(varchar(50), c.UNEGOCIO) = @Sucursal)
                  AND (@Deposito IS NULL OR CONVERT(varchar(50), i.IdDeposito) = @Deposito)
                  AND (@TipoComprobante IS NULL OR c.TC = @TipoComprobante)
                """, cn, r => new VentasArticulosPageDto
                {
                    TotalVendido = GetDecimal(r, 0),
                    ArticulosActivos = GetInt(r, 1),
                    CantidadVendida = GetDecimal(r, 2)
                }, cmd => BindVentasFilters(cmd, filters), token) ?? new VentasArticulosPageDto();

            var topTotal = await QueryCategoryAsync("""
                SELECT TOP (10)
                    i.DESCRIPCION AS Categoria,
                    CONVERT(varchar(50), i.IDARTICULO) AS Codigo,
                    SUM(i.TOTAL) AS Total
                FROM dbo.V_MV_CpteInsumos i
                INNER JOIN dbo.V_MV_Cpte c
                    ON c.TC = i.TC
                   AND c.IDCOMPROBANTE = i.IDCOMPROBANTE
                   AND c.IDCOMPLEMENTO = i.IDCOMPLEMENTO
                INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                WHERE UPPER(LTRIM(RTRIM(tc.SISTEMA))) = 'VENTAS'
                  AND (
                      c.TC LIKE 'FC%'
                      OR c.TC LIKE 'NC%'
                      OR c.TC LIKE 'ND%'
                      OR c.TC LIKE 'FP%'
                  )
                  AND (@FechaDesde IS NULL OR c.FECHA >= @FechaDesde)
                  AND (@FechaHastaExclusive IS NULL OR c.FECHA < @FechaHastaExclusive)
                  AND (@ClienteLike IS NULL OR c.CUENTA LIKE @ClienteLike OR c.NOMBRE LIKE @ClienteLike)
                  AND (@Usuario IS NULL OR c.Usuario = @Usuario)
                  AND (@Sucursal IS NULL OR CONVERT(varchar(50), c.UNEGOCIO) = @Sucursal)
                  AND (@Deposito IS NULL OR CONVERT(varchar(50), i.IdDeposito) = @Deposito)
                  AND (@TipoComprobante IS NULL OR c.TC = @TipoComprobante)
                GROUP BY i.IDARTICULO, i.DESCRIPCION
                ORDER BY SUM(i.TOTAL) DESC
                """, cn, cmd => BindVentasFilters(cmd, filters), token);

            var topCantidad = await QueryCategoryAsync("""
                SELECT TOP (10)
                    i.DESCRIPCION AS Categoria,
                    CONVERT(varchar(50), i.IDARTICULO) AS Codigo,
                    SUM(i.CANTIDAD) AS Total
                FROM dbo.V_MV_CpteInsumos i
                INNER JOIN dbo.V_MV_Cpte c
                    ON c.TC = i.TC
                   AND c.IDCOMPROBANTE = i.IDCOMPROBANTE
                   AND c.IDCOMPLEMENTO = i.IDCOMPLEMENTO
                INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                WHERE UPPER(LTRIM(RTRIM(tc.SISTEMA))) = 'VENTAS'
                  AND (
                      c.TC LIKE 'FC%'
                      OR c.TC LIKE 'NC%'
                      OR c.TC LIKE 'ND%'
                      OR c.TC LIKE 'FP%'
                  )
                  AND (@FechaDesde IS NULL OR c.FECHA >= @FechaDesde)
                  AND (@FechaHastaExclusive IS NULL OR c.FECHA < @FechaHastaExclusive)
                  AND (@ClienteLike IS NULL OR c.CUENTA LIKE @ClienteLike OR c.NOMBRE LIKE @ClienteLike)
                  AND (@Usuario IS NULL OR c.Usuario = @Usuario)
                  AND (@Sucursal IS NULL OR CONVERT(varchar(50), c.UNEGOCIO) = @Sucursal)
                  AND (@Deposito IS NULL OR CONVERT(varchar(50), i.IdDeposito) = @Deposito)
                  AND (@TipoComprobante IS NULL OR c.TC = @TipoComprobante)
                GROUP BY i.IDARTICULO, i.DESCRIPCION
                ORDER BY SUM(i.CANTIDAD) DESC
                """, cn, cmd => BindVentasFilters(cmd, filters), token);

            var articulos = await QueryVentasArticulosAsync("""
                SELECT
                    CONVERT(varchar(50), i.IDARTICULO) AS IdArticulo,
                    i.DESCRIPCION,
                    SUM(i.CANTIDAD) AS CantidadVendida,
                    SUM(i.TOTAL) AS TotalVendido,
                    COUNT(DISTINCT CONCAT(CONVERT(varchar(50), c.TC), '|', CONVERT(varchar(50), c.IDCOMPROBANTE), '|', CONVERT(varchar(50), c.IDCOMPLEMENTO))) AS CantidadComprobantes,
                    MAX(c.FECHA) AS UltimaVenta
                FROM dbo.V_MV_CpteInsumos i
                INNER JOIN dbo.V_MV_Cpte c
                    ON c.TC = i.TC
                   AND c.IDCOMPROBANTE = i.IDCOMPROBANTE
                   AND c.IDCOMPLEMENTO = i.IDCOMPLEMENTO
                INNER JOIN dbo.V_TA_Cpte tc ON tc.CODIGO = c.TC
                WHERE UPPER(LTRIM(RTRIM(tc.SISTEMA))) = 'VENTAS'
                  AND (
                      c.TC LIKE 'FC%'
                      OR c.TC LIKE 'NC%'
                      OR c.TC LIKE 'ND%'
                      OR c.TC LIKE 'FP%'
                  )
                  AND (@FechaDesde IS NULL OR c.FECHA >= @FechaDesde)
                  AND (@FechaHastaExclusive IS NULL OR c.FECHA < @FechaHastaExclusive)
                  AND (@ClienteLike IS NULL OR c.CUENTA LIKE @ClienteLike OR c.NOMBRE LIKE @ClienteLike)
                  AND (@Usuario IS NULL OR c.Usuario = @Usuario)
                  AND (@Sucursal IS NULL OR CONVERT(varchar(50), c.UNEGOCIO) = @Sucursal)
                  AND (@Deposito IS NULL OR CONVERT(varchar(50), i.IdDeposito) = @Deposito)
                  AND (@TipoComprobante IS NULL OR c.TC = @TipoComprobante)
                GROUP BY i.IDARTICULO, i.DESCRIPCION
                ORDER BY SUM(i.TOTAL) DESC
                """, cn, cmd => BindVentasFilters(cmd, filters), token);

            return new VentasArticulosPageDto
            {
                TotalVendido = kpis.TotalVendido,
                ArticulosActivos = kpis.ArticulosActivos,
                CantidadVendida = kpis.CantidadVendida,
                TopPorTotal = topTotal,
                TopPorCantidad = topCantidad,
                Articulos = articulos
            };
        }, "No se pudo cargar la página de artículos de ventas.", ct);
    }

    public async Task<StockDashboardDto> GetStockAsync(StockDashboardFilters filters, CancellationToken ct = default)
    {
        filters ??= new StockDashboardFilters();

        return await ExecuteLoggedAsync("Stock", "GetDashboard", async token =>
        {
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);

            const string stockCurrentWhere = """
                WHERE (@FechaHastaExclusive IS NULL OR s.FECHA < @FechaHastaExclusive)
                  AND (@ArticuloCodigoLike IS NULL OR CONVERT(varchar(50), s.IDArticulo) LIKE @ArticuloCodigoLike)
                  AND (@Deposito IS NULL OR CONVERT(varchar(50), s.IdDeposito) = @Deposito)
                  AND (@Sucursal IS NULL OR CONVERT(varchar(50), s.UNEGOCIO) = @Sucursal)
                  AND (@Estado IS NULL OR s.Estado = @Estado)
                """;

            const string stockArticleWhere = """
                WHERE ISNULL(a.SUSPENDIDO, 0) = 0
                  AND (@ArticuloCodigoLike IS NULL OR CONVERT(varchar(50), a.IDARTICULO) LIKE @ArticuloCodigoLike)
                  AND (@ArticuloDescripcionLike IS NULL OR a.DESCRIPCION LIKE @ArticuloDescripcionLike)
                  AND (@Rubro IS NULL OR CONVERT(varchar(50), a.IDRUBRO) = @Rubro)
                  AND (@Familia IS NULL OR CONVERT(varchar(50), a.IdFamilia) = @Familia)
                """;

            var kpis = await QuerySingleAsync($"""
                WITH StockActual AS (
                    SELECT s.IDArticulo, SUM(s.Cantidad) AS StockActual
                    FROM dbo.V_MV_STOCK s
                    {stockCurrentWhere}
                    GROUP BY s.IDArticulo
                )
                SELECT
                    ISNULL(SUM(CASE WHEN ISNULL(st.StockActual, 0) > 0 THEN ISNULL(st.StockActual, 0) * ISNULL(a.COSTO, 0) ELSE 0 END), 0) AS StockValorizado,
                    SUM(CASE WHEN ISNULL(st.StockActual, 0) > 0 THEN 1 ELSE 0 END) AS ArticulosConStock,
                    SUM(CASE WHEN ISNULL(st.StockActual, 0) <= ISNULL(a.PUNTOPEDIDO, 0) AND ISNULL(a.PUNTOPEDIDO, 0) > 0 THEN 1 ELSE 0 END) AS BajoPuntoPedido,
                    SUM(CASE WHEN ISNULL(st.StockActual, 0) <= 0 THEN 1 ELSE 0 END) AS SinStock
                FROM dbo.V_MA_ARTICULOS a
                LEFT JOIN StockActual st ON st.IDArticulo = a.IDARTICULO
                {stockArticleWhere}
                """, cn, r => new StockDashboardDto
                {
                    StockValorizado = GetDecimal(r, 0),
                    ArticulosConStock = GetInt(r, 1),
                    BajoPuntoPedido = GetInt(r, 2),
                    SinStock = GetInt(r, 3)
                }, cmd => BindStockFilters(cmd, filters), token) ?? new StockDashboardDto();

            var evolucion = await QueryMonthlyAsync("""
                WITH Meses AS (
                    SELECT DATEADD(month, -11, DATEFROMPARTS(YEAR(DATEADD(day, -1, ISNULL(@FechaHastaExclusive, DATEADD(day, 1, GETDATE())))), MONTH(DATEADD(day, -1, ISNULL(@FechaHastaExclusive, DATEADD(day, 1, GETDATE())))), 1)) AS MesInicio
                    UNION ALL
                    SELECT DATEADD(month, 1, MesInicio)
                    FROM Meses
                    WHERE MesInicio < DATEFROMPARTS(YEAR(DATEADD(day, -1, ISNULL(@FechaHastaExclusive, DATEADD(day, 1, GETDATE())))), MONTH(DATEADD(day, -1, ISNULL(@FechaHastaExclusive, DATEADD(day, 1, GETDATE())))), 1)
                ),
                ArticulosFiltrados AS (
                    SELECT
                        a.IDARTICULO,
                        a.COSTO
                    FROM dbo.V_MA_ARTICULOS a
                    WHERE ISNULL(a.SUSPENDIDO, 0) = 0
                      AND (@ArticuloCodigoLike IS NULL OR CONVERT(varchar(50), a.IDARTICULO) LIKE @ArticuloCodigoLike)
                      AND (@ArticuloDescripcionLike IS NULL OR a.DESCRIPCION LIKE @ArticuloDescripcionLike)
                      AND (@Rubro IS NULL OR CONVERT(varchar(50), a.IDRUBRO) = @Rubro)
                      AND (@Familia IS NULL OR CONVERT(varchar(50), a.IdFamilia) = @Familia)
                ),
                StockBase AS (
                    SELECT
                        s.IDArticulo,
                        SUM(s.Cantidad) AS CantidadBase
                    FROM dbo.V_MV_STOCK s
                    INNER JOIN ArticulosFiltrados a ON a.IDARTICULO = s.IDArticulo
                    WHERE s.FECHA < DATEADD(month, -11, DATEFROMPARTS(YEAR(DATEADD(day, -1, ISNULL(@FechaHastaExclusive, DATEADD(day, 1, GETDATE())))), MONTH(DATEADD(day, -1, ISNULL(@FechaHastaExclusive, DATEADD(day, 1, GETDATE())))), 1))
                      AND (@Deposito IS NULL OR CONVERT(varchar(50), s.IdDeposito) = @Deposito)
                      AND (@Sucursal IS NULL OR CONVERT(varchar(50), s.UNEGOCIO) = @Sucursal)
                      AND (@Estado IS NULL OR s.Estado = @Estado)
                    GROUP BY s.IDArticulo
                ),
                MovMes AS (
                    SELECT
                        DATEFROMPARTS(YEAR(s.FECHA), MONTH(s.FECHA), 1) AS MesInicio,
                        s.IDArticulo,
                        SUM(s.Cantidad) AS CantidadMes
                    FROM dbo.V_MV_STOCK s
                    INNER JOIN ArticulosFiltrados a ON a.IDARTICULO = s.IDArticulo
                    WHERE s.FECHA >= DATEADD(month, -11, DATEFROMPARTS(YEAR(DATEADD(day, -1, ISNULL(@FechaHastaExclusive, DATEADD(day, 1, GETDATE())))), MONTH(DATEADD(day, -1, ISNULL(@FechaHastaExclusive, DATEADD(day, 1, GETDATE())))), 1))
                      AND s.FECHA < ISNULL(@FechaHastaExclusive, DATEADD(day, 1, GETDATE()))
                      AND (@Deposito IS NULL OR CONVERT(varchar(50), s.IdDeposito) = @Deposito)
                      AND (@Sucursal IS NULL OR CONVERT(varchar(50), s.UNEGOCIO) = @Sucursal)
                      AND (@Estado IS NULL OR s.Estado = @Estado)
                    GROUP BY DATEFROMPARTS(YEAR(s.FECHA), MONTH(s.FECHA), 1), s.IDArticulo
                ),
                MesArticulo AS (
                    SELECT
                        m.MesInicio,
                        a.IDARTICULO,
                        CASE
                            WHEN ISNULL(sb.CantidadBase, 0)
                                + SUM(ISNULL(mm.CantidadMes, 0)) OVER (
                                    PARTITION BY a.IDARTICULO
                                    ORDER BY m.MesInicio
                                    ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                                ) < 0
                            THEN 0
                            ELSE ISNULL(sb.CantidadBase, 0)
                            + SUM(ISNULL(mm.CantidadMes, 0)) OVER (
                                PARTITION BY a.IDARTICULO
                                ORDER BY m.MesInicio
                                ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                            ) AS StockMes
                        END AS StockMes
                    FROM Meses m
                    CROSS JOIN ArticulosFiltrados a
                    LEFT JOIN StockBase sb ON sb.IDArticulo = a.IDARTICULO
                    LEFT JOIN MovMes mm
                        ON mm.MesInicio = m.MesInicio
                       AND mm.IDArticulo = a.IDARTICULO
                )
                SELECT
                    CONVERT(varchar(7), ma.MesInicio, 120) AS Periodo,
                    SUM(CASE WHEN ma.StockMes > 0 THEN ma.StockMes * ISNULL(a.COSTO, 0) ELSE 0 END) AS Total
                FROM MesArticulo ma
                INNER JOIN ArticulosFiltrados a ON a.IDARTICULO = ma.IDARTICULO
                GROUP BY ma.MesInicio
                ORDER BY ma.MesInicio
                OPTION (MAXRECURSION 12)
                """, cn, cmd => BindStockFilters(cmd, filters), token);

            var topMov = await QueryCategoryAsync("""
                SELECT TOP (8)
                    ISNULL(NULLIF(LTRIM(RTRIM(a.DESCRIPCION)), ''), CONVERT(varchar(50), s.IDArticulo)) AS Categoria,
                    CONVERT(varchar(50), s.IDArticulo) AS Codigo,
                    SUM(ABS(CAST(s.Cantidad AS decimal(18, 2))) * ISNULL(s.COSTO, 0)) AS Total
                FROM dbo.V_MV_STOCK s
                INNER JOIN dbo.V_MA_ARTICULOS a ON a.IDARTICULO = s.IDArticulo
                WHERE (@FechaDesde IS NULL OR s.FECHA >= @FechaDesde)
                  AND (@FechaHastaExclusive IS NULL OR s.FECHA < @FechaHastaExclusive)
                  AND (@ArticuloCodigoLike IS NULL OR CONVERT(varchar(50), s.IDArticulo) LIKE @ArticuloCodigoLike)
                  AND (@ArticuloDescripcionLike IS NULL OR a.DESCRIPCION LIKE @ArticuloDescripcionLike OR s.Descripcion LIKE @ArticuloDescripcionLike)
                  AND (@Rubro IS NULL OR CONVERT(varchar(50), a.IDRUBRO) = @Rubro)
                  AND (@Familia IS NULL OR CONVERT(varchar(50), a.IdFamilia) = @Familia)
                  AND (@Deposito IS NULL OR CONVERT(varchar(50), s.IdDeposito) = @Deposito)
                  AND (@Sucursal IS NULL OR CONVERT(varchar(50), s.UNEGOCIO) = @Sucursal)
                  AND (@Estado IS NULL OR s.Estado = @Estado)
                GROUP BY s.IDArticulo, a.DESCRIPCION
                ORDER BY SUM(ABS(CAST(s.Cantidad AS decimal(18, 2))) * ISNULL(s.COSTO, 0)) DESC
                """, cn, cmd => BindStockFilters(cmd, filters), token);

            var criticos = await QueryStockCriticosAsync(cn, cmd => BindStockFilters(cmd, filters), token);

            return new StockDashboardDto
            {
                StockValorizado = kpis.StockValorizado,
                ArticulosConStock = kpis.ArticulosConStock,
                BajoPuntoPedido = kpis.BajoPuntoPedido,
                SinStock = kpis.SinStock,
                EvolucionMensual = evolucion,
                TopMovimientos = topMov,
                Criticos = criticos
            };
        }, "No se pudo cargar el dashboard de stock.", ct);
    }

    public async Task<CajaBancosDashboardDto> GetCajaBancosAsync(CajaBancosDashboardFilters filters, CancellationToken ct = default)
    {
        filters ??= new CajaBancosDashboardFilters();

        return await ExecuteLoggedAsync("CajaBancos", "GetDashboard", async token =>
        {
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);

            var kpis = await QuerySingleAsync("""
                WITH CajasActuales AS (
                    SELECT
                        ISNULL(NULLIF(LTRIM(RTRIM(c.Descripcion)), ''), CONVERT(varchar(50), c.IdCajas)) AS CajaNombre,
                        c.SALDO,
                        ROW_NUMBER() OVER (
                            PARTITION BY ISNULL(NULLIF(LTRIM(RTRIM(c.Descripcion)), ''), CONVERT(varchar(50), c.IdCajas))
                            ORDER BY c.FECHA DESC
                        ) AS rn
                    FROM dbo.VT_CONSOLIDADO_CAJA c
                    WHERE (@FechaHastaExclusive IS NULL OR c.FECHA < @FechaHastaExclusive)
                      AND (@Caja IS NULL OR ISNULL(NULLIF(LTRIM(RTRIM(c.Descripcion)), ''), CONVERT(varchar(50), c.IdCajas)) = @Caja)
                      AND (@TextoLike IS NULL OR c.Descripcion LIKE @TextoLike)
                )
                SELECT
                    ISNULL((SELECT SUM(SALDO) FROM CajasActuales WHERE rn = 1), 0) AS SaldoCajas,
                    ISNULL((
                        SELECT SUM(CASE WHEN b.[DEBE-HABER] = 'D' THEN b.IMPORTE ELSE -b.IMPORTE END)
                        FROM dbo.V_EstadoBancario b
                        WHERE UPPER(LTRIM(RTRIM(b.TipoVista))) = 'BC'
                          AND (@FechaHastaExclusive IS NULL OR b.FECHA < @FechaHastaExclusive)
                          AND (@BancoCuenta IS NULL OR b.CUENTA = @BancoCuenta)
                          AND (@TextoLike IS NULL OR b.DETALLE LIKE @TextoLike)
                    ), 0) AS SaldoBancos,
                    ISNULL((
                        SELECT SUM(c.INGRESOS)
                        FROM dbo.VT_CONSOLIDADO_CAJA c
                        WHERE (@FechaDesde IS NULL OR c.FECHA >= @FechaDesde)
                          AND (@FechaHastaExclusive IS NULL OR c.FECHA < @FechaHastaExclusive)
                          AND (@Caja IS NULL OR ISNULL(NULLIF(LTRIM(RTRIM(c.Descripcion)), ''), CONVERT(varchar(50), c.IdCajas)) = @Caja)
                          AND (@TextoLike IS NULL OR c.Descripcion LIKE @TextoLike)
                    ),0) AS IngresosPeriodo,
                    ISNULL((
                        SELECT SUM(c.EGRESOS)
                        FROM dbo.VT_CONSOLIDADO_CAJA c
                        WHERE (@FechaDesde IS NULL OR c.FECHA >= @FechaDesde)
                          AND (@FechaHastaExclusive IS NULL OR c.FECHA < @FechaHastaExclusive)
                          AND (@Caja IS NULL OR ISNULL(NULLIF(LTRIM(RTRIM(c.Descripcion)), ''), CONVERT(varchar(50), c.IdCajas)) = @Caja)
                          AND (@TextoLike IS NULL OR c.Descripcion LIKE @TextoLike)
                    ),0) AS EgresosPeriodo,
                    ISNULL((SELECT SUM(SALDO) FROM dbo.VE_CPTES_IMPAGOS),0) AS PendienteCobro,
                    ISNULL((SELECT SUM(SALDO) FROM dbo.CO_CPTES_IMPAGOS),0) AS PendientePago
                """, cn, r => new CajaBancosDashboardDto
                {
                    SaldoCajas = GetDecimal(r, 0),
                    SaldoBancos = GetDecimal(r, 1),
                    Ingresos7Dias = GetDecimal(r, 2),
                    Egresos7Dias = GetDecimal(r, 3),
                    PendienteCobro = GetDecimal(r, 4),
                    PendientePago = GetDecimal(r, 5)
                }, cmd => BindCajaBancosFilters(cmd, filters), token) ?? new CajaBancosDashboardDto();

            var evolucion = await QueryMonthlyAsync("""
                SELECT
                    CONVERT(varchar(10), c.FECHA, 103) AS Periodo,
                    SUM(c.INGRESOS - c.EGRESOS) AS Total
                FROM dbo.VT_CONSOLIDADO_CAJA c
                WHERE (@FechaDesde IS NULL OR c.FECHA >= @FechaDesde)
                  AND (@FechaHastaExclusive IS NULL OR c.FECHA < @FechaHastaExclusive)
                  AND (@Caja IS NULL OR ISNULL(NULLIF(LTRIM(RTRIM(c.Descripcion)), ''), CONVERT(varchar(50), c.IdCajas)) = @Caja)
                  AND (@TextoLike IS NULL OR c.Descripcion LIKE @TextoLike)
                GROUP BY CONVERT(varchar(10), c.FECHA, 103), CAST(c.FECHA AS date)
                ORDER BY CAST(c.FECHA AS date)
                """, cn, cmd => BindCajaBancosFilters(cmd, filters), token);

            var topCajas = await QueryCategoryAsync("""
                SELECT TOP (8)
                    ISNULL(NULLIF(LTRIM(RTRIM(c.Descripcion)), ''), CONVERT(varchar(50), c.IdCajas)) AS Categoria,
                    CONVERT(varchar(50), c.IdCajas) AS Codigo,
                    MAX(c.SALDO) AS Total
                FROM dbo.VT_CONSOLIDADO_CAJA c
                WHERE (@FechaDesde IS NULL OR c.FECHA >= @FechaDesde)
                  AND (@FechaHastaExclusive IS NULL OR c.FECHA < @FechaHastaExclusive)
                  AND (@Caja IS NULL OR ISNULL(NULLIF(LTRIM(RTRIM(c.Descripcion)), ''), CONVERT(varchar(50), c.IdCajas)) = @Caja)
                  AND (@TextoLike IS NULL OR c.Descripcion LIKE @TextoLike)
                GROUP BY c.IdCajas, c.Descripcion
                ORDER BY MAX(c.SALDO) DESC
                """, cn, cmd => BindCajaBancosFilters(cmd, filters), token);

            var topBancos = await QueryCategoryAsync("""
                SELECT TOP (8)
                    b.CUENTA AS Categoria,
                    b.CUENTA AS Codigo,
                    SUM(CASE WHEN b.[DEBE-HABER] = 'D' THEN b.IMPORTE ELSE -b.IMPORTE END) AS Total
                FROM dbo.V_EstadoBancario b
                WHERE UPPER(LTRIM(RTRIM(b.TipoVista))) = 'BC'
                  AND (@FechaDesde IS NULL OR b.FECHA >= @FechaDesde)
                  AND (@FechaHastaExclusive IS NULL OR b.FECHA < @FechaHastaExclusive)
                  AND (@BancoCuenta IS NULL OR b.CUENTA = @BancoCuenta)
                  AND (@TextoLike IS NULL OR b.DETALLE LIKE @TextoLike)
                GROUP BY b.CUENTA
                ORDER BY SUM(CASE WHEN b.[DEBE-HABER] = 'D' THEN b.IMPORTE ELSE -b.IMPORTE END) DESC
                """, cn, cmd => BindCajaBancosFilters(cmd, filters), token);

            return new CajaBancosDashboardDto
            {
                SaldoCajas = kpis.SaldoCajas,
                SaldoBancos = kpis.SaldoBancos,
                Ingresos7Dias = kpis.Ingresos7Dias,
                Egresos7Dias = kpis.Egresos7Dias,
                PendienteCobro = kpis.PendienteCobro,
                PendientePago = kpis.PendientePago,
                EvolucionDiaria = evolucion,
                TopCajas = topCajas,
                TopBancos = topBancos
            };
        }, "No se pudo cargar el dashboard de caja y bancos.", ct);
    }

    public async Task<ContabilidadDashboardDto> GetContabilidadAsync(ContabilidadDashboardFilters filters, CancellationToken ct = default)
    {
        filters ??= new ContabilidadDashboardFilters();

        return await ExecuteLoggedAsync("Contabilidad", "GetDashboard", async token =>
        {
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);

            const string contabilidadWhere = """
                WHERE (@FechaDesde IS NULL OR a.FECHA >= @FechaDesde)
                  AND (@FechaHastaExclusive IS NULL OR a.FECHA < @FechaHastaExclusive)
                  AND (@CuentaLike IS NULL OR a.CUENTA LIKE @CuentaLike)
                  AND (@DetalleLike IS NULL OR a.DETALLE LIKE @DetalleLike)
                  AND (@Usuario IS NULL OR a.USUARIO_LOGEADO = @Usuario)
                  AND (@Sucursal IS NULL OR CONVERT(varchar(50), a.UNEGOCIO) = @Sucursal)
                  AND (@Tipo IS NULL OR a.[DEBE-HABER] = @Tipo)
                """;

            var kpis = await QuerySingleAsync($"""
                SELECT
                    ISNULL(SUM(CASE WHEN a.[DEBE-HABER] = 'D' THEN a.IMPORTE ELSE 0 END),0) AS DebePeriodo,
                    ISNULL(SUM(CASE WHEN a.[DEBE-HABER] = 'H' THEN a.IMPORTE ELSE 0 END),0) AS HaberPeriodo,
                    ISNULL(SUM(CASE WHEN a.[DEBE-HABER] = 'D' THEN a.IMPORTE ELSE -a.IMPORTE END),0) AS SaldoNetoPeriodo,
                    COUNT(DISTINCT a.[NUMERO ASIENTO]) AS AsientosPeriodo
                FROM dbo.MV_ASIENTOS a
                {contabilidadWhere}
                """, cn, r => new ContabilidadDashboardDto
                {
                    DebeMes = GetDecimal(r, 0),
                    HaberMes = GetDecimal(r, 1),
                    SaldoNetoMes = GetDecimal(r, 2),
                    AsientosMes = GetInt(r, 3)
                }, cmd => BindContabilidadFilters(cmd, filters), token) ?? new ContabilidadDashboardDto();

            var evolucion = await QueryMonthlyAsync($"""
                SELECT
                    CONVERT(varchar(7), a.FECHA, 120) AS Periodo,
                    SUM(CASE WHEN a.[DEBE-HABER] = 'D' THEN a.IMPORTE ELSE -a.IMPORTE END) AS Total
                FROM dbo.MV_ASIENTOS a
                {contabilidadWhere}
                GROUP BY CONVERT(varchar(7), a.FECHA, 120)
                ORDER BY Periodo
                """, cn, cmd => BindContabilidadFilters(cmd, filters), token);

            var topCuentas = await QueryCategoryAsync($"""
                SELECT TOP (8)
                    a.CUENTA AS Categoria,
                    a.CUENTA AS Codigo,
                    SUM(ABS(a.IMPORTE)) AS Total
                FROM dbo.MV_ASIENTOS a
                {contabilidadWhere}
                GROUP BY a.CUENTA
                ORDER BY SUM(ABS(a.IMPORTE)) DESC
                """, cn, cmd => BindContabilidadFilters(cmd, filters), token);

            var ultimos = await QueryAsientosAsync($"""
                SELECT TOP (12)
                    a.FECHA,
                    a.CUENTA,
                    a.DETALLE,
                    a.[DEBE-HABER],
                    a.IMPORTE
                FROM dbo.MV_ASIENTOS a
                {contabilidadWhere}
                ORDER BY a.FECHA DESC, a.[NUMERO ASIENTO] DESC, a.SECUENCIA DESC
                """, cn, cmd => BindContabilidadFilters(cmd, filters), token);

            return new ContabilidadDashboardDto
            {
                DebeMes = kpis.DebeMes,
                HaberMes = kpis.HaberMes,
                SaldoNetoMes = kpis.SaldoNetoMes,
                AsientosMes = kpis.AsientosMes,
                EvolucionMensual = evolucion,
                TopCuentas = topCuentas,
                UltimosAsientos = ultimos
            };
        }, "No se pudo cargar el dashboard de contabilidad.", ct);
    }

    private async Task<IReadOnlyList<string>> QueryDistinctAsync(string sql, SqlConnection cn, CancellationToken ct)
    {
        var items = new List<string>();
        await using var cmd = new SqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            var value = GetStringValue(rd, 0);
            if (!string.IsNullOrWhiteSpace(value))
            {
                items.Add(value);
            }
        }

        return items.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<IReadOnlyList<MonthlyPointDto>> QueryMonthlyAsync(string sql, SqlConnection cn, Action<SqlCommand>? bind, CancellationToken ct)
    {
        var items = new List<MonthlyPointDto>();
        await using var cmd = new SqlCommand(sql, cn);
        bind?.Invoke(cmd);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            items.Add(new MonthlyPointDto
            {
                Periodo = GetStringValue(rd, 0),
                Total = GetDecimal(rd, 1)
            });
        }

        return items;
    }

    private async Task<IReadOnlyList<CategoryTotalDto>> QueryCategoryAsync(string sql, SqlConnection cn, Action<SqlCommand>? bind, CancellationToken ct)
    {
        var items = new List<CategoryTotalDto>();
        await using var cmd = new SqlCommand(sql, cn);
        bind?.Invoke(cmd);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            items.Add(new CategoryTotalDto
            {
                Categoria = GetStringValue(rd, 0),
                Codigo = rd.IsDBNull(1) ? null : GetStringValue(rd, 1),
                Total = GetDecimal(rd, 2)
            });
        }

        var total = items.Sum(i => i.Total);
        return items.Select(i => new CategoryTotalDto
        {
            Categoria = i.Categoria,
            Codigo = i.Codigo,
            Total = i.Total,
            Participacion = total == 0 ? 0 : i.Total / total
        }).ToList();
    }

    private async Task<IReadOnlyList<GestionMovimientoDto>> QueryMovementsAsync(string sql, SqlConnection cn, Action<SqlCommand>? bind, CancellationToken ct)
    {
        var items = new List<GestionMovimientoDto>();
        await using var cmd = new SqlCommand(sql, cn);
        bind?.Invoke(cmd);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            items.Add(new GestionMovimientoDto
            {
                Fecha = rd.GetDateTime(0),
                Codigo = GetStringValue(rd, 1),
                Descripcion = GetStringValue(rd, 2),
                Referencia = GetStringValue(rd, 3),
                Total = GetDecimal(rd, 4)
            });
        }

        return items;
    }

    private async Task<IReadOnlyList<VentasClienteResumenDto>> QueryVentasClientesAsync(string sql, SqlConnection cn, Action<SqlCommand>? bind, CancellationToken ct)
    {
        var items = new List<VentasClienteResumenDto>();
        await using var cmd = new SqlCommand(sql, cn);
        bind?.Invoke(cmd);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            items.Add(new VentasClienteResumenDto
            {
                Cuenta = GetStringValue(rd, 0),
                Cliente = GetStringValue(rd, 1),
                TotalFacturado = GetDecimal(rd, 2),
                CantidadComprobantes = GetInt(rd, 3),
                TicketPromedio = GetDecimal(rd, 4),
                UltimaVenta = rd.IsDBNull(5) ? null : rd.GetDateTime(5)
            });
        }

        return items;
    }

    private async Task<IReadOnlyList<VentasRubroResumenDto>> QueryVentasRubrosAsync(string sql, SqlConnection cn, Action<SqlCommand>? bind, CancellationToken ct)
    {
        var items = new List<VentasRubroResumenDto>();
        await using var cmd = new SqlCommand(sql, cn);
        bind?.Invoke(cmd);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            items.Add(new VentasRubroResumenDto
            {
                Rubro = GetStringValue(rd, 0),
                TotalVendido = GetDecimal(rd, 1),
                CantidadArticulos = GetInt(rd, 2),
                CantidadComprobantes = GetInt(rd, 3)
            });
        }

        return items;
    }

    private async Task<IReadOnlyList<VentasFamiliaResumenDto>> QueryVentasFamiliasAsync(string sql, SqlConnection cn, Action<SqlCommand>? bind, CancellationToken ct)
    {
        var items = new List<VentasFamiliaResumenDto>();
        await using var cmd = new SqlCommand(sql, cn);
        bind?.Invoke(cmd);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            items.Add(new VentasFamiliaResumenDto
            {
                Familia = GetStringValue(rd, 0),
                TotalVendido = GetDecimal(rd, 1),
                CantidadArticulos = GetInt(rd, 2),
                CantidadComprobantes = GetInt(rd, 3)
            });
        }

        return items;
    }

    private async Task<IReadOnlyList<VentasArticuloResumenDto>> QueryVentasArticulosAsync(string sql, SqlConnection cn, Action<SqlCommand>? bind, CancellationToken ct)
    {
        var items = new List<VentasArticuloResumenDto>();
        await using var cmd = new SqlCommand(sql, cn);
        bind?.Invoke(cmd);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            items.Add(new VentasArticuloResumenDto
            {
                IdArticulo = GetStringValue(rd, 0),
                Descripcion = GetStringValue(rd, 1),
                CantidadVendida = GetDecimal(rd, 2),
                TotalVendido = GetDecimal(rd, 3),
                CantidadComprobantes = GetInt(rd, 4),
                UltimaVenta = rd.IsDBNull(5) ? null : rd.GetDateTime(5)
            });
        }

        return items;
    }

    private async Task<IReadOnlyList<StockCriticoDto>> QueryStockCriticosAsync(SqlConnection cn, Action<SqlCommand>? bind, CancellationToken ct)
    {
        var items = new List<StockCriticoDto>();
        await using var cmd = new SqlCommand("""
            WITH StockActual AS (
                SELECT s.IDArticulo, SUM(s.Cantidad) AS StockActual
                FROM dbo.V_MV_STOCK s
                WHERE (@FechaHastaExclusive IS NULL OR s.FECHA < @FechaHastaExclusive)
                  AND (@ArticuloCodigoLike IS NULL OR CONVERT(varchar(50), s.IDArticulo) LIKE @ArticuloCodigoLike)
                  AND (@Deposito IS NULL OR CONVERT(varchar(50), s.IdDeposito) = @Deposito)
                  AND (@Sucursal IS NULL OR CONVERT(varchar(50), s.UNEGOCIO) = @Sucursal)
                  AND (@Estado IS NULL OR s.Estado = @Estado)
                GROUP BY s.IDArticulo
            )
            SELECT TOP (12)
                CONVERT(varchar(50), a.IDARTICULO),
                a.DESCRIPCION,
                ISNULL(st.StockActual, 0) AS StockActual,
                ISNULL(a.PUNTOPEDIDO, 0) AS PuntoPedido,
                ISNULL(st.StockActual, 0) * ISNULL(a.COSTO, 0) AS Valorizado
            FROM dbo.V_MA_ARTICULOS a
            LEFT JOIN StockActual st ON st.IDArticulo = a.IDARTICULO
            WHERE ISNULL(a.PUNTOPEDIDO, 0) > 0
              AND ISNULL(st.StockActual, 0) <= ISNULL(a.PUNTOPEDIDO, 0)
              AND (@ArticuloCodigoLike IS NULL OR CONVERT(varchar(50), a.IDARTICULO) LIKE @ArticuloCodigoLike)
              AND (@ArticuloDescripcionLike IS NULL OR a.DESCRIPCION LIKE @ArticuloDescripcionLike)
              AND (@Rubro IS NULL OR CONVERT(varchar(50), a.IDRUBRO) = @Rubro)
              AND (@Familia IS NULL OR CONVERT(varchar(50), a.IdFamilia) = @Familia)
            ORDER BY (ISNULL(st.StockActual, 0) - ISNULL(a.PUNTOPEDIDO, 0)) ASC, a.DESCRIPCION
            """, cn);
        bind?.Invoke(cmd);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            items.Add(new StockCriticoDto
            {
                IdArticulo = GetStringValue(rd, 0),
                Descripcion = GetStringValue(rd, 1),
                StockActual = GetDecimal(rd, 2),
                PuntoPedido = GetDecimal(rd, 3),
                Valorizado = GetDecimal(rd, 4)
            });
        }

        return items;
    }

    private async Task<IReadOnlyList<ContabilidadAsientoResumenDto>> QueryAsientosAsync(string sql, SqlConnection cn, Action<SqlCommand>? bind, CancellationToken ct)
    {
        var items = new List<ContabilidadAsientoResumenDto>();
        await using var cmd = new SqlCommand(sql, cn);
        bind?.Invoke(cmd);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            items.Add(new ContabilidadAsientoResumenDto
            {
                Fecha = rd.GetDateTime(0),
                Cuenta = GetStringValue(rd, 1),
                Detalle = GetStringValue(rd, 2),
                Tipo = GetStringValue(rd, 3),
                Importe = GetDecimal(rd, 4)
            });
        }

        return items;
    }

    private async Task<T?> QuerySingleAsync<T>(string sql, SqlConnection cn, Func<SqlDataReader, T> map, Action<SqlCommand>? bind, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(sql, cn);
        bind?.Invoke(cmd);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        return await rd.ReadAsync(ct) ? map(rd) : default;
    }

    private static void BindVentasFilters(SqlCommand cmd, VentasDashboardFilters filters)
    {
        BindDateRange(cmd, filters.FechaDesde, filters.FechaHasta);
        AddNullableString(cmd, "@ClienteLike", Like(filters.Cliente));
        AddNullableString(cmd, "@Usuario", filters.Usuario);
        AddNullableString(cmd, "@Sucursal", filters.Sucursal);
        AddNullableString(cmd, "@Deposito", filters.Deposito);
        AddNullableString(cmd, "@TipoComprobante", filters.TipoComprobante);
    }

    private static void BindStockFilters(SqlCommand cmd, StockDashboardFilters filters)
    {
        BindDateRange(cmd, filters.FechaDesde, filters.FechaHasta);
        AddNullableString(cmd, "@ArticuloCodigoLike", Like(filters.ArticuloCodigo));
        AddNullableString(cmd, "@ArticuloDescripcionLike", Like(filters.ArticuloDescripcion));
        AddNullableString(cmd, "@Rubro", filters.Rubro);
        AddNullableString(cmd, "@Familia", filters.Familia);
        AddNullableString(cmd, "@Deposito", filters.Deposito);
        AddNullableString(cmd, "@Sucursal", filters.Sucursal);
        AddNullableString(cmd, "@Estado", filters.Estado);
    }

    private static void BindCajaBancosFilters(SqlCommand cmd, CajaBancosDashboardFilters filters)
    {
        BindDateRange(cmd, filters.FechaDesde, filters.FechaHasta);
        AddNullableString(cmd, "@Caja", filters.Caja);
        AddNullableString(cmd, "@BancoCuenta", filters.BancoCuenta);
        AddNullableString(cmd, "@TextoLike", Like(filters.Texto));
    }

    private static void BindContabilidadFilters(SqlCommand cmd, ContabilidadDashboardFilters filters)
    {
        BindDateRange(cmd, filters.FechaDesde, filters.FechaHasta);
        AddNullableString(cmd, "@CuentaLike", Like(filters.CuentaContable));
        AddNullableString(cmd, "@DetalleLike", Like(filters.Detalle));
        AddNullableString(cmd, "@Usuario", filters.Usuario);
        AddNullableString(cmd, "@Sucursal", filters.Sucursal);
        AddNullableString(cmd, "@Tipo", filters.Tipo);
    }

    private static void BindDateRange(SqlCommand cmd, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        cmd.Parameters.AddWithValue("@FechaDesde", (object?)fechaDesde ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FechaHastaExclusive", fechaHasta.HasValue ? fechaHasta.Value.Date.AddDays(1) : DBNull.Value);
    }

    private static void AddNullableString(SqlCommand cmd, string parameterName, string? value)
        => cmd.Parameters.AddWithValue(parameterName, string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim());

    private static string? Like(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : $"%{value.Trim()}%";

    private static string GetStringValue(SqlDataReader rd, int index)
        => rd.IsDBNull(index) ? string.Empty : Convert.ToString(rd.GetValue(index), CultureInfo.InvariantCulture) ?? string.Empty;

    private static decimal GetDecimal(SqlDataReader rd, int index) =>
        rd.IsDBNull(index) ? 0 : Convert.ToDecimal(rd.GetValue(index), CultureInfo.InvariantCulture);

    private static int GetInt(SqlDataReader rd, int index) =>
        rd.IsDBNull(index) ? 0 : Convert.ToInt32(rd.GetValue(index), CultureInfo.InvariantCulture);

    private async Task<T> ExecuteLoggedAsync<T>(string module, string action, Func<CancellationToken, Task<T>> operation, string userMessage, CancellationToken ct)
    {
        try
        {
            return await operation(ct);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var incidentId = await appEvents.LogErrorAsync(module, action, ex, userMessage, null, AppEventSeverity.Error, ct);
            throw new InvalidOperationException($"{userMessage} Código: {incidentId}");
        }
    }

    private static VentasClienteResumenDto WithParticipacion(VentasClienteResumenDto item, decimal total)
        => new()
        {
            Cuenta = item.Cuenta,
            Cliente = item.Cliente,
            TotalFacturado = item.TotalFacturado,
            Participacion = total == 0 ? 0 : item.TotalFacturado / total,
            CantidadComprobantes = item.CantidadComprobantes,
            TicketPromedio = item.TicketPromedio,
            UltimaVenta = item.UltimaVenta
        };

    private static VentasRubroResumenDto WithParticipacion(VentasRubroResumenDto item, decimal total)
        => new()
        {
            Rubro = item.Rubro,
            TotalVendido = item.TotalVendido,
            Participacion = total == 0 ? 0 : item.TotalVendido / total,
            CantidadArticulos = item.CantidadArticulos,
            CantidadComprobantes = item.CantidadComprobantes
        };

    private static VentasFamiliaResumenDto WithParticipacion(VentasFamiliaResumenDto item, decimal total)
        => new()
        {
            Familia = item.Familia,
            TotalVendido = item.TotalVendido,
            Participacion = total == 0 ? 0 : item.TotalVendido / total,
            CantidadArticulos = item.CantidadArticulos,
            CantidadComprobantes = item.CantidadComprobantes
        };
}
