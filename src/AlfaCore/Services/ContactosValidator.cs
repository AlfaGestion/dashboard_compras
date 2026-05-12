using AlfaCore.Models;
using Microsoft.Data.SqlClient;
using System.Net.Mail;

namespace AlfaCore.Services;

public sealed class ContactosValidator(
    IConfiguration configuration,
    ISessionService sessionService) : IContactosValidator
{
    private string ConnectionString => sessionService.GetConnectionString().Length > 0
        ? sessionService.GetConnectionString()
        : configuration.GetConnectionString("AlfaGestion")
          ?? throw new InvalidOperationException("No se configuró la cadena de conexión 'ConnectionStrings:AlfaGestion'.");

    public async Task<ValidationResult> ValidateForSaveAsync(ContactoSaveRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = new ValidationResult();
        var nombre = (request.NombreApellido ?? string.Empty).Trim();
        var domicilio = (request.Domicilio ?? string.Empty).Trim();
        var localidad = (request.Localidad ?? string.Empty).Trim();
        var provincia = (request.ProvinciaCodigo ?? string.Empty).Trim();
        var codigoPostal = (request.CodigoPostal ?? string.Empty).Trim();
        var telefono = (request.Telefono ?? string.Empty).Trim();
        var telefonoAlternativo = (request.TelefonoAlternativo ?? string.Empty).Trim();
        var celular = (request.Celular ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim();
        var website = (request.Website ?? string.Empty).Trim();
        var cargo = (request.Cargo ?? string.Empty).Trim();
        var nroDocumento = (request.NumeroDocumento ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(nombre))
            result.Add("nombre", "El nombre y apellido es obligatorio.");
        else if (nombre.Length > 100)
            result.Add("nombre", "El nombre y apellido no puede superar los 100 caracteres.");

        if (domicilio.Length > 100)
            result.Add("domicilio", "El domicilio no puede superar los 100 caracteres.");

        if (localidad.Length > 100)
            result.Add("localidad", "La localidad no puede superar los 100 caracteres.");

        if (provincia.Length > 4)
            result.Add("provincia", "La provincia no puede superar los 4 caracteres.");

        if (codigoPostal.Length > 20)
            result.Add("codigo-postal", "El código postal no puede superar los 20 caracteres.");

        if (telefono.Length > 100)
            result.Add("telefono", "El teléfono no puede superar los 100 caracteres.");

        if (telefonoAlternativo.Length > 100)
            result.Add("telefono-alternativo", "El teléfono alternativo no puede superar los 100 caracteres.");

        if (celular.Length > 100)
            result.Add("celular", "El celular no puede superar los 100 caracteres.");

        if (email.Length > 100)
            result.Add("email", "El email no puede superar los 100 caracteres.");
        else if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
            result.Add("email", "El email no tiene un formato válido.");

        if (website.Length > 100)
            result.Add("website", "La página web no puede superar los 100 caracteres.");

        if (cargo.Length > 100)
            result.Add("cargo", "El cargo no puede superar los 100 caracteres.");

        if (nroDocumento.Length > 13)
            result.Add("numero-documento", "El número de documento no puede superar los 13 caracteres.");

        if (!result.IsValid || string.IsNullOrWhiteSpace(provincia))
            return result;

        await using var cn = new SqlConnection(ConnectionString);
        await cn.OpenAsync(ct);

        if (!await ProvinciaExistsAsync(cn, provincia, ct))
            result.Add("provincia", "La provincia seleccionada no existe en TA_ESTADOS.");

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

    private static async Task<bool> ProvinciaExistsAsync(SqlConnection cn, string codigo, CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.TA_ESTADOS
            WHERE UPPER(LTRIM(RTRIM(CODIGO))) = @Codigo;
            """;

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Codigo", codigo.Trim().ToUpperInvariant());
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result) > 0;
    }
}
