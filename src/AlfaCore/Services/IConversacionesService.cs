using AlfaCore.Models;

namespace AlfaCore.Services;

public interface IConversacionesService
{
    Task<IReadOnlyList<ConversacionTecnicoOptionDto>> GetTechniciansAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ConversacionEstadoOptionDto>> GetStatesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ConversacionInboxItemDto>> GetInboxAsync(ConversacionesInboxFilters filters, CancellationToken ct = default);
    Task<ConversacionDetalleDto?> GetConversationAsync(long conversationId, CancellationToken ct = default);
    Task<IReadOnlyList<ConversacionMensajeDto>> GetMessagesAsync(long conversationId, CancellationToken ct = default);
    Task<ConversacionMessageResultDto> SendMessageAsync(ConversacionSendMessageRequest request, CancellationToken ct = default);
    Task<long> AddInternalNoteAsync(ConversacionNotaInternaRequest request, CancellationToken ct = default);
    Task AssignConversationAsync(ConversacionAsignacionRequest request, CancellationToken ct = default);
    Task ChangeStatusAsync(ConversacionEstadoRequest request, CancellationToken ct = default);
    Task<ConversacionWebhookResultDto> RegisterIncomingWebhookAsync(ConversacionWebhookRequest request, CancellationToken ct = default);
    Task<long> CreateInternalThreadAsync(ConversacionCrearHiloInternoRequest request, CancellationToken ct = default);
    Task<ConversacionAdjuntoDto> UploadAttachmentAsync(ConversacionUploadAdjuntoRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ConversacionAdjuntoDto>> GetConversationAttachmentsAsync(long idConversacion, CancellationToken ct = default);
    Task<ConversacionAdjuntoServeDto?> GetAttachmentForServeAsync(long idAdjunto, CancellationToken ct = default);
}
