using System.Text.Json;

namespace AlfaCoreShell;

internal sealed class LauncherOptions
{
    public required string Title { get; init; }
    public required Uri TargetUri { get; init; }
    public required string BackendExecutablePath { get; init; }
    public required IReadOnlyList<string> ForwardedBackendArgs { get; init; }
    public bool StartLocalBackend { get; init; }

    public static LauncherOptions Load(string[] args, string baseDirectory)
    {
        var parser = new ArgumentParser(args);
        var config = BackendConfig.Load(baseDirectory);

        var protocol = parser.Protocol ?? config.Protocol ?? "http";
        var port = parser.Port ?? config.Port ?? 5055;
        var host = parser.Host;
        var urlText = parser.Url;

        if (string.IsNullOrWhiteSpace(urlText))
        {
            if (!string.IsNullOrWhiteSpace(host))
            {
                urlText = $"{protocol}://{host}:{port}";
            }
            else if (parser.RemoteMode && !string.IsNullOrWhiteSpace(config.PublicUrl))
            {
                urlText = config.PublicUrl;
            }
            else
            {
                urlText = $"{protocol}://localhost:{port}";
            }
        }

        return new LauncherOptions
        {
            Title = parser.Title ?? config.ApplicationName ?? "AlfaCore - Alfa Gestión",
            TargetUri = new Uri(urlText, UriKind.Absolute),
            BackendExecutablePath = Path.Combine(baseDirectory, "AlfaCore.exe"),
            ForwardedBackendArgs = parser.ForwardedBackendArgs,
            StartLocalBackend = parser.LocalMode || (!parser.RemoteMode && string.IsNullOrWhiteSpace(parser.Url) && string.IsNullOrWhiteSpace(parser.Host))
        };
    }

    private sealed class BackendConfig
    {
        public string? ApplicationName { get; init; }
        public string? Protocol { get; init; }
        public int? Port { get; init; }
        public string? PublicUrl { get; init; }

        public static BackendConfig Load(string baseDirectory)
        {
            var productionPath = Path.Combine(baseDirectory, "appsettings.Production.json");
            var defaultPath = Path.Combine(baseDirectory, "appsettings.json");
            var configPath = File.Exists(productionPath) ? productionPath : defaultPath;

            if (!File.Exists(configPath))
            {
                return new BackendConfig();
            }

            using var stream = File.OpenRead(configPath);
            using var document = JsonDocument.Parse(stream);

            if (!document.RootElement.TryGetProperty("ServidorWeb", out var serverElement))
            {
                return new BackendConfig();
            }

            return new BackendConfig
            {
                ApplicationName = TryGetString(serverElement, "NombreAplicacion"),
                Protocol = TryGetString(serverElement, "Protocolo"),
                Port = TryGetInt(serverElement, "Puerto"),
                PublicUrl = TryGetString(serverElement, "UrlBasePublica")
            };
        }

        private static string? TryGetString(JsonElement element, string propertyName)
            => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        private static int? TryGetInt(JsonElement element, string propertyName)
            => element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var number)
                ? number
                : null;
    }

    private sealed class ArgumentParser
    {
        private readonly List<string> _forwardedBackendArgs = [];

        public ArgumentParser(IEnumerable<string> args)
        {
            foreach (var arg in args)
            {
                if (TryConsumeOption(arg, "--url=", out var url))
                {
                    Url = url;
                    continue;
                }

                if (TryConsumeOption(arg, "--server=", out var host))
                {
                    Host = host;
                    continue;
                }

                if (TryConsumeOption(arg, "--protocol=", out var protocol))
                {
                    Protocol = protocol;
                    continue;
                }

                if (TryConsumeOption(arg, "--port=", out var portText) && int.TryParse(portText, out var port))
                {
                    Port = port;
                    continue;
                }

                if (TryConsumeOption(arg, "--title=", out var title))
                {
                    Title = title;
                    continue;
                }

                if (string.Equals(arg, "--remote", StringComparison.OrdinalIgnoreCase))
                {
                    RemoteMode = true;
                    continue;
                }

                if (string.Equals(arg, "--local", StringComparison.OrdinalIgnoreCase))
                {
                    LocalMode = true;
                    continue;
                }

                if (Uri.TryCreate(arg, UriKind.Absolute, out var absoluteUri)
                    && (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
                {
                    Url = absoluteUri.ToString();
                    continue;
                }

                _forwardedBackendArgs.Add(arg);
            }
        }

        public string? Url { get; }
        public string? Host { get; }
        public string? Protocol { get; }
        public int? Port { get; }
        public string? Title { get; }
        public bool RemoteMode { get; }
        public bool LocalMode { get; }
        public IReadOnlyList<string> ForwardedBackendArgs => _forwardedBackendArgs;

        private static bool TryConsumeOption(string arg, string prefix, out string? value)
        {
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                value = arg[prefix.Length..].Trim().Trim('"');
                return true;
            }

            value = null;
            return false;
        }
    }
}
