using DashboardCompras.Models;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace DashboardCompras.Services;

public sealed class SessionService : ISessionService
{
    private readonly string _filePath;
    private readonly List<SessionDto> _sessions = [];
    private readonly object _lock = new();

    public event Action? SessionChanged;

    public SessionService(IConfiguration configuration, IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "App_Data", "sessions.json");
        Load(configuration);
    }

    public string GetConnectionString()
    {
        lock (_lock)
        {
            var active = _sessions.FirstOrDefault(s => s.Activa);
            return active is null ? string.Empty : Build(active);
        }
    }

    public SessionDto? GetActiveSession()
    {
        lock (_lock) return _sessions.FirstOrDefault(s => s.Activa);
    }

    public IReadOnlyList<SessionDto> GetAllSessions()
    {
        lock (_lock) return [.. _sessions];
    }

    public void SwitchSession(Guid id)
    {
        lock (_lock)
        {
            foreach (var s in _sessions) s.Activa = false;
            var target = _sessions.FirstOrDefault(s => s.Id == id)
                ?? throw new InvalidOperationException("Sesión no encontrada.");
            target.Activa = true;
            Save();
        }
        SessionChanged?.Invoke();
    }

    public void AddSession(string nombre, string servidor, string baseDatos, string usuario, string password)
    {
        lock (_lock)
        {
            _sessions.Add(new SessionDto
            {
                Nombre = nombre.Trim(),
                Servidor = servidor.Trim(),
                BaseDatos = baseDatos.Trim(),
                Usuario = usuario.Trim(),
                Password = password,
                TrustServerCertificate = true,
                Activa = false
            });
            Save();
        }
    }

    public void DeleteSession(Guid id)
    {
        lock (_lock)
        {
            var s = _sessions.FirstOrDefault(s => s.Id == id);
            if (s is null || s.Activa) return;
            _sessions.Remove(s);
            Save();
        }
    }

    private void Load(IConfiguration configuration)
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize<SessionesData>(json, JsonOpts);
                if (data?.Sessions.Count > 0)
                {
                    _sessions.AddRange(data.Sessions);
                    if (!_sessions.Any(s => s.Activa))
                        _sessions[0].Activa = true;
                    return;
                }
            }
        }
        catch { /* archivo corrupto → seed desde config */ }

        SeedFromConfig(configuration);
    }

    private void SeedFromConfig(IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("AlfaGestion") ?? string.Empty;
        SessionDto seed;

        try
        {
            var b = new SqlConnectionStringBuilder(cs);
            seed = new SessionDto
            {
                Nombre = $"{b.DataSource} · {b.InitialCatalog}",
                Servidor = b.DataSource,
                BaseDatos = b.InitialCatalog,
                Usuario = b.UserID,
                Password = b.Password,
                TrustServerCertificate = true,
                Activa = true
            };
        }
        catch
        {
            seed = new SessionDto { Nombre = "Sesión inicial", Activa = true };
        }

        _sessions.Add(seed);
        Save();
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var json = JsonSerializer.Serialize(new SessionesData { Sessions = _sessions }, JsonOpts);
            File.WriteAllText(_filePath, json);
        }
        catch { /* no bloquear si no puede escribir */ }
    }

    private static string Build(SessionDto s) =>
        new SqlConnectionStringBuilder
        {
            DataSource = s.Servidor,
            InitialCatalog = s.BaseDatos,
            UserID = s.Usuario,
            Password = s.Password,
            TrustServerCertificate = s.TrustServerCertificate,
            ApplicationName = "DashboardCompras"
        }.ConnectionString;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
}
