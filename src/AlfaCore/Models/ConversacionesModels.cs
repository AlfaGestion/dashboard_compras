using System.Text.Json;

namespace AlfaCore.Models;

public sealed class ConversacionesInboxFilters
{
    public string Modo { get; set; } = "todas";
    public string Search { get; set; } = string.Empty;
    public string? IdTecnicoActual { get; set; }
    public string? CodigoEstado { get; set; }
    public string? Canal { get; set; }
    public int Limit { get; set; } = 50;
    public int Offset { get; set; }
}

public sealed class ConversacionInboxItemDto
{
    public long IdConversacion { get; set; }
    public string TelefonoWhatsApp { get; set; } = string.Empty;
    public string NombreVisible { get; set; } = string.Empty;
    public string ClienteCodigo { get; set; } = string.Empty;
    public string ClienteNombre { get; set; } = string.Empty;
    public int? IdContacto { get; set; }
    public string ContactoNombre { get; set; } = string.Empty;
    public string CodigoEstado { get; set; } = string.Empty;
    public string EstadoDescripcion { get; set; } = string.Empty;
    public string IdTecnico { get; set; } = string.Empty;
    public string TecnicoNombre { get; set; } = string.Empty;
    public string ResumenUltimoMensaje { get; set; } = string.Empty;
    public DateTime FechaHoraUltimoMensaje { get; set; }
    public bool Archivada { get; set; }
    public bool Bloqueada { get; set; }
}

public sealed class ConversacionDetalleDto
{
    public long IdConversacion { get; set; }
    public string Canal { get; set; } = string.Empty;
    public string TelefonoWhatsApp { get; set; } = string.Empty;
    public string NombreVisible { get; set; } = string.Empty;
    public string ClienteCodigo { get; set; } = string.Empty;
    public string ClienteNombre { get; set; } = string.Empty;
    public int? IdContacto { get; set; }
    public string ContactoNombre { get; set; } = string.Empty;
    public string ContactoTelefono { get; set; } = string.Empty;
    public string ContactoCelular { get; set; } = string.Empty;
    public string ContactoEmail { get; set; } = string.Empty;
    public string ContactoCargo { get; set; } = string.Empty;
    public string CodigoEstado { get; set; } = string.Empty;
    public string EstadoDescripcion { get; set; } = string.Empty;
    public string IdTecnico { get; set; } = string.Empty;
    public string TecnicoNombre { get; set; } = string.Empty;
    public string ResumenUltimoMensaje { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public bool Archivada { get; set; }
    public bool Bloqueada { get; set; }
    public DateTime? FechaHoraPrimerMensaje { get; set; }
    public DateTime FechaHoraUltimoMensaje { get; set; }
    public DateTime? FechaHoraCierre { get; set; }
}

public sealed class ConversacionMensajeDto
{
    public long IdMensaje { get; set; }
    public long IdConversacion { get; set; }
    public string TelefonoWhatsApp { get; set; } = string.Empty;
    public string WhatsAppMessageId { get; set; } = string.Empty;
    public string WhatsAppReplyToMessageId { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string EstadoEnvio { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public string UsuarioAutor { get; set; } = string.Empty;
    public string SistemaAutor { get; set; } = string.Empty;
    public string IdTecnicoAutor { get; set; } = string.Empty;
    public string TecnicoAutorNombre { get; set; } = string.Empty;
    public bool TieneAdjuntos { get; set; }
}

public sealed class ConversacionAdjuntoDto
{
    public long IdAdjunto { get; set; }
    public long IdMensaje { get; set; }
    public string TipoArchivo { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string UrlArchivo { get; set; } = string.Empty;
    public string RutaLocal { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
}

public sealed class ConversacionSendMessageRequest
{
    public long IdConversacion { get; set; }
    public string Texto { get; set; } = string.Empty;
    public string MessageType { get; set; } = "TEXT";
    public string? WhatsAppReplyToMessageId { get; set; }
    public string? IdTecnicoAutor { get; set; }
    public string? UsuarioAccion { get; set; }
    public string? SistemaAccion { get; set; }
}

public sealed class ConversacionNotaInternaRequest
{
    public long IdConversacion { get; set; }
    public string Texto { get; set; } = string.Empty;
    public string? IdTecnicoAutor { get; set; }
    public string? UsuarioAccion { get; set; }
    public string? SistemaAccion { get; set; }
}

public sealed class ConversacionAsignacionRequest
{
    public long IdConversacion { get; set; }
    public string? IdTecnico { get; set; }
    public string? UsuarioAccion { get; set; }
    public string? SistemaAccion { get; set; }
    public string? Observaciones { get; set; }
}

public sealed class ConversacionEstadoRequest
{
    public long IdConversacion { get; set; }
    public string CodigoEstado { get; set; } = string.Empty;
    public string? UsuarioAccion { get; set; }
    public string? SistemaAccion { get; set; }
    public string? Observaciones { get; set; }
}

public sealed class ConversacionMessageResultDto
{
    public long IdMensaje { get; set; }
    public string EstadoEnvio { get; set; } = string.Empty;
    public string WhatsAppMessageId { get; set; } = string.Empty;
}

public sealed class ConversacionWebhookResultDto
{
    public long IdWebhookLog { get; set; }
    public int MensajesDetectados { get; set; }
    public int MensajesProcesados { get; set; }
}

public sealed class ConversacionUploadAdjuntoRequest
{
    public long IdConversacion { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string TipoArchivo { get; set; } = string.Empty;
    public Stream Contenido { get; set; } = Stream.Null;
    public long TamanoBytes { get; set; }
    public string? IdTecnicoAutor { get; set; }
    public string? UsuarioAccion { get; set; }
    public string? SistemaAccion { get; set; }
}

public sealed class ConversacionAdjuntoServeDto
{
    public string RutaLocal { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
}

public sealed class ConversacionCrearHiloInternoRequest
{
    public string NombreHilo { get; set; } = string.Empty;
    public string? IdTecnico { get; set; }
    public string? UsuarioAccion { get; set; }
    public string? SistemaAccion { get; set; }
}

public sealed class ConversacionWebhookRequest
{
    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class ConversacionTecnicoOptionDto
{
    public string IdTecnico { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string UsuarioAsociado { get; set; } = string.Empty;
    public string SistemaAsociado { get; set; } = string.Empty;
}

public sealed class ConversacionEstadoOptionDto
{
    public string CodigoEstado { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public bool EsCerrado { get; set; }
    public int Orden { get; set; }
}
