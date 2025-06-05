using Microsoft.AspNetCore.Mvc;
using MiPokemonApp.Models.Excel;
using MiPokemonApp.Models.ViewModels;
using MiPokemonApp.Services.Interfaces;

namespace MiPokemonApp.Controllers
{
    public class PokemonController : Controller
    {
        private readonly IPokeApiService _pokeApiService;
        private readonly IExcelService _excelService;
        private readonly IEmailService _emailService;

        public PokemonController(
            IPokeApiService pokeApiService,
            IExcelService excelService,
            IEmailService emailService)
        {
            _pokeApiService = pokeApiService;
            _excelService = excelService;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? nameFilter, string? speciesFilter, int page = 1)
        {
            const int PageSize = 20;
            var vm = new PokemonFilterViewModel
            {
                NameFilter = nameFilter,
                SelectedSpecies = speciesFilter,
                PageNumber = page,
                PageSize = PageSize
            };

            try
            {
                Console.WriteLine("[DEBUG] Entrando a Index()");
                Console.WriteLine($"[DEBUG] Parámetros: nameFilter = '{nameFilter}', speciesFilter = '{speciesFilter}', page = {page}");

                // 1. Obtener todas las especies para el dropdown
                vm.SpeciesOptions = await _pokeApiService.GetAllSpeciesNamesAsync();
                Console.WriteLine($"[DEBUG] Se cargaron {vm.SpeciesOptions.Count} especies.");

                // 2. Si hay filtros, necesitamos obtener más datos para filtrar
                int fetchLimit = string.IsNullOrEmpty(nameFilter) && string.IsNullOrEmpty(speciesFilter)
                    ? PageSize
                    : 100; // Obtener más para tener suficientes después del filtrado

                int offset = (page - 1) * PageSize;

                // Si hay filtros, empezar desde 0 para obtener todos los datos necesarios
                int actualOffset = string.IsNullOrEmpty(nameFilter) && string.IsNullOrEmpty(speciesFilter)
                    ? offset
                    : 0;

                var listResponse = await _pokeApiService.GetPokemonListAsync(actualOffset, fetchLimit);
                Console.WriteLine($"[DEBUG] PokeAPI devolvió {listResponse.Results.Count} resultados. TotalCount = {listResponse.Count}");

                var allGridItems = new List<PokemonGridItemViewModel>();

                foreach (var basic in listResponse.Results)
                {
                    var segments = basic.Url.TrimEnd('/').Split('/');
                    if (!int.TryParse(segments.Last(), out int id))
                    {
                        Console.WriteLine($"[WARN] No se pudo extraer ID de URL: {basic.Url}");
                        continue;
                    }

                    // Obtener especie (usa caché)
                    string speciesName = await _pokeApiService.GetSpeciesNameAsync(id);

                    string imageUrl = $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork/{id}.png";

                    allGridItems.Add(new PokemonGridItemViewModel
                    {
                        Id = id,
                        Name = basic.Name,
                        ImageUrl = imageUrl,
                        SpeciesName = speciesName
                    });
                }

                // 3. Aplicar filtros
                var filteredItems = allGridItems.AsQueryable();

                if (!string.IsNullOrEmpty(nameFilter))
                {
                    filteredItems = filteredItems.Where(p => p.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(speciesFilter))
                {
                    filteredItems = filteredItems.Where(p => p.SpeciesName.Equals(speciesFilter, StringComparison.OrdinalIgnoreCase));
                }

                var filteredList = filteredItems.ToList();

                // 4. Aplicar paginación a los resultados filtrados
                if (string.IsNullOrEmpty(nameFilter) && string.IsNullOrEmpty(speciesFilter))
                {
                    // Sin filtros, usar los datos tal como vienen
                    vm.Pokemons = allGridItems;
                    vm.TotalCount = listResponse.Count;
                }
                else
                {
                    // Con filtros, paginar los resultados filtrados
                    vm.Pokemons = filteredList.Skip((page - 1) * PageSize).Take(PageSize).ToList();
                    vm.TotalCount = filteredList.Count;
                }

                Console.WriteLine($"[DEBUG] Total Pokémon después de aplicar filtros: {vm.Pokemons.Count}");
                Console.WriteLine($"[DEBUG] TotalCount para paginación: {vm.TotalCount}");

                return View(vm);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Ocurrió un error en Index(): {ex.Message}");
                Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = "Ocurrió un error al cargar los datos. Intenta de nuevo más tarde.";
                return View(vm);
            }
        }


        [HttpPost]
        public IActionResult ExportToExcel(List<PokemonExcelRow> excelRows)
        {
            var content = _excelService.GeneratePokemonExcel(excelRows);
            return File(content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Pokemons.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> SendEmail(string emailTo, string subject, string body)
        {
            try
            {
                await _emailService.SendEmailAsync(emailTo, subject, body);
                TempData["SuccessMessage"] = "Correo enviado correctamente.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Error al enviar el correo.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> SendBulkEmail(List<string> emailList, string subject, string body)
        {
            try
            {
                await _emailService.SendBulkEmailAsync(emailList, subject, body);
                TempData["SuccessMessage"] = "Correos enviados correctamente.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Error al enviar correos masivos.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                var detail = await _pokeApiService.GetPokemonDetailAsync(id.ToString());
                return PartialView("_PokemonDetailPartial", detail);
            }
            catch
            {
                return Content("Error al obtener detalles.");
            }
        }
    }
}
