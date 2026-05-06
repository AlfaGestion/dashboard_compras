using AlfaCore.Models;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AlfaCore.Services;

public sealed class ConversacionesService(
    IConfiguration configuration,
    ISessionService sessionService,
    IAppEventService appEvents,
    IHttpClientFactory httpClientFactory,
    IConversacionesConfigService conversacionesConfigService,
    IWebHostEnvironment environment) : IConversacionesService
{
    private readonly IAppEventService _appEvents = appEvents;
    private string UploadsBasePath => Path.Combine(environment.ContentRootPath, "App_Data", "uploads", "conversaciones");
    private string ConnectionString => sessionService.GetConnectionString().Length > 0
        ? sessionService.GetConnectionString()
        : configuration.GetConnectionString("AlfaGestion")
          ?? throw new InvalidOperationException("No se configuró la cadena de conexión 'ConnectionStrings:AlfaGestion'.");

    public Task<IReadOnlyList<ConversacionTecnicoOptionDto>> GetTechniciansAsync(CancellationToken ct = default)
        => ExecuteLoggedAsync("Conversaciones", "GetTechnicians", async token =>
        {
            const string sql = """
                SELECT
                    ISNULL(IdTecnico, ''),
                    ISNULL(Nombre, ''),
                    ISNULL(Cargo, ''),
                    ISNULL(UsuarioAsociado, ''),
                    ISNULL(SistemaAsociado, '')
                FROM dbo.V_TA_Tecnicos
                WHERE ISNULL(Baja, 0) = 0
                ORDER BY Nombre
                """;

            var items = new List<ConversacionTecnicoOptionDto>();
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            await using var rd = await cmd.ExecuteReaderAsync(token);
            while (await rd.ReadAsync(token))
            {
                items.Add(new ConversacionTecnicoOptionDto
                {
                    IdTecnico = GetString(rd, 0),
                    Nombre = GetString(rd, 1),
                    Cargo = GetString(rd, 2),
                    UsuarioAsociado = GetString(rd, 3),
                    SistemaAsociado = GetString(rd, 4)
                });
            }

            return (IReadOnlyList<ConversacionTecnicoOptionDto>)items;
        }, "No se pudieron cargar los técnicos.", ct);

    public Task<IReadOnlyList<ConversacionEstadoOptionDto>> GetStatesAsync(CancellationToken ct = default)
        => ExecuteLoggedAsync("Conversaciones", "GetStates", async token =>
        {
            const string sql = """
                SELECT
                    ISNULL(CodigoEstado, ''),
                    ISNULL(Descripcion, ''),
                    ISNULL(EsCerrado, 0),
                    ISNULL(Orden, 0)
                FROM dbo.CONV_ESTADOS
                WHERE ISNULL(Activo, 1) = 1
                ORDER BY Orden, Descripcion
                """;

            var items = new List<ConversacionEstadoOptionDto>();
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            await using var rd = await cmd.ExecuteReaderAsync(token);
            while (await rd.ReadAsync(token))
            {
                items.Add(new ConversacionEstadoOptionDto
                {
                    CodigoEstado = GetString(rd, 0),
                    Descripcion = GetString(rd, 1),
                    EsCerrado = GetInt(rd, 2) == 1,
                    Orden = GetInt(rd, 3)
                });
            }

            return (IReadOnlyList<ConversacionEstadoOptionDto>)items;
        }, "No se pudieron cargar los estados de conversación.", ct);

    public Task<IReadOnlyList<ConversacionInboxItemDto>> GetInboxAsync(ConversacionesInboxFilters filters, CancellationToken ct = default)
        => ExecuteLoggedAsync("Conversaciones", "GetInbox", async token =>
        {
            filters ??= new();
            var items = new List<ConversacionInboxItemDto>();
            var sql = """
                SELECT
                    c.IdConversacion,
                    ISNULL(c.TelefonoWhatsApp, ''),
                    ISNULL(c.NombreVisible, ''),
                    ISNULL(c.ClienteCodigo, ''),
                    ISNULL(cli.RAZON_SOCIAL, ''),
                    c.IdContacto,
                    ISNULL(mc.Nombre_y_Apellido, ''),
                    ISNULL(c.CodigoEstado, ''),
                    ISNULL(e.Descripcion, ''),
                    ISNULL(c.IdTecnico, ''),
                    ISNULL(t.Nombre, ''),
                    ISNULL(c.ResumenUltimoMensaje, ''),
                    c.FechaHoraUltimoMensaje,
                    ISNULL(c.Archivada, 0),
                    ISNULL(c.Bloqueada, 0)
                FROM dbo.CONV_CONVERSACIONES c
                INNER JOIN dbo.CONV_ESTADOS e
                    ON e.CodigoEstado = c.CodigoEstado
                LEFT JOIN dbo.VT_CLIENTES cli
                    ON cli.CODIGO = c.ClienteCodigo
                LEFT JOIN dbo.MA_CONTACTOS mc
                    ON mc.id = c.IdContacto
                LEFT JOIN dbo.V_TA_Tecnicos t
                    ON t.IdTecnico = c.IdTecnico
                WHERE
                    (@Canal IS NULL OR c.Canal = @Canal)
                    AND (@CodigoEstado IS NULL OR c.CodigoEstado = @CodigoEstado)
                    AND (
                        @Search IS NULL
                        OR c.TelefonoWhatsApp LIKE @Search
                        OR c.NombreVisible LIKE @Search
                        OR cli.RAZON_SOCIAL LIKE @Search
                        OR mc.Nombre_y_Apellido LIKE @Search
                        OR c.ResumenUltimoMensaje LIKE @Search
                    )
                    AND (
                        @Modo = 'todas'
                        OR (@Modo = 'sin_asignar' AND (c.IdTecnico IS NULL OR LTRIM(RTRIM(c.IdTecnico)) = ''))
                        OR (@Modo = 'asignadas_a_mi' AND c.IdTecnico = @IdTecnicoActual)
                        OR (@Modo = 'pendientes' AND ISNULL(e.EsCerrado, 0) = 0)
                        OR (@Modo = 'cerradas' AND ISNULL(e.EsCerrado, 0) = 1)
                    )
                ORDER BY c.FechaHoraUltimoMensaje DESC
                OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY
                """;

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@Canal", DbNullable(filters.Canal));
            cmd.Parameters.AddWithValue("@CodigoEstado", DbNullable(filters.CodigoEstado));
            cmd.Parameters.AddWithValue("@Search", DbNullable(Like(filters.Search)));
            cmd.Parameters.AddWithValue("@Modo", NormalizeMode(filters.Modo));
            cmd.Parameters.AddWithValue("@IdTecnicoActual", DbNullable(filters.IdTecnicoActual));
            cmd.Parameters.AddWithValue("@Offset", Math.Max(0, filters.Offset));
            cmd.Parameters.AddWithValue("@Limit", Math.Clamp(filters.Limit, 1, 200));

            await using var rd = await cmd.ExecuteReaderAsync(token);
            while (await rd.ReadAsync(token))
            {
                items.Add(new ConversacionInboxItemDto
                {
                    IdConversacion = rd.GetInt64(0),
                    TelefonoWhatsApp = GetString(rd, 1),
                    NombreVisible = GetString(rd, 2),
                    ClienteCodigo = GetString(rd, 3),
                    ClienteNombre = GetString(rd, 4),
                    IdContacto = rd.IsDBNull(5) ? null : rd.GetInt32(5),
                    ContactoNombre = GetString(rd, 6),
                    CodigoEstado = GetString(rd, 7),
                    EstadoDescripcion = GetString(rd, 8),
                    IdTecnico = GetString(rd, 9),
                    TecnicoNombre = GetString(rd, 10),
                    ResumenUltimoMensaje = GetString(rd, 11),
                    FechaHoraUltimoMensaje = rd.IsDBNull(12) ? DateTime.MinValue : rd.GetDateTime(12),
                    Archivada = !rd.IsDBNull(13) && rd.GetBoolean(13),
                    Bloqueada = !rd.IsDBNull(14) && rd.GetBoolean(14)
                });
            }

            return (IReadOnlyList<ConversacionInboxItemDto>)items;
        }, "No se pudieron cargar las conversaciones.", ct);

    public Task<ConversacionDetalleDto?> GetConversationAsync(long conversationId, CancellationToken ct = default)
        => ExecuteLoggedAsync("Conversaciones", "GetConversation", async token =>
        {
            const string sql = """
                SELECT
                    c.IdConversacion,
                    ISNULL(c.Canal, ''),
                    ISNULL(c.TelefonoWhatsApp, ''),
                    ISNULL(c.NombreVisible, ''),
                    ISNULL(c.ClienteCodigo, ''),
                    ISNULL(cli.RAZON_SOCIAL, ''),
                    c.IdContacto,
                    ISNULL(mc.Nombre_y_Apellido, ''),
                    ISNULL(mc.Telefono, ''),
                    ISNULL(mc.Celular, ''),
                    ISNULL(mc.email, ''),
                    ISNULL(mc.Cargo, ''),
                    ISNULL(c.CodigoEstado, ''),
                    ISNULL(e.Descripcion, ''),
                    ISNULL(c.IdTecnico, ''),
                    ISNULL(t.Nombre, ''),
                    ISNULL(c.ResumenUltimoMensaje, ''),
                    ISNULL(c.Prioridad, ''),
                    ISNULL(c.Archivada, 0),
                    ISNULL(c.Bloqueada, 0),
                    c.FechaHoraPrimerMensaje,
                    c.FechaHoraUltimoMensaje,
                    c.FechaHoraCierre
                FROM dbo.CONV_CONVERSACIONES c
                INNER JOIN dbo.CONV_ESTADOS e
                    ON e.CodigoEstado = c.CodigoEstado
                LEFT JOIN dbo.VT_CLIENTES cli
                    ON cli.CODIGO = c.ClienteCodigo
                LEFT JOIN dbo.MA_CONTACTOS mc
                    ON mc.id = c.IdContacto
                LEFT JOIN dbo.V_TA_Tecnicos t
                    ON t.IdTecnico = c.IdTecnico
                WHERE c.IdConversacion = @IdConversacion
                """;

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@IdConversacion", conversationId);
            await using var rd = await cmd.ExecuteReaderAsync(token);
            if (!await rd.ReadAsync(token))
                return null;

            return new ConversacionDetalleDto
            {
                IdConversacion = rd.GetInt64(0),
                Canal = GetString(rd, 1),
                TelefonoWhatsApp = GetString(rd, 2),
                NombreVisible = GetString(rd, 3),
                ClienteCodigo = GetString(rd, 4),
                ClienteNombre = GetString(rd, 5),
                IdContacto = rd.IsDBNull(6) ? null : rd.GetInt32(6),
                ContactoNombre = GetString(rd, 7),
                ContactoTelefono = GetString(rd, 8),
                ContactoCelular = GetString(rd, 9),
                ContactoEmail = GetString(rd, 10),
                ContactoCargo = GetString(rd, 11),
                CodigoEstado = GetString(rd, 12),
                EstadoDescripcion = GetString(rd, 13),
                IdTecnico = GetString(rd, 14),
                TecnicoNombre = GetString(rd, 15),
                ResumenUltimoMensaje = GetString(rd, 16),
                Prioridad = GetString(rd, 17),
                Archivada = !rd.IsDBNull(18) && rd.GetBoolean(18),
                Bloqueada = !rd.IsDBNull(19) && rd.GetBoolean(19),
                FechaHoraPrimerMensaje = rd.IsDBNull(20) ? null : rd.GetDateTime(20),
                FechaHoraUltimoMensaje = rd.IsDBNull(21) ? DateTime.MinValue : rd.GetDateTime(21),
                FechaHoraCierre = rd.IsDBNull(22) ? null : rd.GetDateTime(22)
            };
        }, "No se pudo cargar la conversación.", ct);

    public Task<IReadOnlyList<ConversacionMensajeDto>> GetMessagesAsync(long conversationId, CancellationToken ct = default)
        => ExecuteLoggedAsync("Conversaciones", "GetMessages", async token =>
        {
            const string sql = """
                SELECT
                    m.IdMensaje,
                    m.IdConversacion,
                    ISNULL(m.TelefonoWhatsApp, ''),
                    ISNULL(m.WhatsAppMessageId, ''),
                    ISNULL(m.WhatsAppReplyToMessageId, ''),
                    ISNULL(m.MessageType, ''),
                    ISNULL(m.Direction, ''),
                    ISNULL(m.EstadoEnvio, ''),
                    ISNULL(m.Texto, ''),
                    m.FechaHora,
                    ISNULL(m.UsuarioAutor, ''),
                    ISNULL(m.SistemaAutor, ''),
                    ISNULL(m.IdTecnicoAutor, ''),
                    ISNULL(t.Nombre, ''),
                    CASE WHEN EXISTS (
                        SELECT 1
                        FROM dbo.CONV_ADJUNTOS a
                        WHERE a.IdMensaje = m.IdMensaje
                    ) THEN 1 ELSE 0 END
                FROM dbo.CONV_MENSAJES m
                LEFT JOIN dbo.V_TA_Tecnicos t
                    ON t.IdTecnico = m.IdTecnicoAutor
                WHERE m.IdConversacion = @IdConversacion
                ORDER BY m.FechaHora ASC, m.IdMensaje ASC
                """;

            var items = new List<ConversacionMensajeDto>();
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@IdConversacion", conversationId);
            await using var rd = await cmd.ExecuteReaderAsync(token);
            while (await rd.ReadAsync(token))
            {
                items.Add(new ConversacionMensajeDto
                {
                    IdMensaje = rd.GetInt64(0),
                    IdConversacion = rd.GetInt64(1),
                    TelefonoWhatsApp = GetString(rd, 2),
                    WhatsAppMessageId = GetString(rd, 3),
                    WhatsAppReplyToMessageId = GetString(rd, 4),
                    MessageType = GetString(rd, 5),
                    Direction = GetString(rd, 6),
                    EstadoEnvio = GetString(rd, 7),
                    Texto = GetString(rd, 8),
                    FechaHora = rd.IsDBNull(9) ? DateTime.MinValue : rd.GetDateTime(9),
                    UsuarioAutor = GetString(rd, 10),
                    SistemaAutor = GetString(rd, 11),
                    IdTecnicoAutor = GetString(rd, 12),
                    TecnicoAutorNombre = GetString(rd, 13),
                    TieneAdjuntos = GetInt(rd, 14) == 1
                });
            }

            return (IReadOnlyList<ConversacionMensajeDto>)items;
        }, "No se pudieron cargar los mensajes.", ct);

    public Task<ConversacionMessageResultDto> SendMessageAsync(ConversacionSendMessageRequest request, CancellationToken ct = default)
        => ExecuteLoggedAsync("Conversaciones", "SendMessage", async token =>
        {
            ValidateOutgoingRequest(request);

            var conversation = await RequireConversationAsync(request.IdConversacion, token);
            var isInternal = string.Equals(conversation.Canal, "INTERNO", StringComparison.OrdinalIgnoreCase);
            var now = DateTime.Now;

            string initialState;
            ConversacionWhatsAppConfigDto? whatsAppConfig = null;
            if (isInternal)
            {
                initialState = "ENVIADO";
            }
            else
            {
                whatsAppConfig = await conversacionesConfigService.GetWhatsAppConfigAsync(token);
                initialState = whatsAppConfig.IsConfiguredForSend ? "PENDIENTE" : "PENDIENTE_CONFIG";
            }

            var messageId = await InsertMessageAsync(new PendingMessageInsert
            {
                ConversationId = request.IdConversacion,
                Phone = conversation.TelefonoWhatsApp,
                ReplyToMessageId = request.WhatsAppReplyToMessageId,
                MessageType = NormalizeMessageType(request.MessageType),
                Direction = "SALIENTE",
                EstadoEnvio = initialState,
                Text = request.Texto.Trim(),
                PayloadJson = string.Empty,
                FechaHora = now,
                UsuarioAutor = request.UsuarioAccion,
                SistemaAutor = request.SistemaAccion,
                IdTecnicoAutor = request.IdTecnicoAutor
            }, token);

            string whatsAppMessageId = string.Empty;
            string finalState = initialState;
            string payload = string.Empty;

            if (!isInternal && whatsAppConfig?.IsConfiguredForSend == true)
            {
                var sendResult = await SendToWhatsAppAsync(whatsAppConfig, conversation.TelefonoWhatsApp, request.Texto.Trim(), request.WhatsAppReplyToMessageId, token);
                whatsAppMessageId = sendResult.WhatsAppMessageId;
                finalState = sendResult.EstadoEnvio;
                payload = sendResult.PayloadJson;
            }

            await UpdateMessageDeliveryAsync(messageId, finalState, whatsAppMessageId, payload, token);
            await RefreshConversationAsync(request.IdConversacion, now, request.Texto.Trim(), token);

            await _appEvents.LogAuditAsync(
                "Conversaciones",
                "SendMessage",
                "CONV_MENSAJES",
                messageId.ToString(CultureInfo.InvariantCulture),
                "Mensaje saliente registrado.",
                new { request.IdConversacion, finalState, whatsAppMessageId },
                token);

            return new ConversacionMessageResultDto
            {
                IdMensaje = messageId,
                EstadoEnvio = finalState,
                WhatsAppMessageId = whatsAppMessageId
            };
        }, "No se pudo registrar el mensaje saliente.", ct);

    public Task<long> AddInternalNoteAsync(ConversacionNotaInternaRequest request, CancellationToken ct = default)
        => ExecuteLoggedAsync("Conversaciones", "AddInternalNote", async token =>
        {
            if (request.IdConversacion <= 0)
                throw new InvalidOperationException("La conversación es obligatoria.");
            if (string.IsNullOrWhiteSpace(request.Texto))
                throw new InvalidOperationException("La nota interna no puede estar vacía.");

            var conversation = await RequireConversationAsync(request.IdConversacion, token);
            var text = request.Texto.Trim();
            var now = DateTime.Now;

            var messageId = await InsertMessageAsync(new PendingMessageInsert
            {
                ConversationId = request.IdConversacion,
                Phone = conversation.TelefonoWhatsApp,
                MessageType = "TEXT",
                Direction = "NOTA_INTERNA",
                EstadoEnvio = string.Empty,
                Text = text,
                PayloadJson = string.Empty,
                FechaHora = now,
                UsuarioAutor = request.UsuarioAccion,
                SistemaAutor = request.SistemaAccion,
                IdTecnicoAutor = request.IdTecnicoAutor
            }, token);

            await RefreshConversationAsync(request.IdConversacion, now, $"Nota interna: {TrimForSummary(text)}", token);
            await _appEvents.LogAuditAsync(
                "Conversaciones",
                "AddInternalNote",
                "CONV_MENSAJES",
                messageId.ToString(CultureInfo.InvariantCulture),
                "Nota interna agregada a la conversación.",
                new { request.IdConversacion },
                token);

            return messageId;
        }, "No se pudo agregar la nota interna.", ct);

    public async Task AssignConversationAsync(ConversacionAsignacionRequest request, CancellationToken ct = default)
    {
        await ExecuteLoggedAsync("Conversaciones", "AssignConversation", async token =>
        {
            if (request.IdConversacion <= 0)
                throw new InvalidOperationException("La conversación es obligatoria.");

            if (!string.IsNullOrWhiteSpace(request.IdTecnico))
                await ValidateTechnicianExistsAsync(request.IdTecnico, token);

            const string updateSql = """
                UPDATE dbo.CONV_CONVERSACIONES
                SET
                    IdTecnico = @IdTecnico,
                    FechaHora_Modificacion = GETDATE()
                WHERE IdConversacion = @IdConversacion
                """;

            const string historySql = """
                INSERT INTO dbo.CONV_ASIGNACIONES
                (
                    IdConversacion,
                    FechaHora,
                    IdTecnico,
                    UsuarioAccion,
                    SistemaAccion,
                    Observaciones
                )
                VALUES
                (
                    @IdConversacion,
                    GETDATE(),
                    @IdTecnico,
                    @UsuarioAccion,
                    @SistemaAccion,
                    @Observaciones
                )
                """;

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var tx = await cn.BeginTransactionAsync(token);

            await using (var cmd = new SqlCommand(updateSql, cn, (SqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@IdConversacion", request.IdConversacion);
                cmd.Parameters.AddWithValue("@IdTecnico", DbNullable(request.IdTecnico));
                await cmd.ExecuteNonQueryAsync(token);
            }

            await using (var cmd = new SqlCommand(historySql, cn, (SqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@IdConversacion", request.IdConversacion);
                cmd.Parameters.AddWithValue("@IdTecnico", DbNullable(request.IdTecnico));
                cmd.Parameters.AddWithValue("@UsuarioAccion", DbNullable(request.UsuarioAccion));
                cmd.Parameters.AddWithValue("@SistemaAccion", DbNullable(request.SistemaAccion));
                cmd.Parameters.AddWithValue("@Observaciones", DbNullable(request.Observaciones));
                await cmd.ExecuteNonQueryAsync(token);
            }

            await tx.CommitAsync(token);

            await _appEvents.LogAuditAsync(
                "Conversaciones",
                "AssignConversation",
                "CONV_CONVERSACIONES",
                request.IdConversacion.ToString(CultureInfo.InvariantCulture),
                "Asignación de conversación actualizada.",
                new { request.IdTecnico },
                token);

            return true;
        }, "No se pudo asignar la conversación.", ct);
    }

    public async Task ChangeStatusAsync(ConversacionEstadoRequest request, CancellationToken ct = default)
    {
        await ExecuteLoggedAsync("Conversaciones", "ChangeStatus", async token =>
        {
            if (request.IdConversacion <= 0)
                throw new InvalidOperationException("La conversación es obligatoria.");
            if (string.IsNullOrWhiteSpace(request.CodigoEstado))
                throw new InvalidOperationException("El estado es obligatorio.");

            var state = request.CodigoEstado.Trim().ToUpperInvariant();
            var isClosed = await GetStateClosedFlagAsync(state, token);

            const string sql = """
                UPDATE dbo.CONV_CONVERSACIONES
                SET
                    CodigoEstado = @CodigoEstado,
                    FechaHoraCierre = CASE WHEN @EsCerrado = 1 THEN ISNULL(FechaHoraCierre, GETDATE()) ELSE NULL END,
                    FechaHora_Modificacion = GETDATE()
                WHERE IdConversacion = @IdConversacion
                """;

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@IdConversacion", request.IdConversacion);
            cmd.Parameters.AddWithValue("@CodigoEstado", state);
            cmd.Parameters.AddWithValue("@EsCerrado", isClosed);
            await cmd.ExecuteNonQueryAsync(token);

            await _appEvents.LogAuditAsync(
                "Conversaciones",
                "ChangeStatus",
                "CONV_CONVERSACIONES",
                request.IdConversacion.ToString(CultureInfo.InvariantCulture),
                "Estado de conversación actualizado.",
                new { CodigoEstado = state, request.Observaciones, request.UsuarioAccion, request.SistemaAccion },
                token);

            return true;
        }, "No se pudo cambiar el estado de la conversación.", ct);
    }

    public Task<ConversacionWebhookResultDto> RegisterIncomingWebhookAsync(ConversacionWebhookRequest request, CancellationToken ct = default)
        => ExecuteLoggedAsync("Conversaciones", "RegisterIncomingWebhook", async token =>
        {
            ArgumentNullException.ThrowIfNull(request);
            var payloadJson = request.Payload.RootElement.GetRawText();
            var headerJson = JsonSerializer.Serialize(request.Headers);

            var webhookLogId = await InsertWebhookLogAsync(payloadJson, headerJson, token);
            var parsedMessages = ParseIncomingMessages(request.Payload.RootElement);
            var processed = 0;

            foreach (var incoming in parsedMessages)
            {
                var conversationId = await EnsureConversationAsync(incoming, token);
                await InsertMessageAsync(new PendingMessageInsert
                {
                    ConversationId = conversationId,
                    Phone = incoming.Phone,
                    MessageType = incoming.MessageType,
                    Direction = "ENTRANTE",
                    EstadoEnvio = "RECIBIDO",
                    Text = incoming.Text,
                    PayloadJson = incoming.RawJson,
                    FechaHora = incoming.Timestamp,
                    UsuarioAutor = string.Empty,
                    SistemaAutor = string.Empty,
                    IdTecnicoAutor = string.Empty,
                    WhatsAppMessageId = incoming.WhatsAppMessageId
                }, token);

                await RefreshConversationAsync(conversationId, incoming.Timestamp, incoming.Text, token);
                processed++;
            }

            await UpdateWebhookLogAsync(webhookLogId, true, string.Empty, token);

            await _appEvents.LogAuditAsync(
                "Conversaciones",
                "RegisterIncomingWebhook",
                "CONV_WEBHOOK_LOG",
                webhookLogId.ToString(CultureInfo.InvariantCulture),
                "Webhook de WhatsApp procesado.",
                new { Mensajes = parsedMessages.Count, Procesados = processed },
                token);

            return new ConversacionWebhookResultDto
            {
                IdWebhookLog = webhookLogId,
                MensajesDetectados = parsedMessages.Count,
                MensajesProcesados = processed
            };
        }, "No se pudo procesar el webhook de WhatsApp.", ct);

    public Task<long> CreateInternalThreadAsync(ConversacionCrearHiloInternoRequest request, CancellationToken ct = default)
        => ExecuteLoggedAsync("Conversaciones", "CreateInternalThread", async token =>
        {
            if (string.IsNullOrWhiteSpace(request.NombreHilo))
                throw new InvalidOperationException("El nombre del hilo es obligatorio.");

            if (!string.IsNullOrWhiteSpace(request.IdTecnico))
                await ValidateTechnicianExistsAsync(request.IdTecnico, token);

            const string sql = """
                INSERT INTO dbo.CONV_CONVERSACIONES
                (
                    Canal,
                    NombreVisible,
                    CodigoEstado,
                    IdTecnico,
                    FechaHoraUltimoMensaje,
                    FechaHora_Grabacion
                )
                VALUES
                (
                    N'INTERNO',
                    @NombreVisible,
                    N'ABIERTA',
                    @IdTecnico,
                    GETDATE(),
                    GETDATE()
                );

                SELECT CAST(SCOPE_IDENTITY() AS bigint);
                """;

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@NombreVisible", request.NombreHilo.Trim());
            cmd.Parameters.AddWithValue("@IdTecnico", DbNullable(request.IdTecnico));
            var result = await cmd.ExecuteScalarAsync(token);
            var id = Convert.ToInt64(result, CultureInfo.InvariantCulture);

            await _appEvents.LogAuditAsync(
                "Conversaciones",
                "CreateInternalThread",
                "CONV_CONVERSACIONES",
                id.ToString(CultureInfo.InvariantCulture),
                "Hilo interno creado.",
                new { request.NombreHilo, request.IdTecnico },
                token);

            return id;
        }, "No se pudo crear el hilo interno.", ct);

    public Task<ConversacionAdjuntoDto> UploadAttachmentAsync(ConversacionUploadAdjuntoRequest request, CancellationToken ct = default)
        => ExecuteLoggedAsync("Conversaciones", "UploadAttachment", async token =>
        {
            if (request.IdConversacion <= 0)
                throw new InvalidOperationException("La conversación es obligatoria.");
            if (string.IsNullOrWhiteSpace(request.NombreArchivo))
                throw new InvalidOperationException("El nombre del archivo es obligatorio.");
            if (request.TamanoBytes <= 0)
                throw new InvalidOperationException("El archivo está vacío.");

            var folder = Path.Combine(UploadsBasePath, request.IdConversacion.ToString(CultureInfo.InvariantCulture));
            Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(request.NombreArchivo).ToLowerInvariant();
            var safeFileName = $"{Guid.NewGuid():N}{ext}";
            var rutaLocal = Path.Combine(folder, safeFileName);

            await using (var fs = File.Create(rutaLocal))
                await request.Contenido.CopyToAsync(fs, token);

            var conversation = await RequireConversationAsync(request.IdConversacion, token);
            var messageType = NormalizeMessageType(request.TipoArchivo);
            var now = DateTime.Now;

            var messageId = await InsertMessageAsync(new PendingMessageInsert
            {
                ConversationId = request.IdConversacion,
                Phone = conversation.TelefonoWhatsApp,
                MessageType = messageType,
                Direction = "SALIENTE",
                EstadoEnvio = "ENVIADO",
                Text = request.NombreArchivo,
                PayloadJson = string.Empty,
                FechaHora = now,
                IdTecnicoAutor = request.IdTecnicoAutor,
                UsuarioAutor = request.UsuarioAccion,
                SistemaAutor = request.SistemaAccion
            }, token);

            const string adjuntoSql = """
                INSERT INTO dbo.CONV_ADJUNTOS
                (
                    IdMensaje,
                    TipoArchivo,
                    NombreArchivo,
                    MimeType,
                    RutaLocal,
                    TamanoBytes,
                    FechaHora_Grabacion
                )
                VALUES
                (
                    @IdMensaje,
                    @TipoArchivo,
                    @NombreArchivo,
                    @MimeType,
                    @RutaLocal,
                    @TamanoBytes,
                    GETDATE()
                );

                SELECT CAST(SCOPE_IDENTITY() AS bigint);
                """;

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(adjuntoSql, cn);
            cmd.Parameters.AddWithValue("@IdMensaje", messageId);
            cmd.Parameters.AddWithValue("@TipoArchivo", messageType);
            cmd.Parameters.AddWithValue("@NombreArchivo", request.NombreArchivo);
            cmd.Parameters.AddWithValue("@MimeType", DbNullable(request.MimeType));
            cmd.Parameters.AddWithValue("@RutaLocal", rutaLocal);
            cmd.Parameters.AddWithValue("@TamanoBytes", request.TamanoBytes);
            var adjuntoId = Convert.ToInt64(await cmd.ExecuteScalarAsync(token), CultureInfo.InvariantCulture);

            await RefreshConversationAsync(request.IdConversacion, now, $"[{messageType}] {request.NombreArchivo}", token);

            return new ConversacionAdjuntoDto
            {
                IdAdjunto = adjuntoId,
                IdMensaje = messageId,
                TipoArchivo = messageType,
                NombreArchivo = request.NombreArchivo,
                MimeType = request.MimeType,
                RutaLocal = rutaLocal,
                TamanoBytes = request.TamanoBytes
            };
        }, "No se pudo guardar el adjunto.", ct);

    public Task<IReadOnlyList<ConversacionAdjuntoDto>> GetConversationAttachmentsAsync(long idConversacion, CancellationToken ct = default)
        => ExecuteLoggedAsync("Conversaciones", "GetConversationAttachments", async token =>
        {
            const string sql = """
                SELECT
                    a.IdAdjunto,
                    a.IdMensaje,
                    ISNULL(a.TipoArchivo, ''),
                    ISNULL(a.NombreArchivo, ''),
                    ISNULL(a.MimeType, ''),
                    ISNULL(a.UrlArchivo, ''),
                    ISNULL(a.RutaLocal, ''),
                    ISNULL(a.TamanoBytes, 0)
                FROM dbo.CONV_ADJUNTOS a
                INNER JOIN dbo.CONV_MENSAJES m ON m.IdMensaje = a.IdMensaje
                WHERE m.IdConversacion = @IdConversacion
                ORDER BY a.IdAdjunto
                """;

            var items = new List<ConversacionAdjuntoDto>();
            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@IdConversacion", idConversacion);
            await using var rd = await cmd.ExecuteReaderAsync(token);
            while (await rd.ReadAsync(token))
            {
                items.Add(new ConversacionAdjuntoDto
                {
                    IdAdjunto = rd.GetInt64(0),
                    IdMensaje = rd.GetInt64(1),
                    TipoArchivo = GetString(rd, 2),
                    NombreArchivo = GetString(rd, 3),
                    MimeType = GetString(rd, 4),
                    UrlArchivo = GetString(rd, 5),
                    RutaLocal = GetString(rd, 6),
                    TamanoBytes = rd.IsDBNull(7) ? 0 : rd.GetInt64(7)
                });
            }

            return (IReadOnlyList<ConversacionAdjuntoDto>)items;
        }, "No se pudieron cargar los adjuntos.", ct);

    public Task<ConversacionAdjuntoServeDto?> GetAttachmentForServeAsync(long idAdjunto, CancellationToken ct = default)
        => ExecuteLoggedAsync("Conversaciones", "GetAttachmentForServe", async token =>
        {
            const string sql = """
                SELECT
                    ISNULL(RutaLocal, ''),
                    ISNULL(MimeType, ''),
                    ISNULL(NombreArchivo, '')
                FROM dbo.CONV_ADJUNTOS
                WHERE IdAdjunto = @IdAdjunto
                """;

            await using var cn = new SqlConnection(ConnectionString);
            await cn.OpenAsync(token);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@IdAdjunto", idAdjunto);
            await using var rd = await cmd.ExecuteReaderAsync(token);
            if (!await rd.ReadAsync(token))
                return null;

            return new ConversacionAdjuntoServeDto
            {
                RutaLocal = GetString(rd, 0),
                MimeType = GetString(rd, 1),
                NombreArchivo = GetString(rd, 2)
            };
        }, "No se pudo obtener el adjunto.", ct);

    private async Task<long> EnsureConversationAsync(IncomingWhatsAppMessage incoming, CancellationToken ct)
    {
        const string findSql = """
            SELECT TOP (1) IdConversacion
            FROM dbo.CONV_CONVERSACIONES
            WHERE TelefonoWhatsApp = @TelefonoWhatsApp
            ORDER BY FechaHoraUltimoMensaje DESC, IdConversacion DESC
            """;

        await using var cn = new SqlConnection(ConnectionString);
        await cn.OpenAsync(ct);

        await using (var findCmd = new SqlCommand(findSql, cn))
        {
            findCmd.Parameters.AddWithValue("@TelefonoWhatsApp", incoming.Phone);
            var existing = await findCmd.ExecuteScalarAsync(ct);
            if (existing is not null && existing is not DBNull)
                return Convert.ToInt64(existing, CultureInfo.InvariantCulture);
        }

        var contact = await TryFindContactByPhoneAsync(cn, incoming.Phone, ct);
        var displayName = !string.IsNullOrWhiteSpace(incoming.ContactName)
            ? incoming.ContactName
            : contact.Name;

        const string insertSql = """
            INSERT INTO dbo.CONV_CONVERSACIONES
            (
                Canal,
                TelefonoWhatsApp,
                NombreVisible,
                ClienteCodigo,
                IdContacto,
                CodigoEstado,
                IdTecnico,
                ResumenUltimoMensaje,
                FechaHoraPrimerMensaje,
                FechaHoraUltimoMensaje,
                FechaHora_Grabacion
            )
            VALUES
            (
                N'WHATSAPP',
                @TelefonoWhatsApp,
                @NombreVisible,
                @ClienteCodigo,
                @IdContacto,
                N'ABIERTA',
                NULL,
                @ResumenUltimoMensaje,
                @FechaHora,
                @FechaHora,
                GETDATE()
            );

            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """;

        await using var cmd = new SqlCommand(insertSql, cn);
        cmd.Parameters.AddWithValue("@TelefonoWhatsApp", incoming.Phone);
        cmd.Parameters.AddWithValue("@NombreVisible", DbNullable(displayName));
        cmd.Parameters.AddWithValue("@ClienteCodigo", DbNullable(contact.ClientCode));
        cmd.Parameters.AddWithValue("@IdContacto", contact.IdContact.HasValue ? contact.IdContact.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@ResumenUltimoMensaje", DbNullable(TrimForSummary(incoming.Text)));
        cmd.Parameters.AddWithValue("@FechaHora", incoming.Timestamp);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private async Task<ConversationIdentity> RequireConversationAsync(long idConversacion, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (1)
                IdConversacion,
                ISNULL(TelefonoWhatsApp, ''),
                ISNULL(Canal, 'WHATSAPP')
            FROM dbo.CONV_CONVERSACIONES
            WHERE IdConversacion = @IdConversacion
            """;

        await using var cn = new SqlConnection(ConnectionString);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@IdConversacion", idConversacion);
        await using var rd = await cmd.ExecuteReaderAsync(ct);

        if (!await rd.ReadAsync(ct))
            throw new InvalidOperationException("La conversación indicada no existe.");

        return new ConversationIdentity
        {
            IdConversacion = rd.GetInt64(0),
            TelefonoWhatsApp = GetString(rd, 1),
            Canal = GetString(rd, 2)
        };
    }

    private async Task<long> InsertMessageAsync(PendingMessageInsert message, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.CONV_MENSAJES
            (
                IdConversacion,
                TelefonoWhatsApp,
                WhatsAppMessageId,
                WhatsAppReplyToMessageId,
                MessageType,
                Direction,
                EstadoEnvio,
                Texto,
                PayloadJson,
                FechaHora,
                UsuarioAutor,
                SistemaAutor,
                IdTecnicoAutor,
                FechaHora_Grabacion
            )
            VALUES
            (
                @IdConversacion,
                @TelefonoWhatsApp,
                @WhatsAppMessageId,
                @WhatsAppReplyToMessageId,
                @MessageType,
                @Direction,
                @EstadoEnvio,
                @Texto,
                @PayloadJson,
                @FechaHora,
                @UsuarioAutor,
                @SistemaAutor,
                @IdTecnicoAutor,
                GETDATE()
            );

            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """;

        await using var cn = new SqlConnection(ConnectionString);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@IdConversacion", message.ConversationId);
        cmd.Parameters.AddWithValue("@TelefonoWhatsApp", DbNullable(message.Phone));
        cmd.Parameters.AddWithValue("@WhatsAppMessageId", DbNullable(message.WhatsAppMessageId));
        cmd.Parameters.AddWithValue("@WhatsAppReplyToMessageId", DbNullable(message.ReplyToMessageId));
        cmd.Parameters.AddWithValue("@MessageType", NormalizeMessageType(message.MessageType));
        cmd.Parameters.AddWithValue("@Direction", message.Direction);
        cmd.Parameters.AddWithValue("@EstadoEnvio", DbNullable(message.EstadoEnvio));
        cmd.Parameters.AddWithValue("@Texto", DbNullable(message.Text));
        cmd.Parameters.AddWithValue("@PayloadJson", DbNullable(message.PayloadJson));
        cmd.Parameters.AddWithValue("@FechaHora", message.FechaHora);
        cmd.Parameters.AddWithValue("@UsuarioAutor", DbNullable(message.UsuarioAutor));
        cmd.Parameters.AddWithValue("@SistemaAutor", DbNullable(message.SistemaAutor));
        cmd.Parameters.AddWithValue("@IdTecnicoAutor", DbNullable(message.IdTecnicoAutor));
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private async Task RefreshConversationAsync(long idConversacion, DateTime fechaHora, string? text, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.CONV_CONVERSACIONES
            SET
                ResumenUltimoMensaje = @ResumenUltimoMensaje,
                FechaHoraPrimerMensaje = ISNULL(FechaHoraPrimerMensaje, @FechaHora),
                FechaHoraUltimoMensaje = @FechaHora,
                FechaHora_Modificacion = GETDATE()
            WHERE IdConversacion = @IdConversacion
            """;

        await using var cn = new SqlConnection(ConnectionString);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@IdConversacion", idConversacion);
        cmd.Parameters.AddWithValue("@ResumenUltimoMensaje", DbNullable(TrimForSummary(text)));
        cmd.Parameters.AddWithValue("@FechaHora", fechaHora);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task UpdateMessageDeliveryAsync(long idMensaje, string estadoEnvio, string whatsAppMessageId, string payloadJson, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.CONV_MENSAJES
            SET
                EstadoEnvio = @EstadoEnvio,
                WhatsAppMessageId = CASE WHEN @WhatsAppMessageId IS NULL THEN WhatsAppMessageId ELSE @WhatsAppMessageId END,
                PayloadJson = CASE
                    WHEN @PayloadJson IS NULL OR LTRIM(RTRIM(@PayloadJson)) = '' THEN PayloadJson
                    ELSE @PayloadJson
                END,
                FechaHora_Modificacion = GETDATE()
            WHERE IdMensaje = @IdMensaje
            """;

        await using var cn = new SqlConnection(ConnectionString);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@IdMensaje", idMensaje);
        cmd.Parameters.AddWithValue("@EstadoEnvio", DbNullable(estadoEnvio));
        cmd.Parameters.AddWithValue("@WhatsAppMessageId", DbNullable(whatsAppMessageId));
        cmd.Parameters.AddWithValue("@PayloadJson", DbNullable(payloadJson));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task<WhatsAppSendResult> SendToWhatsAppAsync(ConversacionWhatsAppConfigDto config, string phone, string text, string? replyToMessageId, CancellationToken ct)
    {
        var url = $"https://graph.facebook.com/{config.ApiVersion}/{config.PhoneNumberId}/messages";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AccessToken.Trim());

        var payload = new Dictionary<string, object?>
        {
            ["messaging_product"] = "whatsapp",
            ["to"] = phone,
            ["type"] = "text",
            ["text"] = new Dictionary<string, object?> { ["body"] = text }
        };

        if (!string.IsNullOrWhiteSpace(replyToMessageId))
            payload["context"] = new Dictionary<string, object?> { ["message_id"] = replyToMessageId.Trim() };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var client = httpClientFactory.CreateClient();
        using var response = await client.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Meta devolvió {(int)response.StatusCode}: {responseBody}");

        var messageId = ExtractSentMessageId(responseBody);
        return new WhatsAppSendResult
        {
            EstadoEnvio = string.IsNullOrWhiteSpace(messageId) ? "ENVIADO" : "ENVIADO_META",
            WhatsAppMessageId = messageId,
            PayloadJson = responseBody
        };
    }

    private async Task<long> InsertWebhookLogAsync(string payloadJson, string headerJson, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.CONV_WEBHOOK_LOG
            (
                Proveedor,
                Evento,
                PayloadJson,
                HeaderJson,
                ProcesadoOk,
                FechaHoraRecepcion,
                Intentos
            )
            VALUES
            (
                N'META_WHATSAPP',
                N'Webhook',
                @PayloadJson,
                @HeaderJson,
                NULL,
                GETDATE(),
                1
            );

            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """;

        await using var cn = new SqlConnection(ConnectionString);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@PayloadJson", payloadJson);
        cmd.Parameters.AddWithValue("@HeaderJson", DbNullable(headerJson));
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private async Task UpdateWebhookLogAsync(long idWebhookLog, bool procesadoOk, string? errorDescripcion, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.CONV_WEBHOOK_LOG
            SET
                ProcesadoOk = @ProcesadoOk,
                ErrorDescripcion = @ErrorDescripcion,
                FechaHoraProcesamiento = GETDATE()
            WHERE IdWebhookLog = @IdWebhookLog
            """;

        await using var cn = new SqlConnection(ConnectionString);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@IdWebhookLog", idWebhookLog);
        cmd.Parameters.AddWithValue("@ProcesadoOk", procesadoOk);
        cmd.Parameters.AddWithValue("@ErrorDescripcion", DbNullable(errorDescripcion));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task<ContactLookupResult> TryFindContactByPhoneAsync(SqlConnection cn, string phone, CancellationToken ct)
    {
        var normalizedPhone = NormalizePhone(phone);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
            return ContactLookupResult.Empty;

        var sql = $"""
            SELECT TOP (1)
                id,
                ISNULL(CuentaRel, ''),
                ISNULL(Nombre_y_Apellido, '')
            FROM dbo.MA_CONTACTOS
            WHERE
                {SqlNormalizePhone("Telefono")} = @Telefono
                OR {SqlNormalizePhone("Celular")} = @Telefono
                OR {SqlNormalizePhone("TelefonoPart")} = @Telefono
                OR {SqlNormalizePhone("CelularPart")} = @Telefono
            ORDER BY id DESC
            """;

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Telefono", normalizedPhone);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        if (!await rd.ReadAsync(ct))
            return ContactLookupResult.Empty;

        return new ContactLookupResult
        {
            IdContact = rd.GetInt32(0),
            ClientCode = GetString(rd, 1),
            Name = GetString(rd, 2)
        };
    }

    private async Task ValidateTechnicianExistsAsync(string idTecnico, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (1) 1
            FROM dbo.V_TA_Tecnicos
            WHERE IdTecnico = @IdTecnico
              AND ISNULL(Baja, 0) = 0
            """;

        await using var cn = new SqlConnection(ConnectionString);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@IdTecnico", idTecnico.Trim());
        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is null || result is DBNull)
            throw new InvalidOperationException("El técnico indicado no existe o está dado de baja.");
    }

    private async Task<bool> GetStateClosedFlagAsync(string codigoEstado, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (1) ISNULL(EsCerrado, 0)
            FROM dbo.CONV_ESTADOS
            WHERE CodigoEstado = @CodigoEstado
            """;

        await using var cn = new SqlConnection(ConnectionString);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@CodigoEstado", codigoEstado);
        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is null || result is DBNull)
            throw new InvalidOperationException("El estado indicado no existe.");

        return Convert.ToBoolean(result, CultureInfo.InvariantCulture);
    }

    private static List<IncomingWhatsAppMessage> ParseIncomingMessages(JsonElement root)
    {
        var items = new List<IncomingWhatsAppMessage>();

        if (!root.TryGetProperty("entry", out var entries) || entries.ValueKind != JsonValueKind.Array)
            return items;

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value))
                    continue;

                string contactName = string.Empty;
                if (value.TryGetProperty("contacts", out var contacts) && contacts.ValueKind == JsonValueKind.Array)
                {
                    foreach (var contact in contacts.EnumerateArray())
                    {
                        if (contact.TryGetProperty("profile", out var profile) &&
                            profile.TryGetProperty("name", out var nameProp))
                        {
                            contactName = nameProp.GetString() ?? string.Empty;
                            break;
                        }
                    }
                }

                if (!value.TryGetProperty("messages", out var messages) || messages.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var message in messages.EnumerateArray())
                {
                    var phone = message.TryGetProperty("from", out var fromProp) ? fromProp.GetString() ?? string.Empty : string.Empty;
                    var type = message.TryGetProperty("type", out var typeProp) ? typeProp.GetString() ?? "unknown" : "unknown";
                    var messageId = message.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? string.Empty : string.Empty;
                    var timestamp = message.TryGetProperty("timestamp", out var tsProp) ? ParseUnixTimestamp(tsProp.GetString()) : DateTime.Now;
                    var text = ExtractIncomingText(message, type);

                    items.Add(new IncomingWhatsAppMessage
                    {
                        Phone = NormalizePhone(phone),
                        ContactName = contactName,
                        MessageType = NormalizeMessageType(type),
                        WhatsAppMessageId = messageId,
                        Timestamp = timestamp,
                        Text = text,
                        RawJson = message.GetRawText()
                    });
                }
            }
        }

        return items;
    }

    private static string ExtractIncomingText(JsonElement message, string type)
    {
        if (string.Equals(type, "text", StringComparison.OrdinalIgnoreCase) &&
            message.TryGetProperty("text", out var text) &&
            text.TryGetProperty("body", out var body))
            return body.GetString() ?? string.Empty;

        return $"[{type}]";
    }

    private static string ExtractSentMessageId(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("messages", out var messages) &&
                messages.ValueKind == JsonValueKind.Array)
            {
                foreach (var message in messages.EnumerateArray())
                {
                    if (message.TryGetProperty("id", out var id))
                        return id.GetString() ?? string.Empty;
                }
            }
        }
        catch
        {
            // Leave empty when the response is not parseable.
        }

        return string.Empty;
    }

    private static DateTime ParseUnixTimestamp(string? value)
    {
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unix))
            return DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime;

        return DateTime.Now;
    }

    private static void ValidateOutgoingRequest(ConversacionSendMessageRequest request)
    {
        if (request.IdConversacion <= 0)
            throw new InvalidOperationException("La conversación es obligatoria.");
        if (string.IsNullOrWhiteSpace(request.Texto))
            throw new InvalidOperationException("El texto del mensaje es obligatorio.");
    }

    private static string NormalizeMode(string? mode)
    {
        var normalized = string.IsNullOrWhiteSpace(mode) ? "todas" : mode.Trim().ToLowerInvariant();
        return normalized switch
        {
            "sin_asignar" => normalized,
            "asignadas_a_mi" => normalized,
            "pendientes" => normalized,
            "cerradas" => normalized,
            _ => "todas"
        };
    }

    private static string NormalizeMessageType(string? messageType)
    {
        var normalized = string.IsNullOrWhiteSpace(messageType) ? "TEXT" : messageType.Trim().ToUpperInvariant();
        return normalized switch
        {
            "TEXT" => normalized,
            "IMAGE" => normalized,
            "DOCUMENT" => normalized,
            "AUDIO" => normalized,
            "VIDEO" => normalized,
            "STICKER" => normalized,
            "LOCATION" => normalized,
            "CONTACT" => normalized,
            "SYSTEM" => normalized,
            _ => "UNKNOWN"
        };
    }

    private static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var sb = new StringBuilder(phone.Length);
        foreach (var ch in phone)
        {
            if (char.IsDigit(ch))
                sb.Append(ch);
        }

        return sb.ToString();
    }

    private static string SqlNormalizePhone(string columnName)
        => $"REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ISNULL({columnName}, ''), ' ', ''), '-', ''), '+', ''), '(', ''), ')', ''), '.', '')";

    private static string? Like(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : $"%{value.Trim()}%";

    private static object DbNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static string GetString(SqlDataReader rd, int index)
        => rd.IsDBNull(index) ? string.Empty : Convert.ToString(rd.GetValue(index), CultureInfo.InvariantCulture) ?? string.Empty;

    private static int GetInt(SqlDataReader rd, int index)
        => rd.IsDBNull(index) ? 0 : Convert.ToInt32(rd.GetValue(index), CultureInfo.InvariantCulture);

    private static string TrimForSummary(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var clean = value.Trim();
        return clean.Length <= 500 ? clean : clean[..500];
    }

    private static bool TryBuildKnownSqlMessage(SqlException ex, out string message)
    {
        message = string.Empty;
        if (ex.Number != 208)
            return false;

        var rawMessage = ex.Message ?? string.Empty;
        if (!rawMessage.Contains("CONV_", StringComparison.OrdinalIgnoreCase))
            return false;

        var objectName = ExtractMissingObjectName(rawMessage);
        var objectLabel = string.IsNullOrWhiteSpace(objectName) ? "CONV_*" : objectName;
        message = $"El modulo Conversaciones todavia no esta inicializado en la base activa. Falta crear el objeto {objectLabel}. Ejecuta el script docs/conversaciones_modelo_inicial.sql y recarga el modulo.";
        return true;
    }

    private static string ExtractMissingObjectName(string rawMessage)
    {
        var marker = "'";
        var firstQuote = rawMessage.IndexOf(marker, StringComparison.Ordinal);
        if (firstQuote < 0)
            return string.Empty;

        var secondQuote = rawMessage.IndexOf(marker, firstQuote + 1, StringComparison.Ordinal);
        if (secondQuote <= firstQuote)
            return string.Empty;

        var value = rawMessage[(firstQuote + 1)..secondQuote].Trim();
        return value.StartsWith("dbo.", StringComparison.OrdinalIgnoreCase)
            ? value[4..]
            : value;
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
        catch (SqlException ex) when (TryBuildKnownSqlMessage(ex, out var knownMessage))
        {
            var incidentId = await _appEvents.LogErrorAsync(module, action, ex, userMessage, null, AppEventSeverity.Error, ct);
            throw new InvalidOperationException($"{knownMessage} Codigo: {incidentId}");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var incidentId = await _appEvents.LogErrorAsync(module, action, ex, userMessage, null, AppEventSeverity.Error, ct);
            throw new InvalidOperationException($"{userMessage} Código: {incidentId}");
        }
    }

    private sealed class PendingMessageInsert
    {
        public long ConversationId { get; init; }
        public string Phone { get; init; } = string.Empty;
        public string WhatsAppMessageId { get; init; } = string.Empty;
        public string? ReplyToMessageId { get; init; }
        public string MessageType { get; init; } = "TEXT";
        public string Direction { get; init; } = string.Empty;
        public string EstadoEnvio { get; init; } = string.Empty;
        public string Text { get; init; } = string.Empty;
        public string PayloadJson { get; init; } = string.Empty;
        public DateTime FechaHora { get; init; }
        public string? UsuarioAutor { get; init; }
        public string? SistemaAutor { get; init; }
        public string? IdTecnicoAutor { get; init; }
    }

    private sealed class ConversationIdentity
    {
        public long IdConversacion { get; init; }
        public string TelefonoWhatsApp { get; init; } = string.Empty;
        public string Canal { get; init; } = string.Empty;
    }

    private sealed class ContactLookupResult
    {
        public static ContactLookupResult Empty { get; } = new();

        public int? IdContact { get; init; }
        public string ClientCode { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
    }

    private sealed class IncomingWhatsAppMessage
    {
        public string Phone { get; init; } = string.Empty;
        public string ContactName { get; init; } = string.Empty;
        public string MessageType { get; init; } = "TEXT";
        public string WhatsAppMessageId { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; }
        public string Text { get; init; } = string.Empty;
        public string RawJson { get; init; } = string.Empty;
    }

    private sealed class WhatsAppSendResult
    {
        public string EstadoEnvio { get; init; } = string.Empty;
        public string WhatsAppMessageId { get; init; } = string.Empty;
        public string PayloadJson { get; init; } = string.Empty;
    }
}
