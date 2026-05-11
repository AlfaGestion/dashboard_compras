namespace AlfaCore.Models;

public sealed class UsuariosFilters
{
    public string Texto { get; set; } = string.Empty;
    public bool? Activo { get; set; } = true;
    public bool? EsGrupo { get; set; }
    public int MaxRows { get; set; } = 250;
}

public sealed class UsuarioGridItemDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EsGrupo { get; set; }
    public bool CambiarProximoInicio { get; set; }
    public bool Activo { get; set; } = true;
    public bool TieneFoto { get; set; }
    public DateTime? FechaHoraGrabacion { get; set; }
    public DateTime? FechaHoraModificacion { get; set; }
}

public sealed class UsuarioDetailDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EsGrupo { get; set; }
    public bool CambiarProximoInicio { get; set; }
    public bool Activo { get; set; } = true;
    public string ContrasenaDecodificada { get; set; } = string.Empty;
    public bool TieneFoto { get; set; }
    public string FotoCacheToken { get; set; } = string.Empty;
    public DateTime? FechaHoraGrabacion { get; set; }
    public DateTime? FechaHoraModificacion { get; set; }
}

public sealed class UsuarioSaveRequest
{
    public string NombreOriginal { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EsGrupo { get; set; }
    public bool CambiarProximoInicio { get; set; }
    public string Contrasena { get; set; } = string.Empty;
    public byte[]? FotoContenido { get; set; }
    public string FotoNombreOriginal { get; set; } = string.Empty;
    public string FotoMimeType { get; set; } = string.Empty;
    public bool QuitarFoto { get; set; }
}

public sealed class UsuarioPhotoServeDto
{
    public string RutaCompleta { get; set; } = string.Empty;
    public string MimeType { get; set; } = "image/jpeg";
    public string NombreArchivo { get; set; } = string.Empty;
}
