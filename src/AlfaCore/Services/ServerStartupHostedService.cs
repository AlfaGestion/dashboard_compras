using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using AlfaCore.Configuration;
using Microsoft.Extensions.Options;

namespace AlfaCore.Services;

public sealed class ServerStartupHostedService(
    IHostApplicationLifetime appLifetime,
    IOptions<ServidorWebOptions> options,
    ILogger<ServerStartupHostedService> logger) : IHostedService
{
    private readonly ServidorWebOptions _options = options.Value;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        appLifetime.ApplicationStarted.Register(OnStarted);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void OnStarted()
    {
        var localUrl = $"{_options.Protocolo}://localhost:{_options.Puerto}";
        var hostName = Environment.MachineName;
        var hostUrl = $"{_options.Protocolo}://{hostName}:{_options.Puerto}";
        var openBrowserEnabled = _options.AbrirNavegadorAlIniciar
            && !string.Equals(Environment.GetEnvironmentVariable("DASHBOARD_NO_BROWSER"), "1", StringComparison.Ordinal);

        logger.LogInformation("{App} iniciado correctamente.", _options.NombreAplicacion);
        logger.LogInformation("Acceso local: {Url}", localUrl);
        logger.LogInformation("Acceso red por nombre: {Url}", hostUrl);

        foreach (var ip in GetLanAddresses())
        {
            logger.LogInformation("Acceso red por IP: {Url}", $"{_options.Protocolo}://{ip}:{_options.Puerto}");
        }

        if (!string.IsNullOrWhiteSpace(_options.UrlBasePublica))
        {
            logger.LogInformation("URL pública configurada: {Url}", _options.UrlBasePublica);
        }

        if (openBrowserEnabled && Environment.UserInteractive)
        {
            TryOpenBrowser(localUrl);
        }
    }

    private void TryOpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo abrir automáticamente el navegador. Abrí manualmente {Url}", url);
        }
    }

    private static IEnumerable<string> GetLanAddresses()
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address))
            .Select(a => a.Address.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
