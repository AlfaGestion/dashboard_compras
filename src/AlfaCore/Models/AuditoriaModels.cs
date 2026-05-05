namespace AlfaCore.Models;

public sealed class AuditoriaErrorRowDto
{
    public int Id { get; init; }
    public DateTime Fecha { get; init; }
    public string Proceso { get; init; } = string.Empty;
    public int ErrorCodigo { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public string Sql { get; init; } = string.Empty;
    public string Pc { get; init; } = string.Empty;
    public string Usuario { get; init; } = string.Empty;
}

public sealed class AuditoriaErrorFilterDto
{
    public DateTime? Desde { get; init; }
    public DateTime? Hasta { get; init; }
    public string Usuario { get; init; } = string.Empty;
    public string Proceso { get; init; } = string.Empty;
    public string Pc { get; init; } = string.Empty;
    public int? ErrorCodigo { get; init; }
    public string Texto { get; init; } = string.Empty;
    public int MaxRows { get; init; } = 200;
}

public sealed class AuditoriaResumenDto
{
    public int ErrorsToday { get; init; }
    public int ErrorsLast7Days { get; init; }
    public string TopProcess { get; init; } = string.Empty;
    public string TopUser { get; init; } = string.Empty;
    public IReadOnlyList<AuditoriaSerieDto> ErrorsByDay { get; init; } = [];
    public IReadOnlyList<AuditoriaRankingItemDto> TopProcesses { get; init; } = [];
    public IReadOnlyList<AuditoriaRankingItemDto> TopUsers { get; init; } = [];
}

public sealed class AuditoriaSerieDto
{
    public string Label { get; init; } = string.Empty;
    public int Value { get; init; }
}

public sealed class AuditoriaRankingItemDto
{
    public string Label { get; init; } = string.Empty;
    public int Value { get; init; }
}
