using Microsoft.Data.SqlClient;

namespace AlfaCore.Configuration;

internal static class StartupConnectionResolver
{
    public static string? Resolve(string[] args, IConfiguration configuration, string contentRootPath)
    {
        var argumentValues = ParseArguments(args);
        var configuredConnectionString = configuration.GetConnectionString("AlfaGestion");
        var configuredBuilder = TryCreateBuilder(configuredConnectionString);
        var suppressPrompt = string.Equals(
            Environment.GetEnvironmentVariable("DASHBOARD_NO_PROMPT"),
            "1",
            StringComparison.Ordinal);

        var server = GetValue(argumentValues, "server") ?? configuredBuilder?.DataSource;
        var database = GetValue(argumentValues, "dbname") ?? configuredBuilder?.InitialCatalog;
        var user = GetValue(argumentValues, "usuario") ?? configuredBuilder?.UserID;
        var password = GetValue(argumentValues, "password") ?? configuredBuilder?.Password;

        var prompted = false;
        if (!suppressPrompt && Environment.UserInteractive && !IsComplete(server, database, user, password))
        {
            Console.WriteLine();
            Console.WriteLine("Faltan datos de conexion. Completalos para continuar.");
            server = PromptRequired("Servidor", server);
            database = PromptRequired("Base", database);
            user = PromptRequired("Usuario", user);
            password = PromptRequired("Clave", password, secret: true);
            prompted = true;
        }

        if (!IsComplete(server, database, user, password))
        {
            return configuredConnectionString;
        }

        var builder = configuredBuilder ?? new SqlConnectionStringBuilder();
        builder.DataSource = server!;
        builder.InitialCatalog = database!;
        builder.UserID = user!;
        builder.Password = password!;
        builder.TrustServerCertificate = true;

        if (string.IsNullOrWhiteSpace(builder.ApplicationName))
        {
            builder.ApplicationName = "AlfaCore";
        }

        if (builder.IntegratedSecurity)
        {
            builder.IntegratedSecurity = false;
        }

        if (prompted)
        {
            TrySaveToProductionConfig(builder.ConnectionString, contentRootPath);
        }

        return builder.ConnectionString;
    }

    private static void TrySaveToProductionConfig(string connectionString, string contentRootPath)
    {
        try
        {
            var path = Path.Combine(contentRootPath, "appsettings.Production.json");
            var escaped = connectionString.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var json = $$"""
                {
                  "ConnectionStrings": {
                    "AlfaGestion": "{{escaped}}"
                  }
                }
                """;
            File.WriteAllText(path, json);
            Console.WriteLine($"Configuracion guardada en {path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Advertencia: no se pudo guardar la configuracion: {ex.Message}");
        }
    }

    private static Dictionary<string, string> ParseArguments(IEnumerable<string> args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var raw = string.Join(" ", args).Trim();

        if (string.IsNullOrWhiteSpace(raw))
        {
            return values;
        }

        var normalized = raw.Replace("alfa@", string.Empty, StringComparison.OrdinalIgnoreCase);
        var segments = normalized.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var segment in segments)
        {
            var cleaned = segment.Trim();
            if (cleaned.StartsWith('@'))
            {
                cleaned = cleaned[1..];
            }

            var separatorIndex = cleaned.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = cleaned[..separatorIndex].Trim();
            var value = cleaned[(separatorIndex + 1)..].Trim();

            if (!string.IsNullOrWhiteSpace(key))
            {
                values[key] = value;
            }
        }

        return values;
    }

    private static SqlConnectionStringBuilder? TryCreateBuilder(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        try
        {
            return new SqlConnectionStringBuilder(connectionString);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetValue(IReadOnlyDictionary<string, string> values, string key)
        => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

    private static bool IsComplete(string? server, string? database, string? user, string? password)
        => !string.IsNullOrWhiteSpace(server)
        && !string.IsNullOrWhiteSpace(database)
        && !string.IsNullOrWhiteSpace(user)
        && !string.IsNullOrWhiteSpace(password);

    private static string PromptRequired(string label, string? currentValue, bool secret = false)
    {
        while (true)
        {
            Console.Write($"{label}");
            if (!string.IsNullOrWhiteSpace(currentValue))
            {
                Console.Write($" [{currentValue}]");
            }

            Console.Write(": ");
            var value = secret ? ReadSecret() : Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            if (!string.IsNullOrWhiteSpace(currentValue))
            {
                return currentValue;
            }
        }
    }

    private static string ReadSecret()
    {
        var chars = new List<char>();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return new string(chars.ToArray());
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (chars.Count > 0)
                {
                    chars.RemoveAt(chars.Count - 1);
                    Console.Write("\b \b");
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                chars.Add(key.KeyChar);
                Console.Write('*');
            }
        }
    }
}
