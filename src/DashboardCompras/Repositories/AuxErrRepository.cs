using DashboardCompras.Models;
using DashboardCompras.Services;
using Microsoft.Data.SqlClient;

namespace DashboardCompras.Repositories;

public sealed class AuxErrRepository(
    IConfiguration configuration,
    ISessionService sessionService,
    ILogger<AuxErrRepository> logger) : IAuxErrRepository
{
    private string ConnectionString => sessionService.GetConnectionString().Length > 0
        ? sessionService.GetConnectionString()
        : configuration.GetConnectionString("AlfaGestion")
          ?? throw new InvalidOperationException("No se configuró la cadena de conexión 'ConnectionStrings:AlfaGestion'.");

    public async Task<int> InsertAsync(AuxErrEntry entry, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.AUX_ERR
            (
                Proceso,
                Fecha,
                Error,
                Descripcion,
                Sql,
                Pc,
                Usuario
            )
            VALUES
            (
                @Proceso,
                GETDATE(),
                @Error,
                @Descripcion,
                @Sql,
                @Pc,
                @Usuario
            );

            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var cn = new SqlConnection(ConnectionString);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Proceso", Truncate(entry.Process, 120));
        cmd.Parameters.AddWithValue("@Error", entry.ErrorCode);
        cmd.Parameters.AddWithValue("@Descripcion", Truncate(entry.Description, 500));
        cmd.Parameters.AddWithValue("@Sql", (object?)NullIfEmpty(Truncate(entry.SqlDetail, 4000)) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Pc", (object?)NullIfEmpty(Truncate(entry.Pc, 120)) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Usuario", (object?)NullIfEmpty(Truncate(entry.UserName, 120)) ?? DBNull.Value);

        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is null || result is DBNull)
        {
            logger.LogWarning("AUX_ERR no devolvió ID para el proceso {Process}.", entry.Process);
            return 0;
        }

        return Convert.ToInt32(result);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
