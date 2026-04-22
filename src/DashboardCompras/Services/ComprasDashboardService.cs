using DashboardCompras.Models;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace DashboardCompras.Services;

public sealed partial class ComprasDashboardService(IConfiguration configuration, ILogger<ComprasDashboardService> logger, ISessionService sessionService) : IComprasDashboardService
{
    private const string HeaderFromClause = """
        FROM vw_compras_cabecera_dashboard c
        WHERE (@FechaDesde IS NULL OR c.FECHA >= @FechaDesde)
          AND (@FechaHasta IS NULL OR c.FECHA < DATEADD(day, 1, @FechaHasta))
          AND (@Proveedor IS NULL OR c.CUENTA LIKE '%' + @Proveedor + '%' OR c.RAZON_SOCIAL LIKE '%' + @Proveedor + '%')
          AND (@Usuario IS NULL OR c.USUARIO = @Usuario)
          AND (@Sucursal IS NULL OR c.SUCURSAL = @Sucursal)
          AND (@Deposito IS NULL OR CONVERT(varchar(20), c.IdDeposito) = @Deposito)
          AND (@Estado IS NULL OR c.EstadoComprobante = @Estado)
          AND (@TipoComprobante IS NULL OR c.TC = @TipoComprobante)
          AND (
                @Articulo IS NULL AND @ArticuloCodigo IS NULL AND @ArticuloDescripcion IS NULL AND @Rubro IS NULL AND @Familia IS NULL
                OR EXISTS (
                    SELECT 1
                    FROM vw_compras_detalle_dashboard d
                    WHERE d.TC = c.TC
                      AND d.IDCOMPROBANTE = c.IDCOMPROBANTE
                      AND d.CUENTA = c.CUENTA
                      AND (
                            @Articulo IS NULL OR LTRIM(RTRIM(d.IDARTICULO)) = LTRIM(RTRIM(@Articulo))
                          )
                      AND (
                            @ArticuloCodigo IS NULL OR LTRIM(RTRIM(d.IDARTICULO)) = LTRIM(RTRIM(@ArticuloCodigo))
                          )
                      AND (
                            @ArticuloDescripcion IS NULL OR d.DESCRIPCION_ARTICULO LIKE '%' + @ArticuloDescripcion + '%'
                          )
                      AND (@Rubro IS NULL OR d.RUBRO = @Rubro)
                      AND (@Familia IS NULL OR d.FAMILIA = @Familia)
                )
            )
        """;

    private const string HeaderWithDetailFromClause = """
        FROM vw_compras_cabecera_dashboard c
        LEFT JOIN (
            SELECT
                d.TC,
                d.IDCOMPROBANTE,
                d.CUENTA,
                COUNT(*) AS CantidadItems
            FROM vw_compras_detalle_dashboard d
            GROUP BY d.TC, d.IDCOMPROBANTE, d.CUENTA
        ) det
            ON det.TC = c.TC
           AND det.IDCOMPROBANTE = c.IDCOMPROBANTE
           AND det.CUENTA = c.CUENTA
        WHERE (@FechaDesde IS NULL OR c.FECHA >= @FechaDesde)
          AND (@FechaHasta IS NULL OR c.FECHA < DATEADD(day, 1, @FechaHasta))
          AND (@Proveedor IS NULL OR c.CUENTA LIKE '%' + @Proveedor + '%' OR c.RAZON_SOCIAL LIKE '%' + @Proveedor + '%')
          AND (@Usuario IS NULL OR c.USUARIO = @Usuario)
          AND (@Sucursal IS NULL OR c.SUCURSAL = @Sucursal)
          AND (@Deposito IS NULL OR CONVERT(varchar(20), c.IdDeposito) = @Deposito)
          AND (@Estado IS NULL OR c.EstadoComprobante = @Estado)
          AND (@TipoComprobante IS NULL OR c.TC = @TipoComprobante)
          AND (
                @Articulo IS NULL AND @ArticuloCodigo IS NULL AND @ArticuloDescripcion IS NULL AND @Rubro IS NULL AND @Familia IS NULL
                OR EXISTS (
                    SELECT 1
                    FROM vw_compras_detalle_dashboard d
                    WHERE d.TC = c.TC
                      AND d.IDCOMPROBANTE = c.IDCOMPROBANTE
                      AND d.CUENTA = c.CUENTA
                      AND (
                            @Articulo IS NULL OR LTRIM(RTRIM(d.IDARTICULO)) = LTRIM(RTRIM(@Articulo))
                          )
                      AND (
                            @ArticuloCodigo IS NULL OR LTRIM(RTRIM(d.IDARTICULO)) = LTRIM(RTRIM(@ArticuloCodigo))
                          )
                      AND (
                            @ArticuloDescripcion IS NULL OR d.DESCRIPCION_ARTICULO LIKE '%' + @ArticuloDescripcion + '%'
                          )
                      AND (@Rubro IS NULL OR d.RUBRO = @Rubro)
                      AND (@Familia IS NULL OR d.FAMILIA = @Familia)
                )
            )
        """;

    private const string DetailFromClause = """
        FROM vw_compras_detalle_dashboard d
        WHERE (@FechaDesde IS NULL OR d.FECHA >= @FechaDesde)
          AND (@FechaHasta IS NULL OR d.FECHA < DATEADD(day, 1, @FechaHasta))
          AND (@Proveedor IS NULL OR d.CUENTA = @Proveedor)
          AND (
                @Articulo IS NULL OR LTRIM(RTRIM(d.IDARTICULO)) = LTRIM(RTRIM(@Articulo))
              )
          AND (
                @ArticuloCodigo IS NULL OR LTRIM(RTRIM(d.IDARTICULO)) = LTRIM(RTRIM(@ArticuloCodigo))
              )
          AND (
                @ArticuloDescripcion IS NULL OR d.DESCRIPCION_ARTICULO LIKE '%' + @ArticuloDescripcion + '%'
              )
          AND (@Rubro IS NULL OR d.RUBRO = @Rubro)
          AND (@Familia IS NULL OR d.FAMILIA = @Familia)
          AND (@Usuario IS NULL OR d.USUARIO = @Usuario)
          AND (@Sucursal IS NULL OR d.SUCURSAL = @Sucursal)
          AND (@Deposito IS NULL OR CONVERT(varchar(20), d.IdDeposito) = @Deposito)
          AND (@TipoComprobante IS NULL OR d.TC = @TipoComprobante)
        """;

    private readonly ISessionService _sessionService = sessionService;
    private string _connectionString => _sessionService.GetConnectionString().Length > 0
        ? _sessionService.GetConnectionString()
        : configuration.GetConnectionString("AlfaGestion")
          ?? throw new InvalidOperationException("No se configuró la cadena de conexión 'ConnectionStrings:AlfaGestion'.");
    private readonly ILogger<ComprasDashboardService> _logger = logger;
    private readonly DashboardCompras.Configuration.DatosSqlOptions _sqlOptions =
        configuration.GetSection(DashboardCompras.Configuration.DatosSqlOptions.SectionName).Get<DashboardCompras.Configuration.DatosSqlOptions>() ?? new();

    private static object ToDbValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private async Task<T> MeasureAsync<T>(string operation, DashboardFilters? filters, Func<Task<T>> action)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await action();
            stopwatch.Stop();
            LogOperation(operation, stopwatch.ElapsedMilliseconds, filters);
            return result;
        }
        catch (SqlException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "SQL {Operacion} falló tras {ElapsedMs} ms. Timeout={Timeout}s. Filtros: {Filtros}",
                operation,
                stopwatch.ElapsedMilliseconds,
                _sqlOptions.CommandTimeoutSegundos,
                DescribeFilters(filters));
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Operación {Operacion} falló tras {ElapsedMs} ms. Filtros: {Filtros}",
                operation,
                stopwatch.ElapsedMilliseconds,
                DescribeFilters(filters));
            throw;
        }
    }

    private void LogOperation(string operation, long elapsedMs, DashboardFilters? filters)
    {
        if (elapsedMs >= _sqlOptions.LogConsultasLentasDesdeMs)
        {
            _logger.LogWarning(
                "SQL {Operacion} demoró {ElapsedMs} ms. Timeout={Timeout}s. Filtros: {Filtros}",
                operation,
                elapsedMs,
                _sqlOptions.CommandTimeoutSegundos,
                DescribeFilters(filters));
            return;
        }

        _logger.LogInformation(
            "SQL {Operacion} OK en {ElapsedMs} ms. Filtros: {Filtros}",
            operation,
            elapsedMs,
            DescribeFilters(filters));
    }

    private string DescribeFilters(DashboardFilters? filters)
    {
        if (filters is null)
        {
            return "sin filtros";
        }

        var parts = new List<string>();

        if (filters.FechaDesde.HasValue || filters.FechaHasta.HasValue)
        {
            parts.Add($"Fecha={filters.FechaDesde:dd/MM/yyyy}..{filters.FechaHasta:dd/MM/yyyy}");
        }

        AddFilterPart(parts, "Proveedor", filters.Proveedor);
        AddFilterPart(parts, "Artículo", filters.Articulo);
        AddFilterPart(parts, "CodArtículo", filters.ArticuloCodigo);
        AddFilterPart(parts, "DescArtículo", filters.ArticuloDescripcion);
        AddFilterPart(parts, "Rubro", filters.Rubro);
        AddFilterPart(parts, "Familia", filters.Familia);
        AddFilterPart(parts, "Usuario", filters.Usuario);
        AddFilterPart(parts, "Sucursal", filters.Sucursal);
        AddFilterPart(parts, "Depósito", filters.Deposito);
        AddFilterPart(parts, "Estado", filters.Estado);
        AddFilterPart(parts, "TC", filters.TipoComprobante);

        if (filters is ComprobantesFilter comprobantes)
        {
            parts.Add($"Página={comprobantes.Pagina}");
            parts.Add($"Tamaño={comprobantes.TamanioPagina}");
        }

        return parts.Count == 0 ? "sin filtros" : string.Join(" | ", parts);
    }

    private static void AddFilterPart(List<string> parts, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{label}={value}");
        }
    }

    private static ComprobanteDto MapComprobante(SqlDataReader reader) => new()
    {
        Tc = reader.SafeGetString("TC"),
        IdComprobante = reader.SafeGetString("IDCOMPROBANTE"),
        Numero = reader.SafeGetString("IDCOMPROBANTE"),
        Fecha = reader.GetDateTime("FECHA"),
        Cuenta = reader.SafeGetString("CUENTA"),
        RazonSocial = reader.SafeGetString("RAZON_SOCIAL"),
        Sucursal = reader.SafeGetString("SUCURSAL"),
        Deposito = reader.SafeGetString("Deposito"),
        Usuario = reader.SafeGetString("USUARIO"),
        NetoDashboard = reader.GetDecimal("NetoDashboard"),
        IvaDashboard = reader.GetDecimal("IvaDashboard"),
        ImporteDashboard = reader.GetDecimal("ImporteDashboard"),
        EstadoComprobante = reader.SafeGetString("EstadoComprobante"),
        CantidadItems = reader.GetInt32("CantidadItems"),
        TieneDetalle = reader.GetBoolean("TieneDetalle"),
        EsContable = reader.GetBoolean("EsContable"),
        IvaEnCero = reader.GetBoolean("IvaEnCero"),
        AlertaOperativa = reader.SafeGetString("AlertaOperativa")
    };
}

internal static class SqlDataReaderExtensions
{
    private static int GetOrdinalSafe(this SqlDataReader reader, string column)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), column, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    public static string SafeGetString(this SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinalSafe(column);
        if (ordinal < 0)
        {
            return string.Empty;
        }

        return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal)) ?? string.Empty;
    }

    public static int GetInt32(this SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinalSafe(column);
        return ordinal < 0 || reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
    }

    public static bool GetBoolean(this SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinalSafe(column);
        return ordinal >= 0 && !reader.IsDBNull(ordinal) && Convert.ToBoolean(reader.GetValue(ordinal));
    }

    public static decimal GetDecimal(this SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinalSafe(column);
        return ordinal < 0 || reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal));
    }

    public static DateTime GetDateTime(this SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinalSafe(column);
        return ordinal < 0 || reader.IsDBNull(ordinal) ? DateTime.MinValue : Convert.ToDateTime(reader.GetValue(ordinal));
    }

    public static DateTime? GetNullableDateTime(this SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinalSafe(column);
        return ordinal < 0 || reader.IsDBNull(ordinal) ? null : Convert.ToDateTime(reader.GetValue(ordinal));
    }
}
