using AlfaCore.Models;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace AlfaCore.Services;

public sealed class InterfacesService(
    IConfiguration configuration,
    ISessionService sessionService,
    IAppEventService appEvents,
    IInterfacesConfigService interfacesConfigService) : IInterfacesService
{
    private readonly IAppEventService _appEvents = appEvents;

    private string ConnectionString => sessionService.GetConnectionString().Length > 0
        ? sessionService.GetConnectionString()
        : configuration.GetConnectionString("AlfaGestion")
          ?? throw new InvalidOperationException("No se configuró la cadena de conexión 'ConnectionStrings:AlfaGestion'.");

    public Task<IReadOnlyList<InterfacesEstadoOptionDto>> GetStatesAsync(CancellationToken ct = default)
        => ExecuteLoggedAsync("Interfaces", "GetStates", async token =>
        {
            const string sql = """
                SELECT
                    IdEstado,
                    ISNULL(Codigo, ''),
                    ISNULL(Descripcion, ''),
                    ISNULL(Orden, 0),
                    ISNULL(Activo, 1),
                    ISNULL(PermiteEdicion, 0),
                    ISNULL(EsInicial, 0),
                    ISNULL(EsFinal, 0),
                    ISNULL(Color, '')
                FROM dbo.INT_ESTADO
                WHERE ISNULL(Activo, 1) = 1
                ORDER BY Orden, Descripcion
                """;

            var items = new List<InterfacesEstadoOptionDto>();
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            await using var rd = await cmd.ExecuteReaderAsync(token);
            while (await rd.ReadAsync(token))
            {
                items.Add(new InterfacesEstadoOptionDto
                {
                    IdEstado = rd.GetInt32(0),
                    Codigo = GetString(rd, 1),
                    Descripcion = GetString(rd, 2),
                    Orden = GetInt(rd, 3),
                    Activo = GetBool(rd, 4),
                    PermiteEdicion = GetBool(rd, 5),
                    EsInicial = GetBool(rd, 6),
                    EsFinal = GetBool(rd, 7),
                    Color = GetString(rd, 8)
                });
            }

            return (IReadOnlyList<InterfacesEstadoOptionDto>)items;
        }, "No se pudieron cargar los estados de Interfaces.", ct);

    public Task<IReadOnlyList<InterfacesTipoDocumentoOptionDto>> GetDocumentTypesAsync(CancellationToken ct = default)
        => ExecuteLoggedAsync("Interfaces", "GetDocumentTypes", async token =>
        {
            const string sql = """
                SELECT
                    IdTipoDocumento,
                    ISNULL(Codigo, ''),
                    ISNULL(Descripcion, ''),
                    ISNULL(Orden, 0),
                    ISNULL(Activo, 1)
                FROM dbo.INT_TIPO_DOCUMENTO
                WHERE ISNULL(Activo, 1) = 1
                ORDER BY Orden, Descripcion
                """;

            var items = new List<InterfacesTipoDocumentoOptionDto>();
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            await using var rd = await cmd.ExecuteReaderAsync(token);
            while (await rd.ReadAsync(token))
            {
                items.Add(new InterfacesTipoDocumentoOptionDto
                {
                    IdTipoDocumento = rd.GetInt32(0),
                    Codigo = GetString(rd, 1),
                    Descripcion = GetString(rd, 2),
                    Orden = GetInt(rd, 3),
                    Activo = GetBool(rd, 4)
                });
            }

            return (IReadOnlyList<InterfacesTipoDocumentoOptionDto>)items;
        }, "No se pudieron cargar los tipos documentales.", ct);

    public Task<IReadOnlyList<InterfacesInboxItemDto>> SearchAsync(InterfacesFilters filters, CancellationToken ct = default)
        => ExecuteLoggedAsync("Interfaces", "Search", async token =>
        {
            filters ??= new InterfacesFilters();
            var sql = $"""
                SELECT TOP ({Math.Max(1, Math.Min(filters.MaxRows, 300))})
                    c.IdComprobanteRecibido,
                    c.FechaHora_Grabacion,
                    ISNULL(c.UsuarioAlta, ''),
                    ISNULL(c.Observacion, ''),
                    ISNULL(c.CantidadAdjuntos, 0),
                    ISNULL(c.Eliminado, 0),
                    e.IdEstado,
                    ISNULL(e.Codigo, ''),
                    ISNULL(e.Descripcion, ''),
                    ISNULL(e.PermiteEdicion, 0),
                    t.IdTipoDocumento,
                    ISNULL(t.Codigo, ''),
                    ISNULL(t.Descripcion, '')
                FROM dbo.INT_COMPROBANTE_RECIBIDO c
                INNER JOIN dbo.INT_ESTADO e
                    ON e.IdEstado = c.IdEstado
                INNER JOIN dbo.INT_TIPO_DOCUMENTO t
                    ON t.IdTipoDocumento = c.IdTipoDocumento
                WHERE (@Desde IS NULL OR c.FechaHora_Grabacion >= @Desde)
                  AND (@Hasta IS NULL OR c.FechaHora_Grabacion < DATEADD(day, 1, @Hasta))
                  AND (@IdEstado IS NULL OR c.IdEstado = @IdEstado)
                  AND (@IdTipoDocumento IS NULL OR c.IdTipoDocumento = @IdTipoDocumento)
                  AND (
                        @Texto = ''
                        OR ISNULL(c.Observacion, '') LIKE '%' + @Texto + '%'
                        OR ISNULL(c.ReferenciaExterna, '') LIKE '%' + @Texto + '%'
                        OR CONVERT(nvarchar(30), c.IdComprobanteRecibido) LIKE '%' + @Texto + '%'
                        OR ISNULL(c.UsuarioAlta, '') LIKE '%' + @Texto + '%'
                      )
                ORDER BY c.FechaHora_Grabacion DESC, c.IdComprobanteRecibido DESC
                """;

            var items = new List<InterfacesInboxItemDto>();
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@Desde", (object?)filters.Desde ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Hasta", (object?)filters.Hasta ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IdEstado", (object?)filters.IdEstado ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IdTipoDocumento", (object?)filters.IdTipoDocumento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Texto", filters.Texto?.Trim() ?? string.Empty);
            await using var rd = await cmd.ExecuteReaderAsync(token);
            while (await rd.ReadAsync(token))
            {
                items.Add(new InterfacesInboxItemDto
                {
                    IdComprobanteRecibido = rd.GetInt64(0),
                    FechaHoraGrabacion = rd.GetDateTime(1),
                    UsuarioAlta = GetString(rd, 2),
                    Observacion = GetString(rd, 3),
                    CantidadAdjuntos = GetInt(rd, 4),
                    Eliminado = GetBool(rd, 5),
                    IdEstado = GetInt(rd, 6),
                    EstadoCodigo = GetString(rd, 7),
                    EstadoDescripcion = GetString(rd, 8),
                    PermiteEdicion = GetBool(rd, 9),
                    IdTipoDocumento = GetInt(rd, 10),
                    TipoDocumentoCodigo = GetString(rd, 11),
                    TipoDocumentoDescripcion = GetString(rd, 12)
                });
            }

            return (IReadOnlyList<InterfacesInboxItemDto>)items;
        }, "No se pudieron cargar los comprobantes recibidos.", ct);

    public Task<InterfacesDetalleDto?> GetByIdAsync(long idComprobanteRecibido, CancellationToken ct = default)
        => ExecuteLoggedAsync("Interfaces", "GetById", async token =>
        {
            const string headerSql = """
                SELECT
                    c.IdComprobanteRecibido,
                    c.FechaHora_Grabacion,
                    c.FechaHora_Modificacion,
                    c.FechaHoraEstado,
                    c.FechaHoraAnulacion,
                    ISNULL(c.UsuarioAlta, ''),
                    ISNULL(c.PcAlta, ''),
                    ISNULL(c.UsuarioModificacion, ''),
                    ISNULL(c.PcModificacion, ''),
                    ISNULL(c.UsuarioAnulacion, ''),
                    ISNULL(c.PcAnulacion, ''),
                    e.IdEstado,
                    ISNULL(e.Codigo, ''),
                    ISNULL(e.Descripcion, ''),
                    ISNULL(e.PermiteEdicion, 0),
                    t.IdTipoDocumento,
                    ISNULL(t.Codigo, ''),
                    ISNULL(t.Descripcion, ''),
                    ISNULL(c.Observacion, ''),
                    ISNULL(c.MotivoAnulacion, ''),
                    ISNULL(c.CantidadAdjuntos, 0),
                    ISNULL(c.RutaBase, ''),
                    ISNULL(c.ReferenciaExterna, ''),
                    ISNULL(c.Eliminado, 0)
                FROM dbo.INT_COMPROBANTE_RECIBIDO c
                INNER JOIN dbo.INT_ESTADO e
                    ON e.IdEstado = c.IdEstado
                INNER JOIN dbo.INT_TIPO_DOCUMENTO t
                    ON t.IdTipoDocumento = c.IdTipoDocumento
                WHERE c.IdComprobanteRecibido = @IdComprobanteRecibido
                """;

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);

            InterfacesDetalleDto? detail = null;
            await using (var cmd = new SqlCommand(headerSql, cn))
            {
                cmd.Parameters.AddWithValue("@IdComprobanteRecibido", idComprobanteRecibido);
                await using var rd = await cmd.ExecuteReaderAsync(token);
                if (await rd.ReadAsync(token))
                {
                    detail = new InterfacesDetalleDto
                    {
                        IdComprobanteRecibido = rd.GetInt64(0),
                        FechaHoraGrabacion = rd.GetDateTime(1),
                        FechaHoraModificacion = rd.IsDBNull(2) ? null : rd.GetDateTime(2),
                        FechaHoraEstado = rd.GetDateTime(3),
                        FechaHoraAnulacion = rd.IsDBNull(4) ? null : rd.GetDateTime(4),
                        UsuarioAlta = GetString(rd, 5),
                        PcAlta = GetString(rd, 6),
                        UsuarioModificacion = GetString(rd, 7),
                        PcModificacion = GetString(rd, 8),
                        UsuarioAnulacion = GetString(rd, 9),
                        PcAnulacion = GetString(rd, 10),
                        IdEstado = GetInt(rd, 11),
                        EstadoCodigo = GetString(rd, 12),
                        EstadoDescripcion = GetString(rd, 13),
                        PermiteEdicion = GetBool(rd, 14),
                        IdTipoDocumento = GetInt(rd, 15),
                        TipoDocumentoCodigo = GetString(rd, 16),
                        TipoDocumentoDescripcion = GetString(rd, 17),
                        Observacion = GetString(rd, 18),
                        MotivoAnulacion = GetString(rd, 19),
                        CantidadAdjuntos = GetInt(rd, 20),
                        RutaBase = GetString(rd, 21),
                        ReferenciaExterna = GetString(rd, 22),
                        Eliminado = GetBool(rd, 23)
                    };
                }
            }

            if (detail is null)
                return null;

            detail.Adjuntos = await GetAttachmentsInternalAsync(cn, idComprobanteRecibido, token);
            detail.Historial = await GetHistoryInternalAsync(cn, idComprobanteRecibido, token);
            return detail;
        }, "No se pudo cargar el comprobante seleccionado.", ct);

    public Task<long> CreateAsync(InterfacesCrearComprobanteRequest request, CancellationToken ct = default)
        => ExecuteLoggedAsync("Interfaces", "Create", async token =>
        {
            ValidateCreateRequest(request);
            var settings = await interfacesConfigService.GetUploadSettingsAsync(token);
            ValidateSettings(settings);

            var initialStateCode = string.IsNullOrWhiteSpace(settings.EstadoInicialCodigo) ? "A_PROCESAR" : settings.EstadoInicialCodigo.Trim();
            var now = DateTime.Now;
            var user = NormalizeActor(request.UsuarioAccion, Environment.UserName, 50);
            var pc = NormalizeActor(request.PcAccion, ResolvePc(), 100);

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);

            var state = await GetStateByCodeAsync(cn, initialStateCode, token)
                ?? throw new InvalidOperationException($"No existe el estado inicial configurado para Interfaces: {initialStateCode}.");
            await EnsureDocumentTypeExistsAsync(cn, request.IdTipoDocumento, token);

            await using var tx = await cn.BeginTransactionAsync(token);
            var savedFiles = new List<string>();
            try
            {
                const string insertSql = """
                    INSERT INTO dbo.INT_COMPROBANTE_RECIBIDO
                    (
                        UsuarioAlta,
                        PcAlta,
                        IdEstado,
                        IdTipoDocumento,
                        Observacion,
                        CantidadAdjuntos,
                        RutaBase,
                        FechaHoraEstado,
                        Eliminado
                    )
                    VALUES
                    (
                        @UsuarioAlta,
                        @PcAlta,
                        @IdEstado,
                        @IdTipoDocumento,
                        @Observacion,
                        0,
                        @RutaBase,
                        GETDATE(),
                        0
                    );

                    SELECT CAST(SCOPE_IDENTITY() AS bigint);
                    """;

                long idComprobante;
                await using (var cmd = new SqlCommand(insertSql, cn, (SqlTransaction)tx))
                {
                    var storedBase = ResolveStoredBase(settings);
                    cmd.Parameters.AddWithValue("@UsuarioAlta", user);
                    cmd.Parameters.AddWithValue("@PcAlta", pc);
                    cmd.Parameters.AddWithValue("@IdEstado", state.IdEstado);
                    cmd.Parameters.AddWithValue("@IdTipoDocumento", request.IdTipoDocumento);
                    cmd.Parameters.AddWithValue("@Observacion", DbNullable(request.Observacion, 1000));
                    cmd.Parameters.AddWithValue("@RutaBase", DbNullable(storedBase, 500));
                    idComprobante = Convert.ToInt64(await cmd.ExecuteScalarAsync(token), CultureInfo.InvariantCulture);
                }

                var relativeFolder = BuildComprobanteFolder(now, idComprobante);
                if (settings.UsaCarpeta)
                    Directory.CreateDirectory(Path.Combine(settings.RutaBase, relativeFolder));

                var order = 1;
                foreach (var attachment in request.Adjuntos)
                {
                    ValidateAttachment(attachment, settings);
                    var extension = NormalizeExtension(Path.GetExtension(attachment.NombreArchivo));
                    var savedName = $"{order:0000}_{Guid.NewGuid():N}{extension}";
                    var relativePath = CombineStoragePath(relativeFolder, savedName);
                    await SaveAttachmentAsync(settings, relativePath, attachment, savedFiles, token);

                    const string insertAttachmentSql = """
                        INSERT INTO dbo.INT_COMPROBANTE_RECIBIDO_ADJUNTO
                        (
                            IdComprobanteRecibido,
                            Orden,
                            NombreOriginal,
                            NombreGuardado,
                            RutaRelativa,
                            Extension,
                            MimeType,
                            TamanoBytes,
                            EsPrincipal,
                            Eliminado,
                            UsuarioAlta,
                            PcAlta
                        )
                        VALUES
                        (
                            @IdComprobanteRecibido,
                            @Orden,
                            @NombreOriginal,
                            @NombreGuardado,
                            @RutaRelativa,
                            @Extension,
                            @MimeType,
                            @TamanoBytes,
                            @EsPrincipal,
                            0,
                            @UsuarioAlta,
                            @PcAlta
                        );
                        """;

                    await using var attachmentCmd = new SqlCommand(insertAttachmentSql, cn, (SqlTransaction)tx);
                    attachmentCmd.Parameters.AddWithValue("@IdComprobanteRecibido", idComprobante);
                    attachmentCmd.Parameters.AddWithValue("@Orden", order);
                    attachmentCmd.Parameters.AddWithValue("@NombreOriginal", Truncate(attachment.NombreArchivo, 255));
                    attachmentCmd.Parameters.AddWithValue("@NombreGuardado", Truncate(savedName, 255));
                    attachmentCmd.Parameters.AddWithValue("@RutaRelativa", Truncate(relativePath, 500));
                    attachmentCmd.Parameters.AddWithValue("@Extension", DbNullable(extension, 20));
                    attachmentCmd.Parameters.AddWithValue("@MimeType", DbNullable(attachment.MimeType, 100));
                    attachmentCmd.Parameters.AddWithValue("@TamanoBytes", attachment.TamanoBytes);
                    attachmentCmd.Parameters.AddWithValue("@EsPrincipal", order == 1);
                    attachmentCmd.Parameters.AddWithValue("@UsuarioAlta", user);
                    attachmentCmd.Parameters.AddWithValue("@PcAlta", pc);
                    await attachmentCmd.ExecuteNonQueryAsync(token);
                    order++;
                }

                await UpdateAttachmentCountAsync(cn, (SqlTransaction)tx, idComprobante, request.Adjuntos.Count, token);
                await InsertHistoryAsync(cn, (SqlTransaction)tx, idComprobante, "ALTA", null, state.IdEstado, user, pc, request.Observacion,
                    new
                    {
                        request.IdTipoDocumento,
                        CantidadAdjuntos = request.Adjuntos.Count
                    }, token);

                await tx.CommitAsync(token);

                await _appEvents.LogAuditAsync(
                    "Interfaces",
                    "Create",
                    "INT_COMPROBANTE_RECIBIDO",
                    idComprobante.ToString(CultureInfo.InvariantCulture),
                    "Comprobante recibido creado.",
                    new
                    {
                        request.IdTipoDocumento,
                        EstadoInicial = state.Codigo,
                        CantidadAdjuntos = request.Adjuntos.Count
                    },
                    token);

                return idComprobante;
            }
            catch
            {
                try
                {
                    await tx.RollbackAsync(token);
                }
                catch
                {
                }

                await CleanupSavedFilesAsync(settings, savedFiles, token);

                throw;
            }
        }, "No se pudo registrar el comprobante recibido.", ct);

    public Task UpdateAsync(InterfacesActualizarComprobanteRequest request, CancellationToken ct = default)
        => ExecuteLoggedAsync("Interfaces", "Update", async token =>
        {
            if (request.IdComprobanteRecibido <= 0)
                throw new InvalidOperationException("El comprobante es obligatorio.");
            if (request.IdTipoDocumento <= 0)
                throw new InvalidOperationException("El tipo documental es obligatorio.");

            var user = NormalizeActor(request.UsuarioAccion, Environment.UserName, 50);
            var pc = NormalizeActor(request.PcAccion, ResolvePc(), 100);

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            var current = await RequireEditableComprobanteAsync(request.IdComprobanteRecibido, token);
            await EnsureDocumentTypeExistsAsync(cn, request.IdTipoDocumento, token);

            await using var tx = await cn.BeginTransactionAsync(token);
            const string sql = """
                UPDATE dbo.INT_COMPROBANTE_RECIBIDO
                SET
                    IdTipoDocumento = @IdTipoDocumento,
                    Observacion = @Observacion,
                    FechaHora_Modificacion = GETDATE(),
                    UsuarioModificacion = @UsuarioModificacion,
                    PcModificacion = @PcModificacion
                WHERE IdComprobanteRecibido = @IdComprobanteRecibido
                """;

            await using (var cmd = new SqlCommand(sql, cn, (SqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@IdTipoDocumento", request.IdTipoDocumento);
                cmd.Parameters.AddWithValue("@Observacion", DbNullable(request.Observacion, 1000));
                cmd.Parameters.AddWithValue("@UsuarioModificacion", DbNullable(user, 50));
                cmd.Parameters.AddWithValue("@PcModificacion", DbNullable(pc, 100));
                cmd.Parameters.AddWithValue("@IdComprobanteRecibido", request.IdComprobanteRecibido);
                await cmd.ExecuteNonQueryAsync(token);
            }

            await InsertHistoryAsync(cn, (SqlTransaction)tx, request.IdComprobanteRecibido, "MODIFICACION", current.IdEstado, current.IdEstado, user, pc, request.Observacion,
                new
                {
                    TipoAnterior = current.IdTipoDocumento,
                    TipoNuevo = request.IdTipoDocumento
                }, token);

            await tx.CommitAsync(token);

            await _appEvents.LogAuditAsync(
                "Interfaces",
                "Update",
                "INT_COMPROBANTE_RECIBIDO",
                request.IdComprobanteRecibido.ToString(CultureInfo.InvariantCulture),
                "Comprobante recibido actualizado.",
                new
                {
                    TipoAnterior = current.IdTipoDocumento,
                    TipoNuevo = request.IdTipoDocumento
                },
                token);
        }, "No se pudo actualizar el comprobante.", ct);

    public Task AddAttachmentsAsync(InterfacesAgregarAdjuntosRequest request, CancellationToken ct = default)
        => ExecuteLoggedAsync("Interfaces", "AddAttachments", async token =>
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.IdComprobanteRecibido <= 0)
                throw new InvalidOperationException("El comprobante es obligatorio.");
            if (request.Adjuntos is null || request.Adjuntos.Count == 0)
                throw new InvalidOperationException("Debés seleccionar al menos un archivo.");

            var settings = await interfacesConfigService.GetUploadSettingsAsync(token);
            ValidateSettings(settings);
            var user = NormalizeActor(request.UsuarioAccion, Environment.UserName, 50);
            var pc = NormalizeActor(request.PcAccion, ResolvePc(), 100);
            var current = await RequireEditableComprobanteAsync(request.IdComprobanteRecibido, token);

            var relativeFolder = BuildComprobanteFolder(current.FechaHoraGrabacion, request.IdComprobanteRecibido);
            if (settings.UsaCarpeta)
                Directory.CreateDirectory(Path.Combine(settings.RutaBase, relativeFolder));

            var nextOrder = current.Adjuntos.Count == 0 ? 1 : current.Adjuntos.Max(x => x.Orden) + 1;
            var savedFiles = new List<string>();

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var tx = await cn.BeginTransactionAsync(token);
            try
            {
                foreach (var attachment in request.Adjuntos)
                {
                    ValidateAttachment(attachment, settings);
                    var extension = NormalizeExtension(Path.GetExtension(attachment.NombreArchivo));
                    var savedName = $"{nextOrder:0000}_{Guid.NewGuid():N}{extension}";
                    var relativePath = CombineStoragePath(relativeFolder, savedName);
                    await SaveAttachmentAsync(settings, relativePath, attachment, savedFiles, token);

                    const string insertAttachmentSql = """
                        INSERT INTO dbo.INT_COMPROBANTE_RECIBIDO_ADJUNTO
                        (
                            IdComprobanteRecibido,
                            Orden,
                            NombreOriginal,
                            NombreGuardado,
                            RutaRelativa,
                            Extension,
                            MimeType,
                            TamanoBytes,
                            EsPrincipal,
                            Eliminado,
                            UsuarioAlta,
                            PcAlta
                        )
                        VALUES
                        (
                            @IdComprobanteRecibido,
                            @Orden,
                            @NombreOriginal,
                            @NombreGuardado,
                            @RutaRelativa,
                            @Extension,
                            @MimeType,
                            @TamanoBytes,
                            0,
                            0,
                            @UsuarioAlta,
                            @PcAlta
                        )
                        """;

                    await using var attachmentCmd = new SqlCommand(insertAttachmentSql, cn, (SqlTransaction)tx);
                    attachmentCmd.Parameters.AddWithValue("@IdComprobanteRecibido", request.IdComprobanteRecibido);
                    attachmentCmd.Parameters.AddWithValue("@Orden", nextOrder);
                    attachmentCmd.Parameters.AddWithValue("@NombreOriginal", Truncate(attachment.NombreArchivo, 255));
                    attachmentCmd.Parameters.AddWithValue("@NombreGuardado", Truncate(savedName, 255));
                    attachmentCmd.Parameters.AddWithValue("@RutaRelativa", Truncate(relativePath, 500));
                    attachmentCmd.Parameters.AddWithValue("@Extension", DbNullable(extension, 20));
                    attachmentCmd.Parameters.AddWithValue("@MimeType", DbNullable(attachment.MimeType, 100));
                    attachmentCmd.Parameters.AddWithValue("@TamanoBytes", attachment.TamanoBytes);
                    attachmentCmd.Parameters.AddWithValue("@UsuarioAlta", user);
                    attachmentCmd.Parameters.AddWithValue("@PcAlta", pc);
                    await attachmentCmd.ExecuteNonQueryAsync(token);
                    nextOrder++;
                }

                await RecalculateAttachmentCountAsync(cn, (SqlTransaction)tx, request.IdComprobanteRecibido, user, pc, token);
                await InsertHistoryAsync(cn, (SqlTransaction)tx, request.IdComprobanteRecibido, "ADJUNTO_ALTA", current.IdEstado, current.IdEstado, user, pc,
                    $"Se agregaron {request.Adjuntos.Count} adjunto(s).", new { Cantidad = request.Adjuntos.Count }, token);

                await tx.CommitAsync(token);

                await _appEvents.LogAuditAsync(
                    "Interfaces",
                    "AddAttachments",
                    "INT_COMPROBANTE_RECIBIDO",
                    request.IdComprobanteRecibido.ToString(CultureInfo.InvariantCulture),
                    "Se agregaron adjuntos al comprobante.",
                    new { Cantidad = request.Adjuntos.Count },
                    token);
            }
            catch
            {
                try
                {
                    await tx.RollbackAsync(token);
                }
                catch
                {
                }

                await CleanupSavedFilesAsync(settings, savedFiles, token);

                throw;
            }
        }, "No se pudieron agregar adjuntos al comprobante.", ct);

    public Task RemoveAttachmentAsync(InterfacesEliminarAdjuntoRequest request, CancellationToken ct = default)
        => ExecuteLoggedAsync("Interfaces", "RemoveAttachment", async token =>
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.IdAdjunto <= 0)
                throw new InvalidOperationException("El adjunto es obligatorio.");

            var user = NormalizeActor(request.UsuarioAccion, Environment.UserName, 50);
            var pc = NormalizeActor(request.PcAccion, ResolvePc(), 100);

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            var attachment = await GetAttachmentByIdAsync(cn, request.IdAdjunto, token)
                ?? throw new InvalidOperationException("El adjunto seleccionado no existe.");
            var current = await RequireEditableComprobanteAsync(attachment.IdComprobanteRecibido, token);

            await using var tx = await cn.BeginTransactionAsync(token);
            const string sql = """
                UPDATE dbo.INT_COMPROBANTE_RECIBIDO_ADJUNTO
                SET
                    Eliminado = 1,
                    FechaHora_Modificacion = GETDATE(),
                    UsuarioModificacion = @UsuarioModificacion,
                    PcModificacion = @PcModificacion
                WHERE IdAdjunto = @IdAdjunto
                """;

            await using (var cmd = new SqlCommand(sql, cn, (SqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@UsuarioModificacion", DbNullable(user, 50));
                cmd.Parameters.AddWithValue("@PcModificacion", DbNullable(pc, 100));
                cmd.Parameters.AddWithValue("@IdAdjunto", request.IdAdjunto);
                await cmd.ExecuteNonQueryAsync(token);
            }

            await RecalculateAttachmentCountAsync(cn, (SqlTransaction)tx, attachment.IdComprobanteRecibido, user, pc, token);
            await InsertHistoryAsync(cn, (SqlTransaction)tx, attachment.IdComprobanteRecibido, "ADJUNTO_BAJA", current.IdEstado, current.IdEstado, user, pc,
                request.Observacion, new { request.IdAdjunto, attachment.NombreOriginal }, token);

            await tx.CommitAsync(token);

            await _appEvents.LogAuditAsync(
                "Interfaces",
                "RemoveAttachment",
                "INT_COMPROBANTE_RECIBIDO",
                attachment.IdComprobanteRecibido.ToString(CultureInfo.InvariantCulture),
                "Adjunto dado de baja lógicamente.",
                new { request.IdAdjunto, attachment.NombreOriginal },
                token);
        }, "No se pudo quitar el adjunto.", ct);

    public Task ChangeStatusAsync(InterfacesCambioEstadoRequest request, CancellationToken ct = default)
        => ExecuteLoggedAsync("Interfaces", "ChangeStatus", async token =>
        {
            if (request.IdComprobanteRecibido <= 0)
                throw new InvalidOperationException("El comprobante es obligatorio.");
            if (request.IdEstadoNuevo <= 0)
                throw new InvalidOperationException("El estado destino es obligatorio.");

            var user = NormalizeActor(request.UsuarioAccion, Environment.UserName, 50);
            var pc = NormalizeActor(request.PcAccion, ResolvePc(), 100);

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            var detail = await GetByIdAsync(request.IdComprobanteRecibido, token)
                ?? throw new InvalidOperationException("El comprobante indicado no existe.");

            var newState = await GetStateByIdAsync(cn, request.IdEstadoNuevo, token)
                ?? throw new InvalidOperationException("El estado seleccionado no existe.");

            if (detail.IdEstado == newState.IdEstado)
                return;

            await using var tx = await cn.BeginTransactionAsync(token);
            const string updateSql = """
                UPDATE dbo.INT_COMPROBANTE_RECIBIDO
                SET
                    IdEstado = @IdEstado,
                    FechaHoraEstado = GETDATE(),
                    FechaHora_Modificacion = GETDATE(),
                    UsuarioModificacion = @UsuarioModificacion,
                    PcModificacion = @PcModificacion,
                    Eliminado = @Eliminado,
                    FechaHoraAnulacion = @FechaHoraAnulacion,
                    UsuarioAnulacion = @UsuarioAnulacion,
                    PcAnulacion = @PcAnulacion,
                    MotivoAnulacion = @MotivoAnulacion
                WHERE IdComprobanteRecibido = @IdComprobanteRecibido
                """;

            var isAnulado = string.Equals(newState.Codigo, "ANULADO", StringComparison.OrdinalIgnoreCase);

            await using (var cmd = new SqlCommand(updateSql, cn, (SqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@IdEstado", newState.IdEstado);
                cmd.Parameters.AddWithValue("@UsuarioModificacion", DbNullable(user, 50));
                cmd.Parameters.AddWithValue("@PcModificacion", DbNullable(pc, 100));
                cmd.Parameters.AddWithValue("@Eliminado", isAnulado);
                cmd.Parameters.AddWithValue("@FechaHoraAnulacion", isAnulado ? DateTime.Now : DBNull.Value);
                cmd.Parameters.AddWithValue("@UsuarioAnulacion", isAnulado ? DbNullable(user, 50) : DBNull.Value);
                cmd.Parameters.AddWithValue("@PcAnulacion", isAnulado ? DbNullable(pc, 100) : DBNull.Value);
                cmd.Parameters.AddWithValue("@MotivoAnulacion", isAnulado ? DbNullable(request.Observacion, 500) : DBNull.Value);
                cmd.Parameters.AddWithValue("@IdComprobanteRecibido", request.IdComprobanteRecibido);
                await cmd.ExecuteNonQueryAsync(token);
            }

            await InsertHistoryAsync(
                cn,
                (SqlTransaction)tx,
                request.IdComprobanteRecibido,
                isAnulado ? "ANULACION" : "CAMBIO_ESTADO",
                detail.IdEstado,
                newState.IdEstado,
                user,
                pc,
                request.Observacion,
                new
                {
                    EstadoAnterior = detail.EstadoCodigo,
                    EstadoNuevo = newState.Codigo
                },
                token);

            await tx.CommitAsync(token);

            await _appEvents.LogAuditAsync(
                "Interfaces",
                "ChangeStatus",
                "INT_COMPROBANTE_RECIBIDO",
                request.IdComprobanteRecibido.ToString(CultureInfo.InvariantCulture),
                isAnulado ? "Comprobante anulado." : "Estado de comprobante actualizado.",
                new
                {
                    EstadoAnterior = detail.EstadoCodigo,
                    EstadoNuevo = newState.Codigo
                },
                token);
        }, "No se pudo actualizar el estado del comprobante.", ct);

    public Task<InterfacesAdjuntoServeDto?> GetAttachmentForServeAsync(long idAdjunto, CancellationToken ct = default)
        => ExecuteLoggedAsync("Interfaces", "GetAttachmentForServe", async token =>
        {
            const string sql = """
                SELECT
                    ISNULL(c.RutaBase, ''),
                    ISNULL(a.RutaRelativa, ''),
                    ISNULL(a.MimeType, ''),
                    ISNULL(a.NombreOriginal, '')
                FROM dbo.INT_COMPROBANTE_RECIBIDO_ADJUNTO a
                INNER JOIN dbo.INT_COMPROBANTE_RECIBIDO c
                    ON c.IdComprobanteRecibido = a.IdComprobanteRecibido
                WHERE a.IdAdjunto = @IdAdjunto
                  AND ISNULL(a.Eliminado, 0) = 0
                """;

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@IdAdjunto", idAdjunto);
            await using var rd = await cmd.ExecuteReaderAsync(token);
            if (!await rd.ReadAsync(token))
                return null;

            var rutaBase = GetString(rd, 0);
            var rutaRelativa = GetString(rd, 1);
            if (string.IsNullOrWhiteSpace(rutaBase) || string.IsNullOrWhiteSpace(rutaRelativa))
                return null;

            return new InterfacesAdjuntoServeDto
            {
                RutaCompleta = BuildStoredFileReference(rutaBase, rutaRelativa),
                MimeType = GetString(rd, 2),
                NombreArchivo = GetString(rd, 3)
            };
        }, "No se pudo obtener el adjunto solicitado.", ct);

    private static async Task UpdateAttachmentCountAsync(SqlConnection cn, SqlTransaction tx, long idComprobante, int count, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.INT_COMPROBANTE_RECIBIDO
            SET CantidadAdjuntos = @CantidadAdjuntos
            WHERE IdComprobanteRecibido = @IdComprobanteRecibido
            """;

        await using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.AddWithValue("@CantidadAdjuntos", count);
        cmd.Parameters.AddWithValue("@IdComprobanteRecibido", idComprobante);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task RecalculateAttachmentCountAsync(SqlConnection cn, SqlTransaction tx, long idComprobante, string usuario, string pc, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.INT_COMPROBANTE_RECIBIDO
            SET
                CantidadAdjuntos =
                (
                    SELECT COUNT(*)
                    FROM dbo.INT_COMPROBANTE_RECIBIDO_ADJUNTO
                    WHERE IdComprobanteRecibido = @IdComprobanteRecibido
                      AND ISNULL(Eliminado, 0) = 0
                ),
                FechaHora_Modificacion = GETDATE(),
                UsuarioModificacion = @UsuarioModificacion,
                PcModificacion = @PcModificacion
            WHERE IdComprobanteRecibido = @IdComprobanteRecibido
            """;

        await using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.AddWithValue("@IdComprobanteRecibido", idComprobante);
        cmd.Parameters.AddWithValue("@UsuarioModificacion", DbNullable(usuario, 50));
        cmd.Parameters.AddWithValue("@PcModificacion", DbNullable(pc, 100));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task EnsureDocumentTypeExistsAsync(SqlConnection cn, int idTipoDocumento, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (1) 1
            FROM dbo.INT_TIPO_DOCUMENTO
            WHERE IdTipoDocumento = @IdTipoDocumento
              AND ISNULL(Activo, 1) = 1
            """;

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@IdTipoDocumento", idTipoDocumento);
        var exists = await cmd.ExecuteScalarAsync(ct);
        if (exists is null)
            throw new InvalidOperationException("El tipo documental seleccionado no existe o está inactivo.");
    }

    private static async Task<IReadOnlyList<InterfacesAdjuntoDto>> GetAttachmentsInternalAsync(SqlConnection cn, long idComprobanteRecibido, CancellationToken ct)
    {
        const string sql = """
            SELECT
                IdAdjunto,
                IdComprobanteRecibido,
                ISNULL(Orden, 0),
                ISNULL(NombreOriginal, ''),
                ISNULL(NombreGuardado, ''),
                ISNULL(RutaRelativa, ''),
                ISNULL(Extension, ''),
                ISNULL(MimeType, ''),
                ISNULL(TamanoBytes, 0),
                ISNULL(EsPrincipal, 0),
                ISNULL(Eliminado, 0),
                FechaHora_Grabacion
            FROM dbo.INT_COMPROBANTE_RECIBIDO_ADJUNTO
            WHERE IdComprobanteRecibido = @IdComprobanteRecibido
              AND ISNULL(Eliminado, 0) = 0
            ORDER BY Orden, IdAdjunto
            """;

        var items = new List<InterfacesAdjuntoDto>();
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@IdComprobanteRecibido", idComprobanteRecibido);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            items.Add(new InterfacesAdjuntoDto
            {
                IdAdjunto = rd.GetInt64(0),
                IdComprobanteRecibido = rd.GetInt64(1),
                Orden = GetInt(rd, 2),
                NombreOriginal = GetString(rd, 3),
                NombreGuardado = GetString(rd, 4),
                RutaRelativa = GetString(rd, 5),
                Extension = GetString(rd, 6),
                MimeType = GetString(rd, 7),
                TamanoBytes = GetLong(rd, 8),
                EsPrincipal = GetBool(rd, 9),
                Eliminado = GetBool(rd, 10),
                FechaHoraGrabacion = rd.GetDateTime(11)
            });
        }

        return items;
    }

    private static async Task<InterfacesAdjuntoDto?> GetAttachmentByIdAsync(SqlConnection cn, long idAdjunto, CancellationToken ct)
    {
        const string sql = """
            SELECT
                IdAdjunto,
                IdComprobanteRecibido,
                ISNULL(Orden, 0),
                ISNULL(NombreOriginal, ''),
                ISNULL(NombreGuardado, ''),
                ISNULL(RutaRelativa, ''),
                ISNULL(Extension, ''),
                ISNULL(MimeType, ''),
                ISNULL(TamanoBytes, 0),
                ISNULL(EsPrincipal, 0),
                ISNULL(Eliminado, 0),
                FechaHora_Grabacion
            FROM dbo.INT_COMPROBANTE_RECIBIDO_ADJUNTO
            WHERE IdAdjunto = @IdAdjunto
            """;

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@IdAdjunto", idAdjunto);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        if (!await rd.ReadAsync(ct))
            return null;

        return new InterfacesAdjuntoDto
        {
            IdAdjunto = rd.GetInt64(0),
            IdComprobanteRecibido = rd.GetInt64(1),
            Orden = GetInt(rd, 2),
            NombreOriginal = GetString(rd, 3),
            NombreGuardado = GetString(rd, 4),
            RutaRelativa = GetString(rd, 5),
            Extension = GetString(rd, 6),
            MimeType = GetString(rd, 7),
            TamanoBytes = GetLong(rd, 8),
            EsPrincipal = GetBool(rd, 9),
            Eliminado = GetBool(rd, 10),
            FechaHoraGrabacion = rd.GetDateTime(11)
        };
    }

    private static async Task<IReadOnlyList<InterfacesHistorialDto>> GetHistoryInternalAsync(SqlConnection cn, long idComprobanteRecibido, CancellationToken ct)
    {
        const string sql = """
            SELECT
                h.IdHistorial,
                h.FechaHora,
                ISNULL(h.Usuario, ''),
                ISNULL(h.Pc, ''),
                ISNULL(h.Accion, ''),
                h.IdEstadoAnterior,
                ISNULL(ea.Descripcion, ''),
                h.IdEstadoNuevo,
                ISNULL(en.Descripcion, ''),
                ISNULL(h.Observacion, ''),
                ISNULL(h.DataJson, '')
            FROM dbo.INT_COMPROBANTE_RECIBIDO_HIST h
            LEFT JOIN dbo.INT_ESTADO ea
                ON ea.IdEstado = h.IdEstadoAnterior
            LEFT JOIN dbo.INT_ESTADO en
                ON en.IdEstado = h.IdEstadoNuevo
            WHERE h.IdComprobanteRecibido = @IdComprobanteRecibido
            ORDER BY h.FechaHora DESC, h.IdHistorial DESC
            """;

        var items = new List<InterfacesHistorialDto>();
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@IdComprobanteRecibido", idComprobanteRecibido);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            items.Add(new InterfacesHistorialDto
            {
                IdHistorial = rd.GetInt64(0),
                FechaHora = rd.GetDateTime(1),
                Usuario = GetString(rd, 2),
                Pc = GetString(rd, 3),
                Accion = GetString(rd, 4),
                IdEstadoAnterior = rd.IsDBNull(5) ? null : rd.GetInt32(5),
                EstadoAnteriorDescripcion = GetString(rd, 6),
                IdEstadoNuevo = rd.IsDBNull(7) ? null : rd.GetInt32(7),
                EstadoNuevoDescripcion = GetString(rd, 8),
                Observacion = GetString(rd, 9),
                DataJson = GetString(rd, 10)
            });
        }

        return items;
    }

    private static async Task InsertHistoryAsync(
        SqlConnection cn,
        SqlTransaction tx,
        long idComprobanteRecibido,
        string accion,
        int? idEstadoAnterior,
        int? idEstadoNuevo,
        string usuario,
        string pc,
        string observacion,
        object? data,
        CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.INT_COMPROBANTE_RECIBIDO_HIST
            (
                IdComprobanteRecibido,
                Usuario,
                Pc,
                Accion,
                IdEstadoAnterior,
                IdEstadoNuevo,
                Observacion,
                DataJson
            )
            VALUES
            (
                @IdComprobanteRecibido,
                @Usuario,
                @Pc,
                @Accion,
                @IdEstadoAnterior,
                @IdEstadoNuevo,
                @Observacion,
                @DataJson
            )
            """;

        await using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.AddWithValue("@IdComprobanteRecibido", idComprobanteRecibido);
        cmd.Parameters.AddWithValue("@Usuario", DbNullable(usuario, 50));
        cmd.Parameters.AddWithValue("@Pc", DbNullable(pc, 100));
        cmd.Parameters.AddWithValue("@Accion", Truncate(accion, 30));
        cmd.Parameters.AddWithValue("@IdEstadoAnterior", (object?)idEstadoAnterior ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IdEstadoNuevo", (object?)idEstadoNuevo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Observacion", DbNullable(observacion, 1000));
        cmd.Parameters.AddWithValue("@DataJson", DbNullable(SerializeData(data), 4000));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<InterfacesEstadoOptionDto?> GetStateByIdAsync(SqlConnection cn, int idEstado, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (1)
                IdEstado,
                ISNULL(Codigo, ''),
                ISNULL(Descripcion, ''),
                ISNULL(Orden, 0),
                ISNULL(Activo, 1),
                ISNULL(PermiteEdicion, 0),
                ISNULL(EsInicial, 0),
                ISNULL(EsFinal, 0),
                ISNULL(Color, '')
            FROM dbo.INT_ESTADO
            WHERE IdEstado = @IdEstado
            """;

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@IdEstado", idEstado);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        if (!await rd.ReadAsync(ct))
            return null;

        return new InterfacesEstadoOptionDto
        {
            IdEstado = rd.GetInt32(0),
            Codigo = GetString(rd, 1),
            Descripcion = GetString(rd, 2),
            Orden = GetInt(rd, 3),
            Activo = GetBool(rd, 4),
            PermiteEdicion = GetBool(rd, 5),
            EsInicial = GetBool(rd, 6),
            EsFinal = GetBool(rd, 7),
            Color = GetString(rd, 8)
        };
    }

    private static async Task<InterfacesEstadoOptionDto?> GetStateByCodeAsync(SqlConnection cn, string codigoEstado, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (1)
                IdEstado,
                ISNULL(Codigo, ''),
                ISNULL(Descripcion, ''),
                ISNULL(Orden, 0),
                ISNULL(Activo, 1),
                ISNULL(PermiteEdicion, 0),
                ISNULL(EsInicial, 0),
                ISNULL(EsFinal, 0),
                ISNULL(Color, '')
            FROM dbo.INT_ESTADO
            WHERE UPPER(LTRIM(RTRIM(Codigo))) = UPPER(LTRIM(RTRIM(@Codigo)))
            """;

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Codigo", codigoEstado.Trim());
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        if (!await rd.ReadAsync(ct))
            return null;

        return new InterfacesEstadoOptionDto
        {
            IdEstado = rd.GetInt32(0),
            Codigo = GetString(rd, 1),
            Descripcion = GetString(rd, 2),
            Orden = GetInt(rd, 3),
            Activo = GetBool(rd, 4),
            PermiteEdicion = GetBool(rd, 5),
            EsInicial = GetBool(rd, 6),
            EsFinal = GetBool(rd, 7),
            Color = GetString(rd, 8)
        };
    }

    private static void ValidateCreateRequest(InterfacesCrearComprobanteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.IdTipoDocumento <= 0)
            throw new InvalidOperationException("El tipo documental es obligatorio.");
        if (request.Adjuntos is null || request.Adjuntos.Count == 0)
            throw new InvalidOperationException("Debés adjuntar al menos un archivo.");
    }

    private static void ValidateSettings(InterfacesUploadSettingsDto settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(settings.RutaBase))
            throw new InvalidOperationException("La ruta base o carpeta remota para documentos no está configurada.");
        if (settings.UsaFtp)
        {
            if (string.IsNullOrWhiteSpace(settings.FtpHost))
                throw new InvalidOperationException("El host FTP no está configurado.");
            if (string.IsNullOrWhiteSpace(settings.FtpUsuario))
                throw new InvalidOperationException("El usuario FTP no está configurado.");
        }
    }

    private static void ValidateAttachment(InterfacesCrearAdjuntoRequest attachment, InterfacesUploadSettingsDto settings)
    {
        if (attachment is null)
            throw new InvalidOperationException("Se recibió un adjunto inválido.");
        if (string.IsNullOrWhiteSpace(attachment.NombreArchivo))
            throw new InvalidOperationException("El nombre del archivo es obligatorio.");
        if (attachment.TamanoBytes <= 0)
            throw new InvalidOperationException("Uno de los archivos está vacío.");
        if (attachment.TamanoBytes > settings.TamanoMaximoBytes)
            throw new InvalidOperationException($"Uno de los archivos supera el máximo permitido de {settings.TamanoMaximoMb} MB.");

        var extension = NormalizeExtension(Path.GetExtension(attachment.NombreArchivo));
        if (settings.ExtensionesPermitidas.Count > 0
            && !settings.ExtensionesPermitidas.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"La extensión {extension} no está permitida para Interfaces.");
        }
    }

    private async Task<InterfacesDetalleDto> RequireEditableComprobanteAsync(long idComprobanteRecibido, CancellationToken ct)
    {
        var current = await GetByIdAsync(idComprobanteRecibido, ct)
            ?? throw new InvalidOperationException("El comprobante indicado no existe.");

        if (!current.PermiteEdicion)
            throw new InvalidOperationException("El comprobante seleccionado ya no permite edición por su estado actual.");

        return current;
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
            var incidentId = await _appEvents.LogErrorAsync(module, action, ex, userMessage, null, AppEventSeverity.Error, ct);
            throw new AppUserFacingException("El esquema del módulo Interfaces no está disponible en la base activa.", incidentId, ex);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var incidentId = await _appEvents.LogErrorAsync(module, action, ex, userMessage, null, AppEventSeverity.Error, ct);
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
        catch (SqlException ex) when (ex.Number == 208)
        {
            var incidentId = await _appEvents.LogErrorAsync(module, action, ex, userMessage, null, AppEventSeverity.Error, ct);
            throw new AppUserFacingException("El esquema del módulo Interfaces no está disponible en la base activa.", incidentId, ex);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var incidentId = await _appEvents.LogErrorAsync(module, action, ex, userMessage, null, AppEventSeverity.Error, ct);
            throw new AppUserFacingException(userMessage, incidentId, ex);
        }
    }

    private static string ResolvePc() => Environment.MachineName;

    private static string NormalizeActor(string? value, string fallback, int maxLength)
    {
        var resolved = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return Truncate(resolved, maxLength);
    }

    private static string NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return string.Empty;

        return extension.Trim().StartsWith('.')
            ? extension.Trim().ToLowerInvariant()
            : "." + extension.Trim().ToLowerInvariant();
    }

    private static string ResolveStoredBase(InterfacesUploadSettingsDto settings)
        => settings.UsaFtp ? settings.BuildFtpBaseUrl().TrimEnd('/') : settings.RutaBase;

    private static string CombineStoragePath(string folder, string fileName)
        => Path.Combine(folder, fileName);

    private static string BuildStoredFileReference(string rutaBase, string rutaRelativa)
    {
        if (rutaBase.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
        {
            var relative = rutaRelativa.Replace('\\', '/').TrimStart('/');
            return rutaBase.TrimEnd('/') + "/" + relative;
        }

        return Path.Combine(rutaBase, rutaRelativa);
    }

    private static string BuildComprobanteFolder(DateTime fechaHoraGrabacion, long idComprobanteRecibido)
        => Path.Combine(
            fechaHoraGrabacion.ToString("yyyy_MM", CultureInfo.InvariantCulture),
            idComprobanteRecibido.ToString(CultureInfo.InvariantCulture));

    private static async Task SaveAttachmentAsync(
        InterfacesUploadSettingsDto settings,
        string relativePath,
        InterfacesCrearAdjuntoRequest attachment,
        List<string> savedFiles,
        CancellationToken ct)
    {
        if (settings.UsaFtp)
        {
            var remoteUrl = settings.BuildFtpBaseUrl().TrimEnd('/') + "/" + relativePath.Replace('\\', '/').TrimStart('/');
            await EnsureFtpDirectoriesAsync(settings, relativePath, ct);
            await UploadToFtpAsync(settings, remoteUrl, attachment.Contenido, ct);
            savedFiles.Add(remoteUrl);
            return;
        }

        var absolutePath = Path.Combine(settings.RutaBase, relativePath);
        await using var fs = File.Create(absolutePath);
        await attachment.Contenido.CopyToAsync(fs, ct);
        savedFiles.Add(absolutePath);
    }

    private static async Task CleanupSavedFilesAsync(InterfacesUploadSettingsDto settings, IReadOnlyList<string> savedFiles, CancellationToken ct)
    {
        foreach (var path in savedFiles)
        {
            try
            {
                if (settings.UsaFtp)
                    await DeleteFtpFileIfExistsAsync(settings, path, ct);
                else if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }
    }

#pragma warning disable SYSLIB0014
    private static async Task EnsureFtpDirectoriesAsync(InterfacesUploadSettingsDto settings, string relativePath, CancellationToken ct)
    {
        var parts = relativePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 1)
            return;

        var current = settings.BuildFtpBaseUrl().TrimEnd('/');
        for (var i = 0; i < parts.Length - 1; i++)
        {
            current += "/" + parts[i];
            var request = (FtpWebRequest)WebRequest.Create(current);
            request.Method = WebRequestMethods.Ftp.MakeDirectory;
            request.Credentials = new NetworkCredential(settings.FtpUsuario, settings.FtpClave);
            request.UseBinary = true;
            request.UsePassive = settings.FtpModoPasivo;
            request.KeepAlive = false;
            try
            {
                using var response = (FtpWebResponse)await request.GetResponseAsync();
            }
            catch (WebException ex)
            {
                if (ex.Response is FtpWebResponse ftpResponse)
                {
                    if (ftpResponse.StatusCode is FtpStatusCode.ActionNotTakenFileUnavailable or FtpStatusCode.ActionNotTakenFilenameNotAllowed)
                        continue;
                }
            }
        }
    }

    private static async Task UploadToFtpAsync(InterfacesUploadSettingsDto settings, string remoteUrl, Stream content, CancellationToken ct)
    {
        var request = (FtpWebRequest)WebRequest.Create(remoteUrl);
        request.Method = WebRequestMethods.Ftp.UploadFile;
        request.Credentials = new NetworkCredential(settings.FtpUsuario, settings.FtpClave);
        request.UseBinary = true;
        request.UsePassive = settings.FtpModoPasivo;
        request.KeepAlive = false;

        await using (var requestStream = await request.GetRequestStreamAsync())
        {
            if (content.CanSeek)
                content.Position = 0;
            await content.CopyToAsync(requestStream, ct);
        }

        using var response = (FtpWebResponse)await request.GetResponseAsync();
    }

    private static async Task DeleteFtpFileIfExistsAsync(InterfacesUploadSettingsDto settings, string remoteUrl, CancellationToken ct)
    {
        var request = (FtpWebRequest)WebRequest.Create(remoteUrl);
        request.Method = WebRequestMethods.Ftp.DeleteFile;
        request.Credentials = new NetworkCredential(settings.FtpUsuario, settings.FtpClave);
        request.UseBinary = true;
        request.UsePassive = settings.FtpModoPasivo;
        request.KeepAlive = false;

        try
        {
            using var response = (FtpWebResponse)await request.GetResponseAsync();
        }
        catch (WebException)
        {
        }
    }
#pragma warning restore SYSLIB0014

    private static object DbNullable(string? value, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : Truncate(value.Trim(), maxLength);

    private static string SerializeData(object? data)
    {
        if (data is null)
            return string.Empty;

        return JsonSerializer.Serialize(data);
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Length <= maxLength ? value.Trim() : value.Trim()[..maxLength];
    }

    private static string GetString(SqlDataReader rd, int index)
        => rd.IsDBNull(index) ? string.Empty : Convert.ToString(rd.GetValue(index)) ?? string.Empty;

    private static int GetInt(SqlDataReader rd, int index)
        => rd.IsDBNull(index) ? 0 : Convert.ToInt32(rd.GetValue(index), CultureInfo.InvariantCulture);

    private static long GetLong(SqlDataReader rd, int index)
        => rd.IsDBNull(index) ? 0 : Convert.ToInt64(rd.GetValue(index), CultureInfo.InvariantCulture);

    private static bool GetBool(SqlDataReader rd, int index)
        => !rd.IsDBNull(index) && Convert.ToBoolean(rd.GetValue(index), CultureInfo.InvariantCulture);
}
