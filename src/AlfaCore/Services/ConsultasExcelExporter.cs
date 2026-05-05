using ClosedXML.Excel;
using AlfaCore.Models;

namespace AlfaCore.Services;

public sealed class ConsultasExcelExporter
{
    public byte[] Exportar(ConsultaGuardadaDto consulta, ConsultaResultadoDto resultado)
        => ExportarTabla(
            $"{consulta.Clave} — {consulta.Descripcion}",
            resultado.EjecutadoEn,
            resultado.TotalFilas,
            resultado.Columnas,
            resultado.Filas);

    public byte[] ExportarAgrupado(
        ConsultaGuardadaDto consulta,
        DateTime ejecutadoEn,
        IReadOnlyList<string> columnas,
        IReadOnlyList<string[]> filas,
        string? etiqueta = null)
        => ExportarTabla(
            $"{consulta.Clave} — {consulta.Descripcion}{(string.IsNullOrWhiteSpace(etiqueta) ? string.Empty : $" — {etiqueta}")}",
            ejecutadoEn,
            filas.Count,
            columnas,
            filas);

    private static byte[] ExportarTabla(
        string titulo,
        DateTime ejecutadoEn,
        int totalFilas,
        IReadOnlyList<string> columnas,
        IReadOnlyList<string[]> filas)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Resultado");

        // Fila 1: título
        var titleCell = ws.Cell(1, 1);
        titleCell.Value = titulo;
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 12;
        titleCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
        titleCell.Style.Font.FontColor = XLColor.White;
        if (columnas.Count > 1)
            ws.Range(1, 1, 1, columnas.Count).Merge();

        // Fila 2: fecha de exportación
        var dateCell = ws.Cell(2, 1);
        dateCell.Value = $"Exportado el {ejecutadoEn:dd/MM/yyyy HH:mm} — {totalFilas} filas";
        dateCell.Style.Font.Italic = true;
        dateCell.Style.Font.FontSize = 9;
        dateCell.Style.Font.FontColor = XLColor.FromHtml("#64748b");
        if (columnas.Count > 1)
            ws.Range(2, 1, 2, columnas.Count).Merge();

        // Fila 3: encabezados
        for (int i = 0; i < columnas.Count; i++)
        {
            var cell = ws.Cell(3, i + 1);
            cell.Value = columnas[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0369a1");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        }

        // Filas de datos
        for (int r = 0; r < filas.Count; r++)
        {
            var fila = filas[r];
            for (int c = 0; c < fila.Length; c++)
            {
                var cell = ws.Cell(r + 4, c + 1);
                var valor = fila[c];

                // Intentar tipar el valor para Excel
                if (decimal.TryParse(valor, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var num))
                    cell.Value = num;
                else if (DateTime.TryParse(valor, out var fecha))
                {
                    cell.Value = fecha;
                    cell.Style.DateFormat.Format = "dd/MM/yyyy";
                }
                else
                    cell.Value = valor;

                // Filas alternas
                if (r % 2 == 1)
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
            }
        }

        // Ajuste automático de ancho
        ws.Columns().AdjustToContents(minWidth: 8, maxWidth: 60);

        // Freeze encabezados
        ws.SheetView.FreezeRows(3);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public static string NombreArchivo(ConsultaGuardadaDto consulta)
    {
        var nombre = consulta.Descripcion.Length > 40
            ? consulta.Descripcion[..40]
            : consulta.Descripcion;
        var sanitizado = string.Concat(nombre.Select(c => char.IsLetterOrDigit(c) || c == ' ' ? c : '_'));
        return $"{consulta.Clave}_{sanitizado.Trim()}_{DateTime.Today:yyyyMMdd}.xlsx";
    }
}
