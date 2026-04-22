namespace DashboardCompras.Models;

public sealed class ConsultaResumenDto
{
    public int Id { get; init; }
    public string Clave { get; init; } = string.Empty;
    public string Grupo { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public bool TieneParametros { get; init; }
}

public sealed class ConsultaGuardadaDto
{
    public int Id { get; init; }
    public string Clave { get; init; } = string.Empty;
    public string Grupo { get; init; } = string.Empty;      // GRUPO = nombre visible del nodo
    public string Descripcion { get; init; } = string.Empty; // DESCRIPCION = ayuda al usuario final
    public string Sql { get; init; } = string.Empty;
    public string Tabla { get; init; } = string.Empty;
    public string CamposTotaliza { get; init; } = string.Empty;
    public string CamposOrdenar { get; init; } = string.Empty;
    public IReadOnlyList<ParametroConsultaDto> Parametros { get; init; } = [];
    public bool TieneParametros => Parametros.Count > 0;
}

// Un parámetro es un campo de V_TA_SCRIPT_CFG con EsParametro = 1.
// El label es CampoSel; el valor lo ingresa el usuario en tiempo de ejecución.
public sealed class ParametroConsultaDto
{
    public int Orden { get; init; }
    public string Campo { get; init; } = string.Empty;
    public TipoParametro Tipo { get; init; }
}

public enum TipoParametro { Texto, Fecha, Numero }

public sealed class GrupoConsultasDto
{
    public string Nombre { get; init; } = string.Empty;
    public IReadOnlyList<ConsultaResumenDto> Consultas { get; init; } = [];
}

// Nodo del árbol jerárquico basado en prefijos de CLAVE (2 dígitos por nivel).
// Nombre = campo GRUPO de la base (display name del nodo).
// Descripcion = campo DESCRIPCION de la base (ayuda al usuario final).
public sealed class NodoArbolDto
{
    public int Id { get; init; }
    public string Clave { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public bool TieneParametros { get; init; }
    public bool TieneSql { get; init; }
    public List<NodoArbolDto> Hijos { get; } = [];
}

public sealed class ConsultaResultadoDto
{
    public bool Exitoso { get; init; }
    public string? MensajeError { get; init; }
    public IReadOnlyList<string> Columnas { get; init; } = [];
    public IReadOnlyList<string[]> Filas { get; init; } = [];
    public int TotalFilas { get; init; }
    public bool TieneMasFilas { get; init; }
    public TimeSpan TiempoEjecucion { get; init; }
    public DateTime EjecutadoEn { get; init; }
}

public sealed class EjecutarConsultaRequest
{
    public int ConsultaId { get; set; }
    public List<string> ValoresParametros { get; set; } = [];
    public int MaxFilas { get; set; } = 500;
}

public sealed class GuardarConsultaRequest
{
    public int? Id { get; set; }
    public string Grupo { get; set; } = string.Empty;       // GRUPO = nombre visible del nodo
    public string Clave { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty; // DESCRIPCION = ayuda al usuario
    public string Sql { get; set; } = string.Empty;
    public string Tabla { get; set; } = string.Empty;
    public string CamposTotaliza { get; set; } = string.Empty;
    public string CamposOrdenar { get; set; } = string.Empty;
    public List<string> EtiquetasParametros { get; set; } = [];
}

public sealed class ColumnaDto
{
    public string Nombre { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
}
