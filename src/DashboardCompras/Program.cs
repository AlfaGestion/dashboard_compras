using DashboardCompras.Components;
using DashboardCompras.Configuration;
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
        builder.Services.AddScoped<IComprasDashboardService, ComprasDashboardService>();
        builder.Services.AddScoped<IInformesIaService, InformesIaService>();
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

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

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
