namespace DashboardCompras.Models;

public sealed class VentasDashboardDto
{
    public decimal TotalFacturadoMes { get; init; }
    public decimal TicketPromedioMes { get; init; }
    public int ComprobantesMes { get; init; }
    public int ClientesActivos { get; init; }
    public IReadOnlyList<MonthlyPointDto> EvolucionMensual { get; init; } = [];
    public IReadOnlyList<CategoryTotalDto> TopClientes { get; init; } = [];
    public IReadOnlyList<CategoryTotalDto> TopArticulos { get; init; } = [];
    public IReadOnlyList<GestionMovimientoDto> UltimosComprobantes { get; init; } = [];
}

public sealed class StockDashboardDto
{
    public decimal StockValorizado { get; init; }
    public int ArticulosConStock { get; init; }
    public int BajoPuntoPedido { get; init; }
    public int SinStock { get; init; }
    public IReadOnlyList<MonthlyPointDto> EvolucionMensual { get; init; } = [];
    public IReadOnlyList<CategoryTotalDto> TopMovimientos { get; init; } = [];
    public IReadOnlyList<StockCriticoDto> Criticos { get; init; } = [];
}

public sealed class CajaBancosDashboardDto
{
    public decimal SaldoCajas { get; init; }
    public decimal SaldoBancos { get; init; }
    public decimal Ingresos7Dias { get; init; }
    public decimal Egresos7Dias { get; init; }
    public decimal PendienteCobro { get; init; }
    public decimal PendientePago { get; init; }
    public IReadOnlyList<MonthlyPointDto> EvolucionDiaria { get; init; } = [];
    public IReadOnlyList<CategoryTotalDto> TopCajas { get; init; } = [];
    public IReadOnlyList<CategoryTotalDto> TopBancos { get; init; } = [];
}

public sealed class ContabilidadDashboardDto
{
    public decimal DebeMes { get; init; }
    public decimal HaberMes { get; init; }
    public decimal SaldoNetoMes { get; init; }
    public int AsientosMes { get; init; }
    public IReadOnlyList<MonthlyPointDto> EvolucionMensual { get; init; } = [];
    public IReadOnlyList<CategoryTotalDto> TopCuentas { get; init; } = [];
    public IReadOnlyList<ContabilidadAsientoResumenDto> UltimosAsientos { get; init; } = [];
}

public sealed class GestionMovimientoDto
{
    public DateTime Fecha { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public string Referencia { get; init; } = string.Empty;
    public decimal Total { get; init; }
}

public sealed class StockCriticoDto
{
    public string IdArticulo { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public decimal StockActual { get; init; }
    public decimal PuntoPedido { get; init; }
    public decimal Valorizado { get; init; }
}

public sealed class ContabilidadAsientoResumenDto
{
    public DateTime Fecha { get; init; }
    public string Cuenta { get; init; } = string.Empty;
    public string Detalle { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public decimal Importe { get; init; }
}

public sealed class VentasClienteResumenDto
{
    public string Cuenta { get; init; } = string.Empty;
    public string Cliente { get; init; } = string.Empty;
    public decimal TotalFacturado { get; init; }
    public decimal Participacion { get; init; }
    public int CantidadComprobantes { get; init; }
    public decimal TicketPromedio { get; init; }
    public DateTime? UltimaVenta { get; init; }
}

public sealed class VentasClientesPageDto
{
    public decimal TotalFacturado { get; init; }
    public int ClientesActivos { get; init; }
    public decimal TicketPromedio { get; init; }
    public IReadOnlyList<CategoryTotalDto> TopClientes { get; init; } = [];
    public IReadOnlyList<VentasClienteResumenDto> Clientes { get; init; } = [];
}

public sealed class VentasRubroResumenDto
{
    public string Rubro { get; init; } = string.Empty;
    public decimal TotalVendido { get; init; }
    public decimal Participacion { get; init; }
    public int CantidadArticulos { get; init; }
    public int CantidadComprobantes { get; init; }
}

public sealed class VentasRubrosPageDto
{
    public decimal TotalVendido { get; init; }
    public int RubrosActivos { get; init; }
    public IReadOnlyList<CategoryTotalDto> TopRubros { get; init; } = [];
    public IReadOnlyList<VentasRubroResumenDto> Rubros { get; init; } = [];
}

public sealed class VentasFamiliaResumenDto
{
    public string Familia { get; init; } = string.Empty;
    public decimal TotalVendido { get; init; }
    public decimal Participacion { get; init; }
    public int CantidadArticulos { get; init; }
    public int CantidadComprobantes { get; init; }
}

public sealed class VentasFamiliasPageDto
{
    public decimal TotalVendido { get; init; }
    public int FamiliasActivas { get; init; }
    public IReadOnlyList<CategoryTotalDto> TopFamilias { get; init; } = [];
    public IReadOnlyList<VentasFamiliaResumenDto> Familias { get; init; } = [];
}

public sealed class VentasArticuloResumenDto
{
    public string IdArticulo { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public decimal CantidadVendida { get; init; }
    public decimal TotalVendido { get; init; }
    public int CantidadComprobantes { get; init; }
    public DateTime? UltimaVenta { get; init; }
}

public sealed class VentasArticulosPageDto
{
    public decimal TotalVendido { get; init; }
    public int ArticulosActivos { get; init; }
    public decimal CantidadVendida { get; init; }
    public IReadOnlyList<CategoryTotalDto> TopPorTotal { get; init; } = [];
    public IReadOnlyList<CategoryTotalDto> TopPorCantidad { get; init; } = [];
    public IReadOnlyList<VentasArticuloResumenDto> Articulos { get; init; } = [];
}

public sealed class VentasComprobanteResumenDto
{
    public string Tc { get; init; } = string.Empty;
    public string IdComprobante { get; init; } = string.Empty;
    public DateTime Fecha { get; init; }
    public string Cuenta { get; init; } = string.Empty;
    public string Cliente { get; init; } = string.Empty;
    public decimal Importe { get; init; }
    public string Usuario { get; init; } = string.Empty;
}

public sealed class VentasComprobanteItemDto
{
    public string IdArticulo { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public decimal Cantidad { get; init; }
    public decimal PrecioNeto { get; init; }
    public decimal TotalConIVA { get; init; }
    public decimal CostoUnit { get; init; }
    public string Rubro { get; init; } = string.Empty;
}

public sealed class VentasComprobantesPageDto
{
    public decimal TotalImporte { get; init; }
    public int TotalComprobantes { get; init; }
    public int ClientesActivos { get; init; }
    public decimal TicketPromedio { get; init; }
    public bool HayMasResultados { get; init; }
    public IReadOnlyList<VentasComprobanteResumenDto> Comprobantes { get; init; } = [];
}

public sealed class VentasResumenTcDto
{
    public string Tc { get; init; } = string.Empty;
    public int Cantidad { get; init; }
    public decimal NetoGravado { get; init; }
    public decimal NetoNoGravado { get; init; }
    public decimal Iva21 { get; init; }
    public decimal Iva105 { get; init; }
    public decimal IvaRec { get; init; }
    public decimal RetIIBB { get; init; }
    public decimal RetGanancias { get; init; }
    public decimal RetIVA { get; init; }
    public decimal Total { get; init; }
}

public sealed class VentasResumenTcPageDto
{
    public int TotalComprobantes { get; init; }
    public decimal TotalGeneral { get; init; }
    public IReadOnlyList<VentasResumenTcDto> Filas { get; init; } = [];
}
