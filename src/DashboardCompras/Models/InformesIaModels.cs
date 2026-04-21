namespace DashboardCompras.Models;

public sealed class InformeIaSuggestionDto
{
    public string Texto { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public string Categoria { get; init; } = string.Empty;
}

public sealed class InformeIaHistoryItemDto
{
    public Guid Id { get; init; }
    public Guid? ExecutionId { get; init; }
    public string Consulta { get; init; } = string.Empty;
    public DateTime FechaHora { get; init; }
    public bool Exitosa { get; init; }
    public string TipoResultado { get; init; } = string.Empty;
    public string Titulo { get; init; } = string.Empty;
}

public sealed class InformeIaPreferencesDto
{
    public bool IncluirGrafico { get; set; } = true;
    public string? OrdenCampo { get; set; }
    public bool OrdenDescendente { get; set; } = true;
    public IReadOnlyList<string> CamposSolicitados { get; set; } = [];
}

public sealed class InformeIaRequestDto
{
    public string Consulta { get; set; } = string.Empty;
    public DashboardFilters Filtros { get; set; } = new();
    public InformeIaPreferencesDto Preferencias { get; set; } = new();
}

public sealed class InformeIaColumnDto
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Format { get; init; } = "texto";
    public string Align { get; init; } = "left";
}

public sealed class InformeIaRowDto
{
    public IReadOnlyList<string> Values { get; init; } = [];
}

public sealed class InformeIaChartDto
{
    public string Tipo { get; init; } = string.Empty;
    public string Titulo { get; init; } = string.Empty;
    public string Subtitulo { get; init; } = string.Empty;
    public string Formato { get; init; } = "moneda";
    public IReadOnlyList<CategoryTotalDto> Barras { get; init; } = [];
    public IReadOnlyList<MonthlyPointDto> Linea { get; init; } = [];
}

public sealed class InformeIaResultDto
{
    public Guid ExecutionId { get; init; }
    public bool Exitoso { get; init; }
    public string ConsultaOriginal { get; init; } = string.Empty;
    public string Titulo { get; init; } = string.Empty;
    public string Subtitulo { get; init; } = string.Empty;
    public string Resumen { get; init; } = string.Empty;
    public string Mensaje { get; init; } = string.Empty;
    public string NotaSeguridad { get; init; } = string.Empty;
    public string TipoResultado { get; init; } = string.Empty;
    public DateTime GeneradoEn { get; init; } = DateTime.Now;
    public DashboardFilters FiltrosAplicados { get; init; } = new();
    public IReadOnlyList<string> FuentesAutorizadas { get; init; } = [];
    public IReadOnlyList<InformeIaColumnDto> Columnas { get; init; } = [];
    public IReadOnlyList<InformeIaRowDto> Filas { get; init; } = [];
    public InformeIaChartDto? Grafico { get; init; }
    public IReadOnlyList<InformeIaSuggestionDto> SugerenciasRelacionadas { get; init; } = [];
    public string SqlGenerado { get; init; } = string.Empty;
}

public sealed class InformeIaExecutionDto
{
    public Guid ExecutionId { get; init; }
    public string UrlResultado { get; init; } = string.Empty;
    public InformeIaResultDto Resultado { get; init; } = new();
}
