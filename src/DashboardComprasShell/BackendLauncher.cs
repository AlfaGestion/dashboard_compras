using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace DashboardComprasShell;

internal sealed class BackendLauncher
{
    private readonly LauncherOptions _options;
    private Process? _startedProcess;

    public BackendLauncher(LauncherOptions options)
    {
        _options = options;
    }

    public async Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        if (!_options.StartLocalBackend || !IsLocalTarget(_options.TargetUri))
        {
            return;
        }

        if (await IsReachableAsync(cancellationToken))
        {
            return;
        }

        if (!File.Exists(_options.BackendExecutablePath))
        {
            throw new FileNotFoundException("No se encontró DashboardCompras.exe para iniciar el backend local.", _options.BackendExecutablePath);
        }

        var backendDir = Path.GetDirectoryName(_options.BackendExecutablePath) ?? AppContext.BaseDirectory;
        var logPath = Path.Combine(backendDir, "backend_startup.log");

        var startInfo = new ProcessStartInfo
        {
            FileName = _options.BackendExecutablePath,
            WorkingDirectory = backendDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var arg in _options.ForwardedBackendArgs)
        {
            startInfo.ArgumentList.Add(arg);
        }

        startInfo.Environment["DASHBOARD_NO_BROWSER"] = "1";
        startInfo.Environment["DASHBOARD_NO_PROMPT"] = "1";

        _startedProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException("No se pudo iniciar el backend local.");

        using var logWriter = new StreamWriter(logPath, append: false) { AutoFlush = true };
        _startedProcess.OutputDataReceived += (_, e) => { if (e.Data != null) logWriter.WriteLine(e.Data); };
        _startedProcess.ErrorDataReceived += (_, e) => { if (e.Data != null) logWriter.WriteLine("ERR: " + e.Data); };
        _startedProcess.BeginOutputReadLine();
        _startedProcess.BeginErrorReadLine();

        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_startedProcess.HasExited)
            {
                logWriter.Flush();
                throw new InvalidOperationException(
                    $"El backend local finalizó inesperadamente. Código: {_startedProcess.ExitCode}." +
                    $"{Environment.NewLine}Revisar log: {logPath}");
            }

            if (await IsReachableAsync(cancellationToken))
            {
                return;
            }

            await Task.Delay(500, cancellationToken);
        }

        throw new TimeoutException("El backend local no respondió a tiempo.");
    }

    private async Task<bool> IsReachableAsync(CancellationToken cancellationToken)
    {
        foreach (var host in GetCandidateHosts(_options.TargetUri))
        {
            if (await CanConnectAsync(host, _options.TargetUri.Port, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsLocalTarget(Uri uri)
        => uri.IsLoopback
        || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
        || string.Equals(uri.Host, Environment.MachineName, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> GetCandidateHosts(Uri targetUri)
    {
        if (!IsLocalTarget(targetUri))
        {
            yield return targetUri.Host;
            yield break;
        }

        yield return "localhost";
        yield return "127.0.0.1";
        yield return "::1";
        yield return Environment.MachineName;
    }

    private static async Task<bool> CanConnectAsync(string host, int port, CancellationToken cancellationToken)
    {
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(2));

            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(host, port, linkedCts.Token);
            return tcpClient.Connected;
        }
        catch
        {
            return false;
        }
    }
}
