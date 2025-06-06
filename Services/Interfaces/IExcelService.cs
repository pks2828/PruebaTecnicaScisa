using MiPokemonApp.Models.Excel;

namespace MiPokemonApp.Services.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de generación de archivos Excel con datos de Pokémons.
    /// </summary>
    public interface IExcelService
    {
        byte[] GeneratePokemonExcel(List<PokemonExcelRow> rows);
    }
}
