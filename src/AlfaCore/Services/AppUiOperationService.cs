using AlfaCore.Models;
using Microsoft.Data.SqlClient;

namespace AlfaCore.Services;

public sealed class AppUiOperationService : IAppUiOperationService
{
    public async Task<AppUiOperationResult> RunAsync(Func<Task> operation, string fallbackTitle, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            await operation();
            return AppUiOperationResult.Ok();
        }
        catch (Exception ex)
        {
            return AppUiOperationResult.Fail(BuildMessage(ex, fallbackTitle));
        }
    }

    public async Task<AppUiOperationResult<T>> RunAsync<T>(Func<Task<T>> operation, string fallbackTitle, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var value = await operation();
            return AppUiOperationResult<T>.Ok(value);
        }
        catch (Exception ex)
        {
            return AppUiOperationResult<T>.Fail(BuildMessage(ex, fallbackTitle));
        }
    }

    public AppUiMessage BuildMessage(Exception exception, string fallbackTitle)
    {
        if (exception is AppValidationException validationEx)
        {
            return new AppUiMessage
            {
                Severity = AppUiFeedbackSeverity.Warning,
                Title = fallbackTitle,
                Message = validationEx.UserMessage,
                Suggestion = "Revisá los campos marcados y volvé a intentar."
            };
        }

        if (exception is AppUserFacingException appEx)
            return BuildFromKnownException(appEx.UserMessage, appEx.ErrorCode, appEx.InnerException, fallbackTitle);

        if (exception is InvalidOperationException invalidOp &&
            TryExtractCode(invalidOp.Message, out var plainMessage, out var code))
        {
            return BuildFromKnownException(plainMessage, code, invalidOp.InnerException, fallbackTitle);
        }

        return new AppUiMessage
        {
            Severity = AppUiFeedbackSeverity.Error,
            Title = fallbackTitle,
            Message = "Ocurrió un problema inesperado. Si persiste, revisá el detalle técnico registrado por el sistema.",
            Suggestion = "Volvé a intentar la operación y, si sigue fallando, compartí el código de error con soporte."
        };
    }

    private static AppUiMessage BuildFromKnownException(string userMessage, string code, Exception? innerException, string fallbackTitle)
    {
        var sqlException = FindSqlException(innerException);
        var win32Code = FindInnermostWin32Code(innerException);

        if (sqlException?.Number is 53 or 64 || win32Code is 53 or 64)
        {
            return new AppUiMessage
            {
                Severity = AppUiFeedbackSeverity.Error,
                Title = "No se pudo conectar a la base activa",
                Message = "La sesión SQL seleccionada no está accesible en este momento. El sistema no pudo llegar al servidor o a la instancia configurada.",
                Code = code,
                Suggestion = "Revisá la sesión activa, el nombre del servidor SQL, la instancia, la red y las credenciales antes de volver a intentar."
            };
        }

        if (sqlException?.Number == 208)
        {
            return new AppUiMessage
            {
                Severity = AppUiFeedbackSeverity.Error,
                Title = fallbackTitle,
                Message = userMessage,
                Code = code,
                Suggestion = "La base activa parece no tener completo el esquema esperado. Revisá los scripts de inicialización del módulo."
            };
        }

        if (!string.IsNullOrWhiteSpace(userMessage))
        {
            return new AppUiMessage
            {
                Severity = AppUiFeedbackSeverity.Error,
                Title = fallbackTitle,
                Message = userMessage,
                Code = code,
                Suggestion = string.IsNullOrWhiteSpace(code)
                    ? "Volvé a intentar la operación. Si persiste, revisá el log del sistema."
                    : "Si el problema persiste, compartí el código con soporte para revisar el incidente."
            };
        }

        return new AppUiMessage
        {
            Severity = AppUiFeedbackSeverity.Error,
            Title = fallbackTitle,
            Message = "No fue posible completar la operación solicitada.",
            Code = code,
            Suggestion = "Volvé a intentar la operación. Si persiste, revisá el log del sistema."
        };
    }

    private static bool TryExtractCode(string message, out string plainMessage, out string code)
    {
        plainMessage = message?.Trim() ?? string.Empty;
        code = string.Empty;
        if (string.IsNullOrWhiteSpace(plainMessage))
            return false;

        var markerIndex = plainMessage.LastIndexOf("Código:", StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
            markerIndex = plainMessage.LastIndexOf("Codigo:", StringComparison.OrdinalIgnoreCase);

        if (markerIndex < 0)
            return false;

        code = plainMessage[(markerIndex + 7)..].Trim();
        plainMessage = plainMessage[..markerIndex].Trim().TrimEnd('.', ':');
        return !string.IsNullOrWhiteSpace(code);
    }

    private static SqlException? FindSqlException(Exception? exception)
    {
        var current = exception;
        while (current is not null)
        {
            if (current is SqlException sqlException)
                return sqlException;
            current = current.InnerException;
        }

        return null;
    }

    private static int? FindInnermostWin32Code(Exception? exception)
    {
        var current = exception;
        while (current is not null)
        {
            if (current is System.ComponentModel.Win32Exception win32)
                return win32.NativeErrorCode;
            current = current.InnerException;
        }

        return null;
    }
}
