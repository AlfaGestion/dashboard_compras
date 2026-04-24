using DashboardCompras.Components;
using DashboardCompras.Configuration;
using DashboardCompras.Models;
using DashboardCompras.Services;
using System.Diagnostics;

namespace DashboardCompras;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        DotEnvLoader.LoadIfPresent(builder.Environment.ContentRootPath);
        var startupConnectionString = StartupConnectionResolver.Resolve(args, builder.Configuration, builder.Environment.ContentRootPath);

        if (!string.IsNullOrWhiteSpace(startupConnectionString))
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AlfaGestion"] = startupConnectionString
            });
        }

        var serverOptions = builder.Configuration.GetSection(ServidorWebOptions.SectionName).Get<ServidorWebOptions>() ?? new();

        builder.Host.UseWindowsService(options =>
        {
            options.ServiceName = "DashboardCompras";
        });

        builder.WebHost.ConfigureKestrel(options =>
        {
            if (serverOptions.EscucharEnRed)
            {
                options.ListenAnyIP(serverOptions.Puerto);
            }
            else
            {
                options.ListenLocalhost(serverOptions.Puerto);
            }
        });

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddSingleton<ISessionService, SessionService>();
        builder.Services.AddScoped<IComprasDashboardService, ComprasDashboardService>();
        builder.Services.AddScoped<IInformesIaService, InformesIaService>();
        builder.Services.AddScoped<IConsultasService, ConsultasService>();
        builder.Services.AddScoped<ICostosService, CostosService>();
        builder.Services.AddSingleton<IAppEventService, AppEventService>();
        builder.Services.AddSingleton<ConsultasExcelExporter>();
        builder.Services.AddSingleton<InformesIaHistoryStore>();
        builder.Services.AddSingleton<InformesIaResultStore>();
        builder.Services.AddScoped<FilterStateService>();
        builder.Services.AddHttpClient();
        builder.Services.AddHttpContextAccessor();
        builder.Services.Configure<ServidorWebOptions>(builder.Configuration.GetSection(ServidorWebOptions.SectionName));
        builder.Services.Configure<DatosSqlOptions>(builder.Configuration.GetSection(DatosSqlOptions.SectionName));
        builder.Services.AddHostedService<ServerStartupHostedService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseMiddleware<AppExceptionLoggingMiddleware>();
        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.MapGet("/api/costos/importaciones/{batchId:int}/descargar-archivo", async (
            int batchId,
            ICostosService costosSvc,
            CancellationToken ct) =>
        {
            var detail = await costosSvc.GetBatchDetailAsync(batchId, ct);
            if (detail is null) return Results.NotFound();

            var path = detail.Batch.SourceFilePath;
            if (!File.Exists(path)) return Results.NotFound("El archivo ya no existe en el servidor.");

            var ext = Path.GetExtension(path).ToLowerInvariant();
            var contentType = ext == ".xlsx"
                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                : "text/plain";

            return Results.File(path, contentType, Path.GetFileName(path));
        });

        app.MapGet("/consultas/{id:int}/descargar-excel", async (
            int id,
            HttpRequest request,
            IConsultasService svc,
            ConsultasExcelExporter exporter,
            CancellationToken ct) =>
        {
            var consulta = await svc.GetConsultaAsync(id, ct);
            if (consulta is null) return Results.NotFound();

            var valores = new List<string>();
            for (int i = 0; request.Query.ContainsKey($"p{i}"); i++)
                valores.Add(request.Query[$"p{i}"].ToString());

            var resultado = await svc.EjecutarAsync(new EjecutarConsultaRequest
            {
                ConsultaId = id,
                ValoresParametros = valores,
                MaxFilas = 100_000
            }, ct);

            if (!resultado.Exitoso)
                return Results.BadRequest(resultado.MensajeError);

            var agruparPor = request.Query["agruparPor"].ToString();
            var columnasAgrupadas = request.Query["ga"]
                .Select(v => v ?? string.Empty)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToArray();
            var filasAgrupadas = request.Query["gf"].ToArray()
                .Select(f => (f ?? string.Empty).Split('\u001F'))
                .ToList();

            var exportarAgrupado =
                !string.IsNullOrWhiteSpace(agruparPor) &&
                columnasAgrupadas.Length > 0 &&
                filasAgrupadas.Count > 0;

            var bytes = exportarAgrupado
                ? exporter.ExportarAgrupado(
                    consulta,
                    resultado.EjecutadoEn,
                    columnasAgrupadas,
                    filasAgrupadas,
                    $"Agrupado por {agruparPor}")
                : exporter.Exportar(consulta, resultado);
            var filename = ConsultasExcelExporter.NombreArchivo(consulta);
            return Results.File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        });

        try
        {
            app.Run();
        }
        catch (IOException ex)
        {
            WriteStartupError(
                $"No se pudo iniciar Dashboard de Compras en el puerto {serverOptions.Puerto}. Verificá si el puerto está ocupado o bloqueado.",
                ex);
            throw;
        }
    }

    private static void WriteStartupError(string message, Exception exception)
    {
        var fullMessage = $"{message}{Environment.NewLine}{exception}";

        try
        {
            Console.Error.WriteLine(fullMessage);
        }
        catch
        {
            // Avoid masking the original startup failure if stderr is unavailable.
        }

        try
        {
            Trace.TraceError(fullMessage);
        }
        catch
        {
            // Best-effort diagnostic fallback only.
        }
    }
}
