using System.Text.RegularExpressions;

namespace AlfaCore.Services;

internal static partial class InformesIaSqlValidator
{
    private static readonly HashSet<string> AllowedViews = new(StringComparer.OrdinalIgnoreCase)
    {
        "vw_compras_cabecera_dashboard",
        "vw_compras_detalle_dashboard",
        "vw_estadisticas_ingresos_diarias",
        "vw_familias_jerarquia"
    };

    private static readonly string[] ForbiddenTokens =
    [
        "INSERT",
        "UPDATE",
        "DELETE",
        "DROP",
        "ALTER",
        "TRUNCATE",
        "EXEC",
        "MERGE",
        "CREATE",
        "INTO",
        "GRANT",
        "REVOKE",
        "DENY",
        "OPENROWSET",
        "OPENDATASOURCE",
        "BULK",
        "RESTORE",
        "BACKUP",
        "DBCC",
        "SHUTDOWN",
        "ATTACH",
        "DETACH"
    ];

    public static bool TryValidate(string sql, out string message)
    {
        message = string.Empty;

        if (string.IsNullOrWhiteSpace(sql))
        {
            message = "No se generó una consulta SQL para ejecutar.";
            return false;
        }

        var trimmed = sql.Trim();
        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            message = "Solo se permiten consultas de lectura que comiencen con SELECT.";
            return false;
        }

        if (trimmed.Contains(';', StringComparison.Ordinal)
            || trimmed.Contains("--", StringComparison.Ordinal)
            || trimmed.Contains("/*", StringComparison.Ordinal)
            || trimmed.Contains("*/", StringComparison.Ordinal))
        {
            message = "La consulta fue rechazada porque contiene separadores o comentarios no permitidos.";
            return false;
        }

        if (trimmed.Contains("sp_", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("xp_", StringComparison.OrdinalIgnoreCase))
        {
            message = "La consulta fue rechazada porque intenta usar procedimientos o extensiones no autorizadas.";
            return false;
        }

        foreach (var token in ForbiddenTokens)
        {
            if (Regex.IsMatch(trimmed, $@"\b{Regex.Escape(token)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                message = $"La consulta fue rechazada porque contiene la instrucción no permitida '{token}'.";
                return false;
            }
        }

        var viewMatches = SqlSourceRegex().Matches(trimmed);
        if (viewMatches.Count == 0)
        {
            message = "La consulta fue rechazada porque no referencia ninguna de las vistas autorizadas.";
            return false;
        }

        foreach (Match match in viewMatches)
        {
            var rawSource = match.Groups[1].Value
                .Replace("[", string.Empty, StringComparison.Ordinal)
                .Replace("]", string.Empty, StringComparison.Ordinal);

            var sourceName = rawSource.Split('.').LastOrDefault() ?? rawSource;
            if (!AllowedViews.Contains(sourceName))
            {
                message = $"La consulta fue rechazada porque intenta leer desde '{sourceName}', que no está autorizado.";
                return false;
            }
        }

        return true;
    }

    [GeneratedRegex(@"\b(?:FROM|JOIN)\s+([A-Za-z0-9_\.\[\]]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SqlSourceRegex();
}
