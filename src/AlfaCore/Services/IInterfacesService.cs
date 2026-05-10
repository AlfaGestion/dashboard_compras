using AlfaCore.Models;

namespace AlfaCore.Services;

public interface IInterfacesService
{
    Task<IReadOnlyList<InterfacesEstadoOptionDto>> GetStatesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<InterfacesTipoDocumentoOptionDto>> GetDocumentTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<InterfacesInboxItemDto>> SearchAsync(InterfacesFilters filters, CancellationToken ct = default);
    Task<InterfacesDetalleDto?> GetByIdAsync(long idComprobanteRecibido, CancellationToken ct = default);
    Task<long> CreateAsync(InterfacesCrearComprobanteRequest request, CancellationToken ct = default);
    Task UpdateAsync(InterfacesActualizarComprobanteRequest request, CancellationToken ct = default);
    Task AddAttachmentsAsync(InterfacesAgregarAdjuntosRequest request, CancellationToken ct = default);
    Task RemoveAttachmentAsync(InterfacesEliminarAdjuntoRequest request, CancellationToken ct = default);
    Task ChangeStatusAsync(InterfacesCambioEstadoRequest request, CancellationToken ct = default);
    Task<InterfacesAdjuntoServeDto?> GetAttachmentForServeAsync(long idAdjunto, CancellationToken ct = default);
}
