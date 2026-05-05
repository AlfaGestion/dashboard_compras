using AlfaCore.Models;

namespace AlfaCore.Services;

public interface ICostosService
{
    Task<IReadOnlyList<CostosProfileDto>> GetProfilesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CostosProviderLookupDto>> SearchProvidersAsync(string term, int limit = 15, CancellationToken ct = default);
    Task<int> CreateProfileAsync(CostosProfileDto profile, CancellationToken ct = default);
    Task UpdateProfileAsync(CostosProfileDto profile, CancellationToken ct = default);
    Task DeleteProfileAsync(int profileId, CancellationToken ct = default);
    Task<IReadOnlyList<CostosBatchSummaryDto>> GetRecentBatchesAsync(int limit = 25, CancellationToken ct = default);
    Task<IReadOnlyList<CostosHistoryDto>> GetRecentHistoryAsync(int limit = 100, CancellationToken ct = default);
    Task<CostosFileColumnsDto> DetectFileColumnsAsync(string fileName, Stream content, string preferredSheet = "", CancellationToken ct = default);
    Task<CostosImportResultDto> ImportStructuredFileAsync(int profileId, string originalFileName, Stream content, string userName, string? notesOverride = null, int? forcedPriceColIndex = null, IProgress<int>? progress = null, CancellationToken ct = default);
    Task<CostosBatchDetailDto?> GetBatchDetailAsync(int batchId, CancellationToken ct = default);
    Task<int?> GetLastUsedProfileIdAsync(CancellationToken ct = default);
    Task ProcessMatchingAsync(int batchId, string userName, IProgress<int>? progress = null, CancellationToken ct = default);
    Task<IReadOnlyList<CostosMatchCandidateDto>> GetCandidatesAsync(int batchId, int rowNumber, string? searchTerm = null, bool includeFallback = false, CancellationToken ct = default);
    Task ConfirmRowAsync(int batchId, int rowNumber, string? articleId, string userName, CancellationToken ct = default);
    Task DiscardRowAsync(int batchId, int rowNumber, string userName, CancellationToken ct = default);
    Task<CostosApplyResultDto> ApplyConfirmedRowsAsync(int batchId, string userName, CancellationToken ct = default);
    Task<CostosApplyResultDto> ApplySelectedRowsAsync(int batchId, IReadOnlyCollection<int> rowNumbers, string userName, CancellationToken ct = default);
    Task<CostosUndoResultDto> UndoLastApplyAsync(int batchId, string userName, CancellationToken ct = default);
}
