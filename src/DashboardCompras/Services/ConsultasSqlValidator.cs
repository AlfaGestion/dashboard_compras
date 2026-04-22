using System.Text.RegularExpressions;

namespace DashboardCompras.Services;

internal static partial class ConsultasSqlValidator
{
    private static readonly string[] ForbiddenTokens =
    [
        "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "TRUNCATE",
        "EXEC", "EXECUTE", "MERGE", "CREATE", "GRANT", "REVOKE",
        "DENY", "OPENROWSET", "OPENDATASOURCE", "BULK",
        "RESTORE", "BACKUP", "DBCC", "SHUTDOWN"
    ];

    public static bool TryValidate(string sql, out string message)
    {
        message = string.Empty;
        if (string.IsNullOrWhiteSpace(sql))
        {
            message = "La consulta no tiene SQL definido.";
            return false;
        }

        var trimmed = sql.Trim();
        var upper = trimmed.ToUpperInvariant();

        if (!upper.StartsWith("SELECT", StringComparison.Ordinal) &&
            !upper.StartsWith("WITH", StringComparison.Ordinal))
        {
            message = "Solo se permiten consultas de lectura (SELECT).";
            return false;
        }

        foreach (var token in ForbiddenTokens)
        {
            if (Regex.IsMatch(trimmed, $@"\b{Regex.Escape(token)}\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                message = $"La consulta contiene una instrucción no permitida: '{token}'.";
                return false;
            }
        }

        return true;
    }
}
