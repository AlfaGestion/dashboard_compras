using AlfaCore.Models;
using Microsoft.Data.SqlClient;

namespace AlfaCore.Services;

public sealed class UsuariosService(
    IConfiguration configuration,
    ISessionService sessionService,
    IAppEventService appEvents,
    IWebHostEnvironment env,
    UsuariosPasswordCodec passwordCodec) : IUsuariosService
{
    private const string ModuleName = "Usuarios";
    private const string SistemaFijo = "CN000PR";
    private const string DefaultCaja = "1";
    private const string DefaultUnidadNegocio = "   1";

    private string ConnectionString => sessionService.GetConnectionString().Length > 0
        ? sessionService.GetConnectionString()
        : configuration.GetConnectionString("AlfaGestion")
          ?? throw new InvalidOperationException("No se configuró la cadena de conexión 'ConnectionStrings:AlfaGestion'.");

    public Task<IReadOnlyList<UsuarioGridItemDto>> SearchAsync(UsuariosFilters filters, CancellationToken ct = default)
        => ExecuteLoggedAsync(ModuleName, "Search", async token =>
        {
            filters ??= new UsuariosFilters();
            var sql = $"""
                SELECT TOP ({Math.Max(1, Math.Min(filters.MaxRows, 500))})
                    ISNULL(NOMBRE, ''),
                    ISNULL(email_de, ''),
                    ISNULL(EsGrupo, 0),
                    ISNULL(CambiarProximoInicio, 0),
                    ISNULL(Activo, 1),
                    FechaHora_Grabacion,
                    FechaHora_Modificacion
                FROM dbo.TA_USUARIOS
                WHERE UPPER(LTRIM(RTRIM(SISTEMA))) = @Sistema
                  AND (
                        @Texto = ''
                        OR ISNULL(NOMBRE, '') LIKE '%' + @Texto + '%'
                        OR ISNULL(email_de, '') LIKE '%' + @Texto + '%'
                      )
                  AND (@Activo IS NULL OR ISNULL(Activo, 1) = @Activo)
                  AND (@EsGrupo IS NULL OR ISNULL(EsGrupo, 0) = @EsGrupo)
                ORDER BY ISNULL(Activo, 1) DESC, NOMBRE ASC
                """;

            var rows = new List<UsuarioGridItemDto>();
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@Sistema", SistemaFijo);
            cmd.Parameters.AddWithValue("@Texto", filters.Texto?.Trim() ?? string.Empty);
            cmd.Parameters.AddWithValue("@Activo", filters.Activo.HasValue ? filters.Activo.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@EsGrupo", filters.EsGrupo.HasValue ? filters.EsGrupo.Value : DBNull.Value);

            await using var rd = await cmd.ExecuteReaderAsync(token);
            while (await rd.ReadAsync(token))
            {
                var nombre = GetString(rd, 0);
                rows.Add(new UsuarioGridItemDto
                {
                    Nombre = nombre,
                    Email = GetString(rd, 1),
                    EsGrupo = GetBool(rd, 2),
                    CambiarProximoInicio = GetBool(rd, 3),
                    Activo = GetBool(rd, 4),
                    FechaHoraGrabacion = rd.IsDBNull(5) ? null : rd.GetDateTime(5),
                    FechaHoraModificacion = rd.IsDBNull(6) ? null : rd.GetDateTime(6),
                    TieneFoto = await PhotoExistsAsync(nombre, token)
                });
            }

            return (IReadOnlyList<UsuarioGridItemDto>)rows;
        }, "No se pudieron cargar los usuarios.", ct);

    public Task<UsuarioDetailDto?> GetByIdAsync(string nombre, CancellationToken ct = default)
        => ExecuteLoggedAsync(ModuleName, "GetById", async token =>
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return null;

            const string sql = """
                SELECT
                    ISNULL(NOMBRE, ''),
                    ISNULL(email_de, ''),
                    ISNULL(EsGrupo, 0),
                    ISNULL(CambiarProximoInicio, 0),
                    ISNULL(Activo, 1),
                    ISNULL(PASSWORD, ''),
                    FechaHora_Grabacion,
                    FechaHora_Modificacion
                FROM dbo.TA_USUARIOS
                WHERE UPPER(LTRIM(RTRIM(SISTEMA))) = @Sistema
                  AND UPPER(LTRIM(RTRIM(NOMBRE))) = @Nombre
                """;

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@Sistema", SistemaFijo);
            cmd.Parameters.AddWithValue("@Nombre", nombre.Trim().ToUpperInvariant());
            await using var rd = await cmd.ExecuteReaderAsync(token);

            if (!await rd.ReadAsync(token))
                return null;

            var decoded = passwordCodec.Decode(GetString(rd, 5));
            var canonicalName = GetString(rd, 0);
            var photoInfo = await TryGetPhotoInfoAsync(canonicalName, token);

            return new UsuarioDetailDto
            {
                Nombre = canonicalName,
                Email = GetString(rd, 1),
                EsGrupo = GetBool(rd, 2),
                CambiarProximoInicio = GetBool(rd, 3),
                Activo = GetBool(rd, 4),
                ContrasenaDecodificada = decoded,
                TieneFoto = photoInfo.Exists,
                FotoCacheToken = photoInfo.CacheToken,
                FechaHoraGrabacion = rd.IsDBNull(6) ? null : rd.GetDateTime(6),
                FechaHoraModificacion = rd.IsDBNull(7) ? null : rd.GetDateTime(7)
            };
        }, "No se pudo cargar el usuario seleccionado.", ct);

    public Task<string> SaveAsync(UsuarioSaveRequest request, CancellationToken ct = default)
        => ExecuteLoggedAsync(ModuleName, "Save", async token =>
        {
            ArgumentNullException.ThrowIfNull(request);
            var normalized = NormalizeRequest(request);
            ValidateRequest(normalized);

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);

            var isNew = string.IsNullOrWhiteSpace(normalized.NombreOriginal);
            var oldName = normalized.NombreOriginal.Trim();
            var newName = normalized.Nombre.Trim();

            if (isNew)
            {
                if (await ExistsAsync(cn, newName, token))
                    throw new InvalidOperationException("Ya existe un usuario con ese nombre en el sistema actual.");
            }
            else
            {
                if (!await ExistsAsync(cn, oldName, token))
                    throw new InvalidOperationException("El usuario seleccionado ya no existe en la base activa.");

                if (!string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase) &&
                    await ExistsAsync(cn, newName, token))
                {
                    throw new InvalidOperationException("Ya existe otro usuario con ese nombre en el sistema actual.");
                }
            }

            await using var tx = await cn.BeginTransactionAsync(token);

            if (isNew)
            {
                const string insertSql = """
                    INSERT INTO dbo.TA_USUARIOS
                    (
                        Nombre,
                        Sistema,
                        Password,
                        FechaHora_Grabacion,
                        FechaHora_Modificacion,
                        CambiarProximoInicio,
                        EsGrupo,
                        IDCAJA,
                        UNEGOCIO,
                        email_de,
                        V_ModificaArtLuegoDeCargado,
                        Activo
                    )
                    VALUES
                    (
                        @Nombre,
                        @Sistema,
                        @Password,
                        GETDATE(),
                        GETDATE(),
                        @CambiarProximoInicio,
                        @EsGrupo,
                        @IdCaja,
                        @UNegocio,
                        @Email,
                        @ModificaArt,
                        1
                    );
                    """;

                await using var insertCmd = new SqlCommand(insertSql, cn, (SqlTransaction)tx);
                FillSaveParameters(insertCmd, normalized, newName);
                await insertCmd.ExecuteNonQueryAsync(token);
            }
            else
            {
                const string updateSql = """
                    UPDATE dbo.TA_USUARIOS
                    SET
                        Nombre = @Nombre,
                        Password = @Password,
                        FechaHora_Modificacion = GETDATE(),
                        CambiarProximoInicio = @CambiarProximoInicio,
                        EsGrupo = @EsGrupo,
                        IDCAJA = @IdCaja,
                        UNEGOCIO = @UNegocio,
                        email_de = @Email,
                        V_ModificaArtLuegoDeCargado = @ModificaArt
                    WHERE UPPER(LTRIM(RTRIM(Sistema))) = @Sistema
                      AND UPPER(LTRIM(RTRIM(Nombre))) = @NombreOriginal;
                    """;

                await using var updateCmd = new SqlCommand(updateSql, cn, (SqlTransaction)tx);
                FillSaveParameters(updateCmd, normalized, newName);
                updateCmd.Parameters.AddWithValue("@NombreOriginal", oldName.ToUpperInvariant());
                await updateCmd.ExecuteNonQueryAsync(token);
            }

            await tx.CommitAsync(token);

            await ApplyPhotoChangesAsync(oldName, newName, normalized, token);

            await appEvents.LogAuditAsync(
                ModuleName,
                isNew ? "Create" : "Update",
                "TA_USUARIOS",
                $"{newName}|{SistemaFijo}",
                isNew ? "Usuario creado." : "Usuario actualizado.",
                new
                {
                    Nombre = newName,
                    normalized.Email,
                    normalized.EsGrupo,
                    normalized.CambiarProximoInicio,
                    Activo = true
                },
                token);

            return newName;
        }, "No se pudo guardar el usuario.", ct);

    public Task DeactivateAsync(string nombre, CancellationToken ct = default)
        => ExecuteLoggedAsync(ModuleName, "Deactivate", async token =>
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new InvalidOperationException("No se recibió el usuario a desactivar.");

            const string sql = """
                UPDATE dbo.TA_USUARIOS
                SET
                    Activo = 0,
                    FechaHora_Modificacion = GETDATE()
                WHERE UPPER(LTRIM(RTRIM(Sistema))) = @Sistema
                  AND UPPER(LTRIM(RTRIM(Nombre))) = @Nombre;
                """;

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@Sistema", SistemaFijo);
            cmd.Parameters.AddWithValue("@Nombre", nombre.Trim().ToUpperInvariant());
            var affected = await cmd.ExecuteNonQueryAsync(token);
            if (affected == 0)
                throw new InvalidOperationException("El usuario seleccionado ya no existe en la base activa.");

            await appEvents.LogAuditAsync(
                ModuleName,
                "Deactivate",
                "TA_USUARIOS",
                $"{nombre.Trim()}|{SistemaFijo}",
                "Usuario dado de baja.",
                new { Nombre = nombre.Trim(), Activo = false },
                token);
        }, "No se pudo dar de baja el usuario.", ct);

    public Task<UsuarioPhotoServeDto?> GetPhotoForServeAsync(string nombre, CancellationToken ct = default)
        => ExecuteLoggedAsync(ModuleName, "GetPhotoForServe", async token =>
        {
            var path = await TryResolvePhotoPathAsync(nombre, token);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            return new UsuarioPhotoServeDto
            {
                RutaCompleta = path,
                MimeType = "image/jpeg",
                NombreArchivo = Path.GetFileName(path)
            };
        }, "No se pudo obtener la imagen del usuario.", ct);

    private void FillSaveParameters(SqlCommand cmd, UsuarioSaveRequest request, string newName)
    {
        cmd.Parameters.AddWithValue("@Nombre", newName);
        cmd.Parameters.AddWithValue("@Sistema", SistemaFijo);
        cmd.Parameters.AddWithValue("@Password", DbNullable(request.EsGrupo ? string.Empty : passwordCodec.Encode(request.Contrasena)));
        cmd.Parameters.AddWithValue("@CambiarProximoInicio", request.EsGrupo ? false : request.CambiarProximoInicio);
        cmd.Parameters.AddWithValue("@EsGrupo", request.EsGrupo);
        cmd.Parameters.AddWithValue("@IdCaja", DefaultCaja);
        cmd.Parameters.AddWithValue("@UNegocio", DefaultUnidadNegocio);
        cmd.Parameters.AddWithValue("@Email", DbNullable(request.Email));
        cmd.Parameters.AddWithValue("@ModificaArt", true);
    }

    private async Task ApplyPhotoChangesAsync(string oldName, string newName, UsuarioSaveRequest request, CancellationToken ct)
    {
        var oldPath = await TryResolvePhotoPathAsync(string.IsNullOrWhiteSpace(oldName) ? newName : oldName, ct);
        var newPath = await TryResolvePhotoPathAsync(newName, ct);
        if (string.IsNullOrWhiteSpace(newPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);

        if (request.QuitarFoto)
        {
            if (!string.IsNullOrWhiteSpace(oldPath) && File.Exists(oldPath))
                File.Delete(oldPath);

            if (!string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase) && File.Exists(newPath))
                File.Delete(newPath);

            return;
        }

        if (request.FotoContenido is { Length: > 0 })
        {
            await File.WriteAllBytesAsync(newPath, request.FotoContenido, ct);
            if (!string.IsNullOrWhiteSpace(oldPath) &&
                !string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase) &&
                File.Exists(oldPath))
            {
                File.Delete(oldPath);
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(oldPath) &&
            !string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase) &&
            File.Exists(oldPath))
        {
            File.Copy(oldPath, newPath, true);
            File.Delete(oldPath);
        }
    }

    private static UsuarioSaveRequest NormalizeRequest(UsuarioSaveRequest request)
        => new()
        {
            NombreOriginal = request.NombreOriginal?.Trim() ?? string.Empty,
            Nombre = request.Nombre?.Trim() ?? string.Empty,
            Email = request.Email?.Trim() ?? string.Empty,
            EsGrupo = request.EsGrupo,
            CambiarProximoInicio = request.CambiarProximoInicio,
            Contrasena = request.Contrasena ?? string.Empty,
            FotoContenido = request.FotoContenido,
            FotoNombreOriginal = request.FotoNombreOriginal ?? string.Empty,
            FotoMimeType = request.FotoMimeType ?? string.Empty,
            QuitarFoto = request.QuitarFoto
        };

    private static void ValidateRequest(UsuarioSaveRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new InvalidOperationException("El nombre de usuario es obligatorio.");

        if (request.Nombre.Length > 50)
            throw new InvalidOperationException("El nombre de usuario no puede superar los 50 caracteres.");

        if (request.Email.Length > 150)
            throw new InvalidOperationException("El email no puede superar los 150 caracteres.");

        if (!request.EsGrupo)
        {
            if (string.IsNullOrWhiteSpace(request.Contrasena))
                throw new InvalidOperationException("La contraseña es obligatoria para usuarios comunes.");

            if (request.Contrasena.Length > 13)
                throw new InvalidOperationException("La contraseña no puede superar los 13 caracteres por compatibilidad con la base actual.");
        }

        if (request.FotoContenido is { Length: > 0 })
        {
            var extension = Path.GetExtension(request.FotoNombreOriginal ?? string.Empty).Trim().ToLowerInvariant();
            if (extension is not ".jpg" and not ".jpeg")
                throw new InvalidOperationException("La imagen del usuario debe estar en formato JPG.");
        }
    }

    private async Task<bool> ExistsAsync(SqlConnection cn, string nombre, CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.TA_USUARIOS
            WHERE UPPER(LTRIM(RTRIM(SISTEMA))) = @Sistema
              AND UPPER(LTRIM(RTRIM(NOMBRE))) = @Nombre;
            """;

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Sistema", SistemaFijo);
        cmd.Parameters.AddWithValue("@Nombre", nombre.Trim().ToUpperInvariant());
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result) > 0;
    }

    private async Task<bool> PhotoExistsAsync(string nombre, CancellationToken ct)
    {
        var path = await TryResolvePhotoPathAsync(nombre, ct);
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
    }

    private async Task<(bool Exists, string CacheToken)> TryGetPhotoInfoAsync(string nombre, CancellationToken ct)
    {
        var path = await TryResolvePhotoPathAsync(nombre, ct);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return (false, string.Empty);

        return (true, File.GetLastWriteTimeUtc(path).Ticks.ToString());
    }

    private async Task<string?> TryResolvePhotoPathAsync(string nombre, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return null;

        var relativeBase = await TryReadRutaImagenesAsync(ct);
        if (string.IsNullOrWhiteSpace(relativeBase))
            return null;

        var cleanedBase = relativeBase.Trim().TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var basePath = Path.Combine(env.ContentRootPath, cleanedBase);
        return Path.Combine(basePath, "USUARIOS", $"{nombre.Trim()}.jpg");
    }

    private async Task<string> TryReadRutaImagenesAsync(CancellationToken ct)
    {
        await using var cn = new SqlConnection(ConnectionString);
        await cn.OpenAsync(ct);
        var detailColumn = await ResolveDetailColumnAsync(cn, ct);

        var sql = $"""
            SELECT TOP (1)
                ISNULL(VALOR, ''),
                ISNULL({detailColumn}, '')
            FROM dbo.TA_CONFIGURACION
            WHERE UPPER(LTRIM(RTRIM(CLAVE))) = 'RUTAIMAGENES'
            """;

        await using var cmd = new SqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        if (!await rd.ReadAsync(ct))
            return string.Empty;

        return ResolveStoredValue(GetString(rd, 0), GetString(rd, 1));
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

    private static string ResolveStoredValue(string value, string auxValue)
        => !string.IsNullOrWhiteSpace(value) ? value.Trim() : auxValue.Trim();

    private static string GetString(SqlDataReader rd, int index)
        => rd.IsDBNull(index) ? string.Empty : Convert.ToString(rd.GetValue(index)) ?? string.Empty;

    private static bool GetBool(SqlDataReader rd, int index)
        => !rd.IsDBNull(index) && Convert.ToBoolean(rd.GetValue(index));

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

    private async Task ExecuteLoggedAsync(
        string module,
        string action,
        Func<CancellationToken, Task> operation,
        string userMessage,
        CancellationToken ct)
    {
        try
        {
            await operation(ct);
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
