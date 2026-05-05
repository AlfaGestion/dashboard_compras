namespace AlfaCore.Configuration;

public sealed class DatosSqlOptions
{
    public const string SectionName = "DatosSql";

    public int CommandTimeoutSegundos { get; set; } = 30;
    public int LogConsultasLentasDesdeMs { get; set; } = 1000;
}
