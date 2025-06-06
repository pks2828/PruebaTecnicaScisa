using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using MiPokemonApp.Models.Excel;
using MiPokemonApp.Services.Interfaces;

namespace MiPokemonApp.Services.Implementations
{
    /// <summary>
    /// Servicio para la generación de archivos Excel con información de Pokémons.
    /// </summary>
    public class ExcelService : IExcelService
    {
        private const string SHEET_NAME = "Pokemons";
        private const string HEADER_ID = "ID";
        private const string HEADER_NAME = "Nombre";
        private const string HEADER_TYPES = "Tipos";

        // private readonly ILogger<ExcelService> _logger; // Descomentar si se inyecta ILogger

        /// <summary>
        /// Genera un archivo Excel con las filas de datos proporcionadas.
        /// </summary>
        /// <param name="rows">Lista de filas que representan datos de Pokémons.</param>
        /// <returns>Arreglo de bytes con el contenido del archivo Excel generado.</returns>
        /// <exception cref="ArgumentNullException">Si <paramref name="rows"/> es nulo.</exception>
        /// <exception cref="ExcelException">Si ocurre un error durante la creación del Excel.</exception>
        public byte[] GeneratePokemonExcel(List<PokemonExcelRow> rows)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows), "La lista de filas no puede ser nula.");
            }

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add(SHEET_NAME);

                    // Agregar encabezados
                    AddHeaders(worksheet);

                    // Agregar filas de datos
                    PopulateRows(worksheet, rows);

                    using (var memoryStream = new MemoryStream())
                    {
                        workbook.SaveAs(memoryStream);
                        return memoryStream.ToArray();
                    }
                }
            }
            catch (IOException ex)
            {
                // _logger.LogError(ex, "Error de IO al generar el Excel de Pokémons.");
                throw new ExcelException("Error de E/S al generar el archivo Excel.", ex);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error inesperado al generar el Excel de Pokémons.");
                throw new ExcelException("Error inesperado al generar el archivo Excel.", ex);
            }
        }

        /// <summary>
        /// Agrega la fila de encabezados al <paramref name="worksheet"/>.
        /// </summary>
        /// <param name="worksheet">Hoja de cálculo donde se agregarán los encabezados.</param>
        private void AddHeaders(IXLWorksheet worksheet)
        {
            worksheet.Cell(1, 1).Value = HEADER_ID;
            worksheet.Cell(1, 2).Value = HEADER_NAME;
            worksheet.Cell(1, 3).Value = HEADER_TYPES;
        }

        /// <summary>
        /// Llena las filas de datos a partir de la lista proporcionada.
        /// </summary>
        /// <param name="worksheet">Hoja de cálculo donde se agregarán las filas.</param>
        /// <param name="rows">Lista de <see cref="PokemonExcelRow"/> con la información a insertar.</param>
        private void PopulateRows(IXLWorksheet worksheet, List<PokemonExcelRow> rows)
        {
            int rowIndex = 2; // Comienza en la fila 2, tras los encabezados
            foreach (var row in rows)
            {
                // Validación de cada fila
                if (row == null)
                {
                    // _logger.LogWarning("Fila nula encontrada en la lista; se omite.");
                    continue;
                }

                worksheet.Cell(rowIndex, 1).Value = row.Id;
                worksheet.Cell(rowIndex, 2).Value = row.Name;
                worksheet.Cell(rowIndex, 3).Value = row.types; // Se respeta nombre de propiedad existente
                rowIndex++;
            }
        }

        /// <summary>
        /// Excepción personalizada para errores en la generación de archivos Excel.
        /// </summary>
        public class ExcelException : Exception
        {
            public ExcelException() { }

            public ExcelException(string message) : base(message) { }

            public ExcelException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}
