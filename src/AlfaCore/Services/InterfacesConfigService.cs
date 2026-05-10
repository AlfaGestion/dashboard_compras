using AlfaCore.Models;
using Microsoft.Data.SqlClient;

namespace AlfaCore.Services;

public sealed class InterfacesConfigService(
    IConfiguration configuration,
    ISessionService sessionService,
    IAppEventService appEvents) : IInterfacesConfigService
{
    private const string ConfigGroup = "INTERFACES";
    private const string DefaultDestinoTipo = "FTP";
    private const string DefaultDestinoNombre = "Recepción principal";
    private const string DefaultRutaBase = "/";
    private const string DefaultFtpHost = "alfanet.ddns.net";
    private const int DefaultFtpPuerto = 21;
    private const string DefaultFtpUsuario = "ftpalfa";
    private const string DefaultFtpClave = "24681012";

    private string ConnectionString => sessionService.GetConnectionString().Length > 0
        ? sessionService.GetConnectionString()
        : configuration.GetConnectionString("AlfaGestion")
          ?? throw new InvalidOperationException("No se configuró la cadena de conexión 'ConnectionStrings:AlfaGestion'.");

    public Task<InterfacesUploadSettingsDto> GetUploadSettingsAsync(CancellationToken ct = default)
        => ExecuteLoggedAsync("Interfaces", "GetUploadSettings", async token =>
        {
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            var detailColumn = await ResolveDetailColumnAsync(cn, token);

            var sql = $"""
                SELECT
                    UPPER(LTRIM(RTRIM(CLAVE))),
                    ISNULL(VALOR, ''),
                    ISNULL({detailColumn}, '')
                FROM dbo.TA_CONFIGURACION
                WHERE UPPER(LTRIM(RTRIM(CLAVE))) IN
                (
                    'INTERFACESRECEPCIONTIPO',
                    'INTERFACESRECEPCIONNOMBRE',
                    'INTERFACESRECEPCIONRUTA',
                    'INTERFACESFTPHOST',
                    'INTERFACESFTPPUERTO',
                    'INTERFACESFTPUSUARIO',
                    'INTERFACESFTPCLAVE',
                    'INTERFACESFTPPASIVO',
                    'INTERFACESESTADOINICIAL',
                    'INTERFACESTAMANOMAXIMOMB',
                    'INTERFACESEXTENSIONESPERMITIDAS'
                )
                """;

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            await using var cmd = new SqlCommand(sql, cn);
            await using var rd = await cmd.ExecuteReaderAsync(token);
            while (await rd.ReadAsync(token))
            {
                var key = GetString(rd, 0);
                var value = ResolveStoredValue(GetString(rd, 1), GetString(rd, 2));
                values[key] = value;
            }

            var estadoInicial = ReadValue(values, "INTERFACESESTADOINICIAL", "A_PROCESAR");
            var tamanoMb = 25;
            if (int.TryParse(ReadValue(values, "INTERFACESTAMANOMAXIMOMB", "25"), out var parsedMb) && parsedMb > 0)
                tamanoMb = parsedMb;

            var extensiones = ParseExtensions(ReadValue(values, "INTERFACESEXTENSIONESPERMITIDAS", ".pdf,.jpg,.jpeg,.png,.xls,.xlsx,.csv,.txt"));
            var ftpPuerto = DefaultFtpPuerto;
            if (int.TryParse(ReadValue(values, "INTERFACESFTPPUERTO", DefaultFtpPuerto.ToString()), out var parsedPuerto) && parsedPuerto > 0)
                ftpPuerto = parsedPuerto;

            var ftpPasivo = !string.Equals(ReadValue(values, "INTERFACESFTPPASIVO", "SI"), "NO", StringComparison.OrdinalIgnoreCase);

            return new InterfacesUploadSettingsDto
            {
                DestinoTipo = ReadValue(values, "INTERFACESRECEPCIONTIPO", DefaultDestinoTipo).Trim().ToUpperInvariant(),
                DestinoNombre = ReadValue(values, "INTERFACESRECEPCIONNOMBRE", DefaultDestinoNombre).Trim(),
                RutaBase = ReadValue(values, "INTERFACESRECEPCIONRUTA", DefaultRutaBase).Trim(),
                FtpHost = ReadValue(values, "INTERFACESFTPHOST", DefaultFtpHost).Trim(),
                FtpPuerto = ftpPuerto,
                FtpUsuario = ReadValue(values, "INTERFACESFTPUSUARIO", DefaultFtpUsuario).Trim(),
                FtpClave = ReadValue(values, "INTERFACESFTPCLAVE", DefaultFtpClave).Trim(),
                FtpModoPasivo = ftpPasivo,
                EstadoInicialCodigo = estadoInicial.Trim(),
                TamanoMaximoMb = tamanoMb,
                ExtensionesPermitidas = extensiones
            };
        }, "No se pudo cargar la configuración de Interfaces.", ct);

    public async Task SaveUploadSettingsAsync(InterfacesUploadSettingsDto settings, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await ExecuteLoggedAsync("Interfaces", "SaveUploadSettings", async token =>
        {
            var normalized = Normalize(settings);

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            var detailColumn = await ResolveDetailColumnAsync(cn, token);
            await using var tx = await cn.BeginTransactionAsync(token);

            foreach (var item in BuildItems(normalized))
            {
                var stored = SplitStoredValue(item.Value);
                var sql = $"""
                    UPDATE dbo.TA_CONFIGURACION
                    SET
                        VALOR = @Valor,
                        {detailColumn} = @ValorAux,
                        GRUPO = @Grupo
                    WHERE UPPER(LTRIM(RTRIM(CLAVE))) = @ClaveNormalizada;

                    IF @@ROWCOUNT = 0
                    BEGIN
                        INSERT INTO dbo.TA_CONFIGURACION (CLAVE, VALOR, {detailColumn}, GRUPO)
                        VALUES (@Clave, @Valor, @ValorAux, @Grupo);
                    END;
                    """;

                await using var cmd = new SqlCommand(sql, cn, (SqlTransaction)tx);
                cmd.Parameters.AddWithValue("@ClaveNormalizada", item.Key.ToUpperInvariant());
                cmd.Parameters.AddWithValue("@Clave", item.Key);
                cmd.Parameters.AddWithValue("@Valor", DbNullable(stored.Value));
                cmd.Parameters.AddWithValue("@ValorAux", DbNullable(stored.AuxValue));
                cmd.Parameters.AddWithValue("@Grupo", ConfigGroup);
                await cmd.ExecuteNonQueryAsync(token);
            }

            await tx.CommitAsync(token);

            await appEvents.LogAuditAsync(
                "Interfaces",
                "SaveUploadSettings",
                "TA_CONFIGURACION",
                ConfigGroup,
                "Configuración de recepción documental actualizada.",
                new
                {
                    normalized.DestinoTipo,
                    normalized.DestinoNombre,
                    normalized.RutaBase,
                    normalized.FtpHost,
                    normalized.FtpPuerto,
                    normalized.FtpUsuario,
                    normalized.FtpModoPasivo,
                    normalized.EstadoInicialCodigo,
                    normalized.TamanoMaximoMb
                },
                token);

            return true;
        }, "No se pudo guardar la configuración de Interfaces.", ct);
    }

    private static async Task<string> ResolveDetailColumnAsync(SqlConnection cn, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (1) name
            FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.TA_CONFIGURACION')
              AND LOWER(name) IN (N'valoraux', N'valor_aux')
            ORDER BY name
            """;

        await using var cmd = new SqlCommand(sql, cn);
        var result = await cmd.ExecuteScalarAsync(ct);
        var column = Convert.ToString(result) ?? string.Empty;
        return string.IsNullOrWhiteSpace(column) ? "DESCRIPCION" : column;
    }

    private static IReadOnlyList<string> ParseExtensions(string value)
    {
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static item => item.StartsWith('.') ? item.Trim().ToLowerInvariant() : "." + item.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<(string Key, string Value)> BuildItems(InterfacesUploadSettingsDto settings)
    {
        yield return ("InterfacesRecepcionTipo", settings.DestinoTipo);
        yield return ("InterfacesRecepcionNombre", settings.DestinoNombre);
        yield return ("InterfacesRecepcionRuta", settings.RutaBase);
        yield return ("InterfacesFtpHost", settings.FtpHost);
        yield return ("InterfacesFtpPuerto", settings.FtpPuerto.ToString());
        yield return ("InterfacesFtpUsuario", settings.FtpUsuario);
        yield return ("InterfacesFtpClave", settings.FtpClave);
        yield return ("InterfacesFtpPasivo", settings.FtpModoPasivo ? "SI" : "NO");
        yield return ("InterfacesEstadoInicial", settings.EstadoInicialCodigo);
        yield return ("InterfacesTamanoMaximoMb", settings.TamanoMaximoMb.ToString());
        yield return ("InterfacesExtensionesPermitidas", string.Join(",", settings.ExtensionesPermitidas));
    }

    private static InterfacesUploadSettingsDto Normalize(InterfacesUploadSettingsDto settings)
    {
        var tipo = string.Equals(settings.DestinoTipo, "CARPETA", StringComparison.OrdinalIgnoreCase) ? "CARPETA" : "FTP";
        var ruta = string.IsNullOrWhiteSpace(settings.RutaBase) ? DefaultRutaBase : settings.RutaBase.Trim();
        if (tipo == "FTP")
            ruta = InterfacesUploadSettingsDto.NormalizeFtpPath(ruta);

        var extensiones = ParseExtensions(string.Join(",", settings.ExtensionesPermitidas));
        if (extensiones.Count == 0)
            extensiones = ParseExtensions(".pdf,.jpg,.jpeg,.png,.xls,.xlsx,.csv,.txt");

        return new InterfacesUploadSettingsDto
        {
            DestinoTipo = tipo,
            DestinoNombre = string.IsNullOrWhiteSpace(settings.DestinoNombre) ? DefaultDestinoNombre : settings.DestinoNombre.Trim(),
            RutaBase = ruta,
            FtpHost = string.IsNullOrWhiteSpace(settings.FtpHost) ? DefaultFtpHost : settings.FtpHost.Trim(),
            FtpPuerto = settings.FtpPuerto <= 0 ? DefaultFtpPuerto : settings.FtpPuerto,
            FtpUsuario = string.IsNullOrWhiteSpace(settings.FtpUsuario) ? DefaultFtpUsuario : settings.FtpUsuario.Trim(),
            FtpClave = string.IsNullOrWhiteSpace(settings.FtpClave) ? DefaultFtpClave : settings.FtpClave.Trim(),
            FtpModoPasivo = settings.FtpModoPasivo,
            EstadoInicialCodigo = string.IsNullOrWhiteSpace(settings.EstadoInicialCodigo) ? "A_PROCESAR" : settings.EstadoInicialCodigo.Trim(),
            TamanoMaximoMb = settings.TamanoMaximoMb <= 0 ? 25 : settings.TamanoMaximoMb,
            ExtensionesPermitidas = extensiones
        };
    }

    private static string ResolveStoredValue(string value, string auxValue)
        => !string.IsNullOrWhiteSpace(value) ? value.Trim() : auxValue.Trim();

    private static string ReadValue(Dictionary<string, string> values, string key, string fallback = "")
        => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    private static string GetString(SqlDataReader rd, int index)
        => rd.IsDBNull(index) ? string.Empty : Convert.ToString(rd.GetValue(index)) ?? string.Empty;

    private static (string Value, string AuxValue) SplitStoredValue(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        if (normalized.Length > 150)
            return (string.Empty, normalized);

        return (normalized, string.Empty);
    }

    private static object DbNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

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
