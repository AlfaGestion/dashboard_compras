using AlfaCore.Models;
using Microsoft.Data.SqlClient;

namespace AlfaCore.Services;

public sealed class AuditoriaService(
    IConfiguration configuration,
    ISessionService sessionService,
    IAppEventService appEvents) : IAuditoriaService
{
    private string ConnectionString => sessionService.GetConnectionString().Length > 0
        ? sessionService.GetConnectionString()
        : configuration.GetConnectionString("AlfaGestion")
          ?? throw new InvalidOperationException("No se configuró la cadena de conexión 'ConnectionStrings:AlfaGestion'.");

    public async Task<AuditoriaResumenDto> GetResumenAsync(CancellationToken ct = default)
    {
        return await ExecuteLoggedAsync("Auditoria", "GetResumen", async token =>
        {
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);

            var today = await ScalarIntAsync("""
                SELECT COUNT(*)
                FROM dbo.AUX_ERR
                WHERE CAST(Fecha AS date) = CAST(GETDATE() AS date)
                """, cn, token);

            var last7 = await ScalarIntAsync("""
                SELECT COUNT(*)
                FROM dbo.AUX_ERR
                WHERE Fecha >= DATEADD(day, -7, GETDATE())
                """, cn, token);

            var series = await QueryRankingAsync("""
                SELECT TOP (14)
                    CONVERT(varchar(10), CAST(Fecha AS date), 103) AS Label,
                    COUNT(*) AS Value
                FROM dbo.AUX_ERR
                WHERE Fecha >= DATEADD(day, -14, GETDATE())
                GROUP BY CAST(Fecha AS date)
                ORDER BY CAST(Fecha AS date)
                """, cn, token);

            var processes = await QueryRankingAsync("""
                SELECT TOP (8)
                    ISNULL(NULLIF(LTRIM(RTRIM(Proceso)), ''), '(sin proceso)') AS Label,
                    COUNT(*) AS Value
                FROM dbo.AUX_ERR
                WHERE Fecha >= DATEADD(day, -30, GETDATE())
                GROUP BY Proceso
                ORDER BY COUNT(*) DESC, Proceso
                """, cn, token);

            var users = await QueryRankingAsync("""
                SELECT TOP (8)
                    ISNULL(NULLIF(LTRIM(RTRIM(Usuario)), ''), '(sin usuario)') AS Label,
                    COUNT(*) AS Value
                FROM dbo.AUX_ERR
                WHERE Fecha >= DATEADD(day, -30, GETDATE())
                GROUP BY Usuario
                ORDER BY COUNT(*) DESC, Usuario
                """, cn, token);

            return new AuditoriaResumenDto
            {
                ErrorsToday = today,
                ErrorsLast7Days = last7,
                TopProcess = processes.FirstOrDefault()?.Label ?? "—",
                TopUser = users.FirstOrDefault()?.Label ?? "—",
                ErrorsByDay = series.Select(x => new AuditoriaSerieDto { Label = x.Label, Value = x.Value }).ToList(),
                TopProcesses = processes,
                TopUsers = users
            };
        }, "No se pudo cargar el resumen de auditoría.", ct);
    }

    public async Task<IReadOnlyList<AuditoriaErrorRowDto>> SearchErrorsAsync(AuditoriaErrorFilterDto filter, CancellationToken ct = default)
    {
        return await ExecuteLoggedAsync("Auditoria", "SearchErrors", async token =>
        {
            var sql = $"""
                SELECT TOP ({Math.Max(1, filter.MaxRows)})
                    ID,
                    Fecha,
                    ISNULL(Proceso, ''),
                    ISNULL(Error, 0),
                    ISNULL(Descripcion, ''),
                    ISNULL(Sql, ''),
                    ISNULL(Pc, ''),
                    ISNULL(Usuario, '')
                FROM dbo.AUX_ERR
                WHERE (@Desde IS NULL OR Fecha >= @Desde)
                  AND (@Hasta IS NULL OR Fecha < DATEADD(day, 1, @Hasta))
                  AND (@Usuario = '' OR ISNULL(Usuario, '') = @Usuario)
                  AND (@Proceso = '' OR ISNULL(Proceso, '') = @Proceso)
                  AND (@Pc = '' OR ISNULL(Pc, '') LIKE '%' + @Pc + '%')
                  AND (@Error IS NULL OR ISNULL(Error, 0) = @Error)
                  AND (
                        @Texto = ''
                        OR ISNULL(Descripcion, '') LIKE '%' + @Texto + '%'
                        OR ISNULL(Sql, '') LIKE '%' + @Texto + '%'
                        OR ISNULL(Proceso, '') LIKE '%' + @Texto + '%'
                        OR ISNULL(Usuario, '') LIKE '%' + @Texto + '%'
                      )
                ORDER BY Fecha DESC, ID DESC
                """;

            var items = new List<AuditoriaErrorRowDto>();
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@Desde", (object?)filter.Desde ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Hasta", (object?)filter.Hasta ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Usuario", filter.Usuario ?? string.Empty);
            cmd.Parameters.AddWithValue("@Proceso", filter.Proceso ?? string.Empty);
            cmd.Parameters.AddWithValue("@Pc", filter.Pc ?? string.Empty);
            cmd.Parameters.AddWithValue("@Error", (object?)filter.ErrorCodigo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Texto", filter.Texto ?? string.Empty);
            await using var rd = await cmd.ExecuteReaderAsync(token);
            while (await rd.ReadAsync(token))
            {
                items.Add(MapError(rd));
            }

            return (IReadOnlyList<AuditoriaErrorRowDto>)items;
        }, "No se pudieron buscar errores de auditoría.", ct);
    }

    public async Task<AuditoriaErrorRowDto?> GetErrorByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteLoggedAsync("Auditoria", "GetErrorById", async token =>
        {
            const string sql = """
                SELECT
                    ID,
                    Fecha,
                    ISNULL(Proceso, ''),
                    ISNULL(Error, 0),
                    ISNULL(Descripcion, ''),
                    ISNULL(Sql, ''),
                    ISNULL(Pc, ''),
                    ISNULL(Usuario, '')
                FROM dbo.AUX_ERR
                WHERE ID = @Id
                """;

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@Id", id);
            await using var rd = await cmd.ExecuteReaderAsync(token);
            return await rd.ReadAsync(token) ? MapError(rd) : null;
        }, "No se pudo cargar el error solicitado.", ct);
    }

    public async Task<IReadOnlyList<string>> GetUsuariosAsync(CancellationToken ct = default)
    {
        return await QueryDistinctAsync("Usuario", ct);
    }

    public async Task<IReadOnlyList<string>> GetProcesosAsync(CancellationToken ct = default)
    {
        return await QueryDistinctAsync("Proceso", ct);
    }

    private async Task<IReadOnlyList<string>> QueryDistinctAsync(string field, CancellationToken ct)
    {
        return await ExecuteLoggedAsync("Auditoria", $"GetDistinct{field}", async token =>
        {
            var sql = $"""
                SELECT DISTINCT TOP (200)
                    LTRIM(RTRIM(ISNULL({field}, '')))
                FROM dbo.AUX_ERR
                WHERE ISNULL({field}, '') <> ''
                ORDER BY LTRIM(RTRIM(ISNULL({field}, '')))
                """;

            var items = new List<string>();
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            await using var rd = await cmd.ExecuteReaderAsync(token);
            while (await rd.ReadAsync(token))
            {
                items.Add(rd.GetString(0));
            }

            return (IReadOnlyList<string>)items;
        }, $"No se pudo cargar la lista de {field.ToLowerInvariant()}.", ct);
    }

    private static async Task<int> ScalarIntAsync(string sql, SqlConnection cn, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(sql, cn);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is null or DBNull ? 0 : Convert.ToInt32(result);
    }

    private static async Task<List<AuditoriaRankingItemDto>> QueryRankingAsync(string sql, SqlConnection cn, CancellationToken ct)
    {
        var items = new List<AuditoriaRankingItemDto>();
        await using var cmd = new SqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            items.Add(new AuditoriaRankingItemDto
            {
                Label = rd.GetString(0),
                Value = rd.GetInt32(1)
            });
        }

        return items;
    }

    private static AuditoriaErrorRowDto MapError(SqlDataReader rd)
    {
        return new AuditoriaErrorRowDto
        {
            Id = Convert.ToInt32(rd.GetValue(0)),
            Fecha = rd.GetDateTime(1),
            Proceso = rd.GetString(2),
            ErrorCodigo = rd.IsDBNull(3) ? 0 : Convert.ToInt32(rd.GetValue(3)),
            Descripcion = rd.GetString(4),
            Sql = rd.GetString(5),
            Pc = rd.GetString(6),
            Usuario = rd.GetString(7)
        };
    }

    private async Task<T> ExecuteLoggedAsync<T>(
        string module,
        string action,
        Func<CancellationToken, Task<T>> operation,
        string userMessage,
        CancellationToken ct)
    {
        try
        {
            return await operation(ct);
        }
        catch (SqlException ex) when (ex.Number == 208)
        {
            throw new InvalidOperationException("La tabla AUX_ERR no existe en esta base de datos activa.");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var incidentId = await appEvents.LogErrorAsync(module, action, ex, userMessage, null, AppEventSeverity.Error, ct);
            throw new AppUserFacingException(userMessage, incidentId, ex);
        }
    }
}
