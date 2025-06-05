using System.IO;
using ClosedXML.Excel;
using MiPokemonApp.Models.Excel;
using MiPokemonApp.Services.Interfaces;

namespace MiPokemonApp.Services.Implementations
{
    public class ExcelService : IExcelService
    {
        public byte[] GeneratePokemonExcel(List<PokemonExcelRow> rows)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Pokemons");

            // Encabezados
            sheet.Cell(1, 1).Value = "ID";
            sheet.Cell(1, 2).Value = "Nombre";
            sheet.Cell(1, 3).Value = "Especie";

            int fila = 2;
            foreach (var r in rows)
            {
                sheet.Cell(fila, 1).Value = r.Id;
                sheet.Cell(fila, 2).Value = r.Name;
                sheet.Cell(fila, 3).Value = r.Species;
                fila++;
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
