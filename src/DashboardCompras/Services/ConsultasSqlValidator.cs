using System.Text.RegularExpressions;

namespace DashboardCompras.Services;

internal static partial class ConsultasSqlValidator
{
    private static readonly string[] AllowedStartingTokens =
    [
        "SELECT", "WITH", "DECLARE", "SET"
    ];

    private static readonly string[] ForbiddenTokens =
    [
        "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "TRUNCATE",
        "EXEC", "EXECUTE", "MERGE", "CREATE", "GRANT", "REVOKE",
        "DENY", "OPENROWSET", "OPENDATASOURCE", "BULK", "WAITFOR",
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

        var sanitized = SanitizeForValidation(sql);
        var firstToken = FirstTokenRegex().Match(sanitized).Value;
        if (string.IsNullOrWhiteSpace(firstToken))
        {
            message = "La consulta no contiene instrucciones SQL válidas.";
            return false;
        }

        if (!AllowedStartingTokens.Contains(firstToken, StringComparer.OrdinalIgnoreCase))
        {
            message = "Solo se permiten consultas de lectura. Podés comenzar con SELECT, WITH, DECLARE o SET si el bloque termina en un SELECT.";
            return false;
        }

        if (!Regex.IsMatch(sanitized, @"\bSELECT\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            message = "La consulta debe incluir un SELECT para devolver resultados.";
            return false;
        }

        foreach (var token in ForbiddenTokens)
        {
            if (Regex.IsMatch(sanitized, $@"\b{Regex.Escape(token)}\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                message = $"La consulta contiene una instrucción no permitida: '{token}'.";
                return false;
            }
        }

        return true;
    }

    private static string SanitizeForValidation(string sql)
    {
        var chars = sql.ToCharArray();
        var result = new char[chars.Length];
        var i = 0;
        var inString = false;
        var inLineComment = false;
        var inBlockComment = false;

        while (i < chars.Length)
        {
            var current = chars[i];
            var next = i + 1 < chars.Length ? chars[i + 1] : '\0';

            if (inLineComment)
            {
                result[i] = current is '\r' or '\n' ? current : ' ';
                if (current is '\r' or '\n')
                {
                    inLineComment = false;
                }

                i++;
                continue;
            }

            if (inBlockComment)
            {
                result[i] = current is '\r' or '\n' ? current : ' ';
                if (current == '*' && next == '/')
                {
                    result[i + 1] = ' ';
                    inBlockComment = false;
                    i += 2;
                    continue;
                }

                i++;
                continue;
            }

            if (inString)
            {
                result[i] = ' ';
                if (current == '\'' && next == '\'')
                {
                    result[i + 1] = ' ';
                    i += 2;
                    continue;
                }

                if (current == '\'')
                {
                    inString = false;
                }

                i++;
                continue;
            }

            if (current == '-' && next == '-')
            {
                result[i] = ' ';
                result[i + 1] = ' ';
                inLineComment = true;
                i += 2;
                continue;
            }

            if (current == '/' && next == '*')
            {
                result[i] = ' ';
                result[i + 1] = ' ';
                inBlockComment = true;
                i += 2;
                continue;
            }

            if (current == '\'')
            {
                result[i] = ' ';
                inString = true;
                i++;
                continue;
            }

            result[i] = current;
            i++;
        }

        return new string(result).Trim();
    }

    [GeneratedRegex(@"\b[A-Z_][A-Z0-9_]*\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FirstTokenRegex();
}
