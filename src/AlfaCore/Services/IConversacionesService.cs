using AlfaCore.Models;

namespace AlfaCore.Services;

public interface IConversacionesService
{
    Task<IReadOnlyList<ConversacionInboxItemDto>> GetInboxAsync(ConversacionesInboxFilters filters, CancellationToken ct = default);
    Task<ConversacionDetalleDto?> GetConversationAsync(long conversationId, CancellationToken ct = default);
    Task<IReadOnlyList<ConversacionMensajeDto>> GetMessagesAsync(long conversationId, CancellationToken ct = default);
    Task<ConversacionMessageResultDto> SendMessageAsync(ConversacionSendMessageRequest request, CancellationToken ct = default);
    Task<long> AddInternalNoteAsync(ConversacionNotaInternaRequest request, CancellationToken ct = default);
    Task AssignConversationAsync(ConversacionAsignacionRequest request, CancellationToken ct = default);
    Task ChangeStatusAsync(ConversacionEstadoRequest request, CancellationToken ct = default);
    Task<ConversacionWebhookResultDto> RegisterIncomingWebhookAsync(ConversacionWebhookRequest request, CancellationToken ct = default);
}
