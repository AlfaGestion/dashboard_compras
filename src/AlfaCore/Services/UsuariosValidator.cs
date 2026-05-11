using AlfaCore.Models;
using Microsoft.Data.SqlClient;
using System.Net.Mail;

namespace AlfaCore.Services;

public sealed class UsuariosValidator(
    IConfiguration configuration,
    ISessionService sessionService) : IUsuariosValidator
{
    private const string SistemaFijo = "CN000PR";

    private string ConnectionString => sessionService.GetConnectionString().Length > 0
        ? sessionService.GetConnectionString()
        : configuration.GetConnectionString("AlfaGestion")
          ?? throw new InvalidOperationException("No se configuró la cadena de conexión 'ConnectionStrings:AlfaGestion'.");

    public async Task<ValidationResult> ValidateForSaveAsync(UsuarioSaveRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = new ValidationResult();
        var nombreOriginal = (request.NombreOriginal ?? string.Empty).Trim();
        var nombre = (request.Nombre ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim();
        var contrasena = request.Contrasena ?? string.Empty;
        var esGrupo = request.EsGrupo;
        var cambiarProximoInicio = request.CambiarProximoInicio;

        if (string.IsNullOrWhiteSpace(nombre))
            result.Add("nombre", "El nombre de usuario es obligatorio.");
        else if (nombre.Length > 50)
            result.Add("nombre", "El nombre de usuario no puede superar los 50 caracteres.");

        if (email.Length > 150)
            result.Add("email", "El email no puede superar los 150 caracteres.");
        else if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
            result.Add("email", "El email no tiene un formato válido.");

        if (esGrupo)
        {
            if (!string.IsNullOrWhiteSpace(contrasena))
                result.Add("contrasena", "Los grupos no deben guardar contraseña en este módulo.");

            if (cambiarProximoInicio)
                result.Add("cambiar-proximo-inicio", "Los grupos no usan cambio de contraseña al próximo inicio.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(contrasena))
                result.Add("contrasena", "La contraseña es obligatoria para usuarios comunes.");
            else if (contrasena.Length > 13)
                result.Add("contrasena", "La contraseña no puede superar los 13 caracteres por compatibilidad con la base actual.");
        }

        if (request.FotoContenido is { Length: > 0 })
        {
            var extension = Path.GetExtension(request.FotoNombreOriginal ?? string.Empty).Trim().ToLowerInvariant();
            if (extension is not ".jpg" and not ".jpeg")
                result.Add("foto", "La imagen del usuario debe estar en formato JPG.");
        }

        if (!result.IsValid)
            return result;

        await using var cn = new SqlConnection(ConnectionString);
        await cn.OpenAsync(ct);

        if (string.IsNullOrWhiteSpace(nombreOriginal))
        {
            if (await ExistsAsync(cn, nombre, ct))
                result.Add("nombre", "Ya existe un usuario con ese nombre en el sistema actual.");
        }
        else
        {
            if (!await ExistsAsync(cn, nombreOriginal, ct))
            {
                result.Add(string.Empty, "El usuario seleccionado ya no existe en la base activa.");
                return result;
            }

            if (!string.Equals(nombreOriginal, nombre, StringComparison.OrdinalIgnoreCase) &&
                await ExistsAsync(cn, nombre, ct))
            {
                result.Add("nombre", "Ya existe otro usuario con ese nombre en el sistema actual.");
            }
        }

        return result;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> ExistsAsync(SqlConnection cn, string nombre, CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.TA_USUARIOS
            WHERE UPPER(LTRIM(RTRIM(SISTEMA))) = @Sistema
              AND UPPER(LTRIM(RTRIM(NOMBRE))) = @Nombre;
            """;

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Sistema", SistemaFijo);
        cmd.Parameters.AddWithValue("@Nombre", nombre.Trim().ToUpperInvariant());
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result) > 0;
    }
}
