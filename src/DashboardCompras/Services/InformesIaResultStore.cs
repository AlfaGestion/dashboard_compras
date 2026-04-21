using DashboardCompras.Models;
using System.Text.Json;

namespace DashboardCompras.Services;

public sealed class InformesIaResultStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _directory;

    public InformesIaResultStore(IHostEnvironment environment)
    {
        _directory = Path.Combine(environment.ContentRootPath, "App_Data", "informesia-results");
        Directory.CreateDirectory(_directory);
    }

    public async Task SaveAsync(InformeIaResultDto result, CancellationToken cancellationToken)
    {
        var filePath = GetPath(result.ExecutionId);
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, result, JsonOptions, cancellationToken);
    }

    public async Task<InformeIaResultDto?> GetAsync(Guid executionId, CancellationToken cancellationToken)
    {
        var filePath = GetPath(executionId);
        if (!File.Exists(filePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<InformeIaResultDto>(stream, JsonOptions, cancellationToken);
    }

    private string GetPath(Guid executionId) => Path.Combine(_directory, $"{executionId:N}.json");
}
