using DashboardCompras.Models;
using Microsoft.Data.SqlClient;

namespace DashboardCompras.Services;

public sealed partial class ComprasDashboardService
{
    public async Task<ActividadPageDto> GetActividadPageAsync(DashboardFilters filters, CancellationToken cancellationToken = default)
    {
        return await MeasureAsync("GetActividadPage", filters, async () =>
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);

            var usuarios = (await ReadListAsync(connection, $"""
                SELECT
                    c.USUARIO AS Usuario,
                    COUNT(*) AS CantidadComprobantes,
                    ISNULL(SUM(ISNULL(det.CantidadItems, 0)), 0) AS CantidadItems,
                    ISNULL(AVG(CAST(ISNULL(det.CantidadItems, 0) AS decimal(18,2))), 0) AS PromedioItemsPorComprobante,
                    ISNULL(SUM(c.ImporteDashboard), 0) AS ImporteTotal,
                    MAX(c.FECHA) AS UltimaActividad,
                    COUNT(DISTINCT CONVERT(date, c.FECHA)) AS DiasConActividad,
                    SUM(CASE WHEN ISNULL(det.CantidadItems, 0) > 0 THEN 1 ELSE 0 END) AS ComprobantesConDetalle,
                    SUM(CASE WHEN ISNULL(det.CantidadItems, 0) = 0 THEN 1 ELSE 0 END) AS ComprobantesContables
                {HeaderWithDetailFromClause}
                GROUP BY c.USUARIO
                ORDER BY COUNT(*) DESC, ISNULL(SUM(ISNULL(det.CantidadItems, 0)), 0) DESC;
                """, filters, reader =>
            {
                var comprobantes = reader.GetInt32("CantidadComprobantes");
                var conDetalle = reader.GetInt32("ComprobantesConDetalle");
                return new ActividadUsuarioResumenDto
                {
                    Usuario = reader.SafeGetString("Usuario"),
                    CantidadComprobantes = comprobantes,
                    CantidadItems = reader.GetInt32("CantidadItems"),
                    PromedioItemsPorComprobante = reader.GetDecimal("PromedioItemsPorComprobante"),
                    ImporteTotal = reader.GetDecimal("ImporteTotal"),
                    UltimaActividad = reader.GetNullableDateTime("UltimaActividad"),
                    DiasConActividad = reader.GetInt32("DiasConActividad"),
                    ComprobantesConDetalle = conDetalle,
                    ComprobantesContables = reader.GetInt32("ComprobantesContables"),
                    PorcentajeConDetalle = comprobantes > 0 ? conDetalle / (decimal)comprobantes : 0m
                };
            }, cancellationToken)).Where(x => !string.IsNullOrWhiteSpace(x.Usuario)).ToList();

            var actividadPorDia = (await ReadListAsync(connection, $"""
                SELECT
                    CONVERT(date, c.FECHA) AS Fecha,
                    COUNT(*) AS CantidadComprobantes,
                    ISNULL(SUM(ISNULL(det.CantidadItems, 0)), 0) AS CantidadItems,
                    ISNULL(SUM(c.ImporteDashboard), 0) AS ImporteTotal
                {HeaderWithDetailFromClause}
                GROUP BY CONVERT(date, c.FECHA)
                ORDER BY CONVERT(date, c.FECHA);
                """, filters, reader => new ActividadDiaDto
            {
                Fecha = reader.GetDateTime("Fecha"),
                CantidadComprobantes = reader.GetInt32("CantidadComprobantes"),
                CantidadItems = reader.GetInt32("CantidadItems"),
                ImporteTotal = reader.GetDecimal("ImporteTotal")
            }, cancellationToken)).ToList();

            var totalComprobantes = usuarios.Sum(x => x.CantidadComprobantes);
            var totalItems = usuarios.Sum(x => x.CantidadItems);
            var usuariosActivos = usuarios.Count;
            var diaMayor = actividadPorDia.OrderByDescending(x => x.CantidadComprobantes).ThenByDescending(x => x.CantidadItems).FirstOrDefault();
            var usuarioMayor = usuarios.OrderByDescending(x => x.CantidadComprobantes).ThenByDescending(x => x.CantidadItems).FirstOrDefault();
            var usuarioMasDetalle = usuarios
                .OrderByDescending(x => x.ComprobantesConDetalle)
                .ThenByDescending(x => x.CantidadItems)
                .FirstOrDefault();

            return new ActividadPageDto
            {
                Kpis = new ActividadKpisDto
                {
                    CantidadComprobantes = totalComprobantes,
                    CantidadItems = totalItems,
                    UsuariosActivos = usuariosActivos,
                    PromedioItemsPorComprobante = totalComprobantes > 0 ? totalItems / (decimal)totalComprobantes : 0m,
                    PromedioComprobantesPorUsuario = usuariosActivos > 0 ? totalComprobantes / (decimal)usuariosActivos : 0m,
                    DiaMayorActividad = diaMayor is null ? "Sin datos" : $"{diaMayor.Fecha:dd/MM/yyyy} · {diaMayor.CantidadComprobantes} comprobantes",
                    UsuarioMasActivo = usuarioMayor is null ? "Sin datos" : $"{usuarioMayor.Usuario} · {usuarioMayor.CantidadComprobantes} comprobantes",
                    UsuarioMasDetalle = usuarioMasDetalle?.Usuario ?? "Sin datos",
                    ComprobantesConDetalleMaximos = usuarioMasDetalle?.ComprobantesConDetalle ?? 0,
                    ItemsDelUsuarioMasDetalle = usuarioMasDetalle?.CantidadItems ?? 0
                },
                Usuarios = usuarios,
                ActividadPorDia = actividadPorDia,
                SerieComprobantesPorDia = actividadPorDia.Select(x => new MonthlyPointDto
                {
                    Periodo = x.Fecha.ToString("dd/MM"),
                    Total = x.CantidadComprobantes
                }).ToList(),
                SerieItemsPorDia = actividadPorDia.Select(x => new MonthlyPointDto
                {
                    Periodo = x.Fecha.ToString("dd/MM"),
                    Total = x.CantidadItems
                }).ToList(),
                ComprobantesPorUsuario = usuarios.Take(10).Select(x => new CategoryTotalDto
                {
                    Categoria = x.Usuario,
                    Codigo = x.Usuario,
                    Total = x.CantidadComprobantes
                }).ToList(),
                ItemsPorUsuario = usuarios.Take(10).Select(x => new CategoryTotalDto
                {
                    Categoria = x.Usuario,
                    Codigo = x.Usuario,
                    Total = x.CantidadItems
                }).ToList(),
                SegmentacionTipoCarga = BuildSegmentacionTipoCarga(usuarios, totalComprobantes),
                Insights = BuildActividadInsights(usuarios, actividadPorDia, totalComprobantes)
            };
        });
    }

    public async Task<ActividadUsuarioDetalleDto?> GetActividadUsuarioDetalleAsync(string usuario, DashboardFilters filters, CancellationToken cancellationToken = default)
    {
        return await MeasureAsync("GetActividadUsuarioDetalle", filters, async () =>
        {
            var scoped = filters.Clone();
            scoped.Usuario = usuario;

            await using var connection = await OpenConnectionAsync(cancellationToken);

            var resumen = (await ReadListAsync(connection, $"""
                SELECT
                    c.USUARIO AS Usuario,
                    COUNT(*) AS CantidadComprobantes,
                    ISNULL(SUM(ISNULL(det.CantidadItems, 0)), 0) AS CantidadItems,
                    ISNULL(AVG(CAST(ISNULL(det.CantidadItems, 0) AS decimal(18,2))), 0) AS PromedioItemsPorComprobante,
                    ISNULL(SUM(c.ImporteDashboard), 0) AS ImporteTotal,
                    MAX(c.FECHA) AS UltimaActividad,
                    COUNT(DISTINCT CONVERT(date, c.FECHA)) AS DiasConActividad,
                    SUM(CASE WHEN ISNULL(det.CantidadItems, 0) > 0 THEN 1 ELSE 0 END) AS ComprobantesConDetalle,
                    SUM(CASE WHEN ISNULL(det.CantidadItems, 0) = 0 THEN 1 ELSE 0 END) AS ComprobantesContables
                {HeaderWithDetailFromClause}
                GROUP BY c.USUARIO;
                """, scoped, reader =>
            {
                var comprobantes = reader.GetInt32("CantidadComprobantes");
                var conDetalle = reader.GetInt32("ComprobantesConDetalle");
                return new ActividadUsuarioResumenDto
                {
                    Usuario = reader.SafeGetString("Usuario"),
                    CantidadComprobantes = comprobantes,
                    CantidadItems = reader.GetInt32("CantidadItems"),
                    PromedioItemsPorComprobante = reader.GetDecimal("PromedioItemsPorComprobante"),
                    ImporteTotal = reader.GetDecimal("ImporteTotal"),
                    UltimaActividad = reader.GetNullableDateTime("UltimaActividad"),
                    DiasConActividad = reader.GetInt32("DiasConActividad"),
                    ComprobantesConDetalle = conDetalle,
                    ComprobantesContables = reader.GetInt32("ComprobantesContables"),
                    PorcentajeConDetalle = comprobantes > 0 ? conDetalle / (decimal)comprobantes : 0m
                };
            }, cancellationToken)).FirstOrDefault();

            if (resumen is null)
            {
                return null;
            }

            var actividadPorDia = await ReadListAsync(connection, $"""
                SELECT
                    CONVERT(date, c.FECHA) AS Fecha,
                    COUNT(*) AS CantidadComprobantes,
                    ISNULL(SUM(ISNULL(det.CantidadItems, 0)), 0) AS CantidadItems
                {HeaderWithDetailFromClause}
                GROUP BY CONVERT(date, c.FECHA)
                ORDER BY CONVERT(date, c.FECHA);
                """, scoped, reader => new ActividadDiaDto
            {
                Fecha = reader.GetDateTime("Fecha"),
                CantidadComprobantes = reader.GetInt32("CantidadComprobantes"),
                CantidadItems = reader.GetInt32("CantidadItems")
            }, cancellationToken);

            var comprobantes = await ReadListAsync(connection, $"""
                SELECT TOP (50)
                    c.TC, c.IDCOMPROBANTE, c.NUMERO, c.FECHA, c.CUENTA, c.RAZON_SOCIAL,
                    c.SUCURSAL, CONVERT(varchar(20), c.IdDeposito) AS Deposito, c.USUARIO,
                    c.NetoDashboard, c.IvaDashboard, c.ImporteDashboard, c.EstadoComprobante,
                    ISNULL(det.CantidadItems, 0) AS CantidadItems,
                    CAST(CASE WHEN ISNULL(det.CantidadItems, 0) > 0 THEN 1 ELSE 0 END AS bit) AS TieneDetalle,
                    CAST(CASE WHEN ISNULL(det.CantidadItems, 0) = 0 THEN 1 ELSE 0 END AS bit) AS EsContable,
                    CAST(CASE WHEN c.IvaDashboard = 0 AND c.ImporteDashboard > 0 THEN 1 ELSE 0 END AS bit) AS IvaEnCero,
                    CASE
                        WHEN c.ImporteDashboard = 0 THEN 'En cero'
                        WHEN ISNULL(det.CantidadItems, 0) = 0 THEN 'Sin detalle'
                        WHEN c.IvaDashboard = 0 AND c.ImporteDashboard > 0 THEN 'IVA 0'
                        ELSE ''
                    END AS AlertaOperativa
                {HeaderWithDetailFromClause}
                ORDER BY c.FECHA DESC, c.IDCOMPROBANTE DESC;
                """, scoped, MapComprobante, cancellationToken);

            return new ActividadUsuarioDetalleDto
            {
                Resumen = resumen,
                SerieComprobantesPorDia = actividadPorDia.Select(x => new MonthlyPointDto
                {
                    Periodo = x.Fecha.ToString("dd/MM"),
                    Total = x.CantidadComprobantes
                }).ToList(),
                SerieItemsPorDia = actividadPorDia.Select(x => new MonthlyPointDto
                {
                    Periodo = x.Fecha.ToString("dd/MM"),
                    Total = x.CantidadItems
                }).ToList(),
                Comprobantes = comprobantes
            };
        });
    }

    private static IReadOnlyList<CategoryTotalDto> BuildSegmentacionTipoCarga(IReadOnlyList<ActividadUsuarioResumenDto> usuarios, int totalComprobantes)
    {
        var divisor = totalComprobantes == 0 ? 1 : totalComprobantes;
        var conDetalle = usuarios.Sum(x => x.ComprobantesConDetalle);
        var contables = usuarios.Sum(x => x.ComprobantesContables);

        return
        [
            new CategoryTotalDto { Categoria = "Con detalle", Codigo = "detalle", Total = conDetalle, Participacion = conDetalle / (decimal)divisor },
            new CategoryTotalDto { Categoria = "Contables", Codigo = "contable", Total = contables, Participacion = contables / (decimal)divisor }
        ];
    }

    private static IReadOnlyList<ActividadInsightDto> BuildActividadInsights(
        IReadOnlyList<ActividadUsuarioResumenDto> usuarios,
        IReadOnlyList<ActividadDiaDto> dias,
        int totalComprobantes)
    {
        var insights = new List<ActividadInsightDto>();

        var mayor = usuarios.OrderByDescending(x => x.CantidadComprobantes).FirstOrDefault();
        if (mayor is not null && totalComprobantes > 0)
        {
            insights.Add(new ActividadInsightDto
            {
                Tipo = "info",
                Mensaje = $"{mayor.Usuario} cargó {(mayor.CantidadComprobantes / (decimal)totalComprobantes):P1} de los comprobantes del período."
            });
        }

        var mayorDetalle = usuarios
            .OrderByDescending(x => x.ComprobantesConDetalle)
            .ThenByDescending(x => x.CantidadItems)
            .FirstOrDefault();
        if (mayorDetalle is not null && mayorDetalle.ComprobantesConDetalle > 0)
        {
            insights.Add(new ActividadInsightDto
            {
                Tipo = "success",
                Mensaje = $"{mayorDetalle.Usuario} lidera la carga detallada con {mayorDetalle.ComprobantesConDetalle} comprobantes con detalle y {mayorDetalle.CantidadItems} ítems."
            });
        }

        var menor = usuarios.Where(x => x.CantidadComprobantes > 0).OrderBy(x => x.CantidadComprobantes).FirstOrDefault();
        if (menor is not null && usuarios.Count > 1)
        {
            insights.Add(new ActividadInsightDto
            {
                Tipo = "warn",
                Mensaje = $"{menor.Usuario} tuvo la menor actividad con {menor.CantidadComprobantes} comprobantes."
            });
        }

        var pico = dias.OrderByDescending(x => x.CantidadComprobantes).ThenByDescending(x => x.CantidadItems).FirstOrDefault();
        if (pico is not null)
        {
            insights.Add(new ActividadInsightDto
            {
                Tipo = "success",
                Mensaje = $"El {pico.Fecha:dd/MM/yyyy} fue el día con mayor actividad: {pico.CantidadComprobantes} comprobantes y {pico.CantidadItems} ítems."
            });
        }

        if (dias.Count > 1)
        {
            var diasSinActividad = 0;
            for (var date = dias.Min(x => x.Fecha).Date; date <= dias.Max(x => x.Fecha).Date; date = date.AddDays(1))
            {
                if (!dias.Any(x => x.Fecha.Date == date))
                {
                    diasSinActividad++;
                }
            }

            if (diasSinActividad > 0)
            {
                insights.Add(new ActividadInsightDto
                {
                    Tipo = "warn",
                    Mensaje = $"Hay {diasSinActividad} días sin carga de comprobantes dentro del período analizado."
                });
            }
        }

        var promedio = dias.Count > 0 ? dias.Average(x => x.CantidadComprobantes) : 0;
        var picos = dias.Count(x => x.CantidadComprobantes > promedio * 1.5);
        if (picos > 0)
        {
            insights.Add(new ActividadInsightDto
            {
                Tipo = "info",
                Mensaje = $"Se detectaron {picos} picos de carga por encima del promedio diario."
            });
        }

        return insights.Take(5).ToList();
    }
}
