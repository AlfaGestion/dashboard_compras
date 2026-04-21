using DashboardCompras.Models;
using System.Text.Json;

namespace DashboardCompras.Services;

public sealed class InformesIaHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;

    public InformesIaHistoryStore(IHostEnvironment environment)
    {
        var directory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "informesia-history.json");
    }

    public async Task<IReadOnlyList<InformeIaHistoryItemDto>> GetAsync(string userKey, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var data = await ReadUnsafeAsync(cancellationToken);
            return data.TryGetValue(userKey, out var items)
                ? items.OrderByDescending(x => x.FechaHora).ToList()
                : [];
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AppendAsync(string userKey, InformeIaHistoryItemDto item, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var data = await ReadUnsafeAsync(cancellationToken);
            if (!data.TryGetValue(userKey, out var items))
            {
                items = [];
                data[userKey] = items;
            }

            items.Insert(0, item);
            if (items.Count > 20)
            {
                items.RemoveRange(20, items.Count - 20);
            }

            await WriteUnsafeAsync(data, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteAsync(string userKey, Guid id, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var data = await ReadUnsafeAsync(cancellationToken);
            if (!data.TryGetValue(userKey, out var items))
            {
                return;
            }

            items.RemoveAll(x => x.Id == id);
            await WriteUnsafeAsync(data, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<string, List<InformeIaHistoryItemDto>>> ReadUnsafeAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return new Dictionary<string, List<InformeIaHistoryItemDto>>(StringComparer.OrdinalIgnoreCase);
        }

        await using var stream = File.OpenRead(_filePath);
        var data = await JsonSerializer.DeserializeAsync<Dictionary<string, List<InformeIaHistoryItemDto>>>(stream, JsonOptions, cancellationToken);
        return data is null
            ? new Dictionary<string, List<InformeIaHistoryItemDto>>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, List<InformeIaHistoryItemDto>>(data, StringComparer.OrdinalIgnoreCase);
    }

    private async Task WriteUnsafeAsync(Dictionary<string, List<InformeIaHistoryItemDto>> data, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, data, JsonOptions, cancellationToken);
    }
}
