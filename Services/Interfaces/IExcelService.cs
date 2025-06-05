using MiPokemonApp.Models.Excel;

namespace MiPokemonApp.Services.Interfaces
{
    public interface IExcelService
    {
        byte[] GeneratePokemonExcel(List<PokemonExcelRow> rows);
    }
}
