using AlfaCore.Models;

namespace AlfaCore.Services;

public interface IInformesIaService
{
    Task<IReadOnlyList<InformeIaSuggestionDto>> GetSuggestionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InformeIaHistoryItemDto>> GetHistoryAsync(CancellationToken cancellationToken = default);
    Task DeleteHistoryItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InformeIaExecutionDto> ExecuteAndStoreAsync(InformeIaRequestDto request, CancellationToken cancellationToken = default);
    Task<InformeIaResultDto?> GetExecutionResultAsync(Guid executionId, CancellationToken cancellationToken = default);
}
