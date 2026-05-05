namespace AlfaCore.Services;

public sealed class AppExceptionLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAppEventService appEvents)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await appEvents.LogErrorAsync(
                "HTTP",
                $"{context.Request.Method} {context.Request.Path}",
                ex,
                "Se produjo un error inesperado procesando la solicitud.",
                new
                {
                    context.Request.QueryString,
                    context.TraceIdentifier
                });
            throw;
        }
    }
}
