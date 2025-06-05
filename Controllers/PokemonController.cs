using Microsoft.AspNetCore.Mvc;
using MiPokemonApp.Helpers;
using MiPokemonApp.Models.Excel;
using MiPokemonApp.Models.ViewModels;
using MiPokemonApp.Services.Interfaces;
using Newtonsoft.Json;

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
                // 1. Obtener especies
                vm.SpeciesOptions = await _pokeApiService.GetAllSpeciesNamesAsync();

                // 2. Definir fetchLimit y offset según filtros
                int fetchLimit = string.IsNullOrEmpty(nameFilter) && string.IsNullOrEmpty(speciesFilter)
                    ? PageSize
                    : 100;
                int offset = (page - 1) * PageSize;
                int actualOffset = string.IsNullOrEmpty(nameFilter) && string.IsNullOrEmpty(speciesFilter)
                    ? offset
                    : 0;

                var listResponse = await _pokeApiService.GetPokemonListAsync(actualOffset, fetchLimit);

                // 3. Construir lista completa en memoria de la página actual
                var allGridItems = new List<PokemonGridItemViewModel>();
                foreach (var basic in listResponse.Results)
                {
                    var segments = basic.Url.TrimEnd('/').Split('/');
                    if (!int.TryParse(segments.Last(), out int id)) continue;

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

                // 4. Si NO hay filtros, usar CreateFromPage para ventaneo
                if (string.IsNullOrEmpty(nameFilter) && string.IsNullOrEmpty(speciesFilter))
                {
                    // a) La lista “allGridItems” ya trae EXACTAMENTE PageSize (20) elementos porque fetchLimit = PageSize
                    // b) listResponse.Count es el total real de todos los Pokémon (por ejemplo, 115)
                    var paginated = await PaginatedList<PokemonGridItemViewModel>.CreateFromPage(
                        pageItems: allGridItems,
                        totalCount: listResponse.Count,
                        pageIndex: page,
                        pageSize: PageSize
                    );

                    vm.Pokemons = paginated;                 // 20 elementos de esta página
                    vm.TotalCount = paginated.TotalCount;      // = listResponse.Count
                    vm.PageNumbers = paginated.PageNumbers;     // rango “ventaneado” (ej. [3,4,5,6,7])
                    vm.HasPreviousPage = paginated.HasPreviousPage;
                    vm.HasNextPage = paginated.HasNextPage;
                }
                else
                {
                    // 5. Si HAY filtros, filtrar en memoria y usar CreateAsync normal
                    var filteredItems = allGridItems.AsQueryable();
                    if (!string.IsNullOrEmpty(nameFilter))
                        filteredItems = filteredItems
                            .Where(p => p.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(speciesFilter))
                        filteredItems = filteredItems
                            .Where(p => p.SpeciesName.Equals(speciesFilter, StringComparison.OrdinalIgnoreCase));

                    var filteredList = filteredItems.ToList();

                    var paginated = await PaginatedList<PokemonGridItemViewModel>.CreateAsync(
                        filteredList.AsQueryable(),
                        page,
                        PageSize
                    );

                    vm.Pokemons = paginated;
                    vm.TotalCount = paginated.TotalCount;
                    vm.PageNumbers = paginated.PageNumbers;
                    vm.HasPreviousPage = paginated.HasPreviousPage;
                    vm.HasNextPage = paginated.HasNextPage;
                }

                return View(vm);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Ocurrió un error al cargar los datos. Intenta de nuevo más tarde.";
                return View(vm);
            }
        }





        [HttpPost]
        public IActionResult ExportToExcel(string excelRows)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(excelRows))
                    return BadRequest("Los datos para exportar no fueron proporcionados.");

                var pokemons = JsonConvert.DeserializeObject<List<PokemonExcelRow>>(excelRows);
                if (pokemons == null || !pokemons.Any())
                    return BadRequest("La lista de datos está vacía o mal formada.");

                var content = _excelService.GeneratePokemonExcel(pokemons);

                return File(content,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            "Pokemons.xlsx");
            }
            catch (Exception)
            {
                return StatusCode(500, "Ocurrió un error al procesar la exportación.");
            }
        }


        [HttpPost]
        public async Task<IActionResult> SendBulkEmail(
            [FromForm] string emailList,   // Recibe el texto "a@ejemplo.com, b@ejemplo.com, c@ejemplo.com"
            [FromForm] string subject,
            [FromForm] string body)
        {
            // 1. Validar que no vengan campos vacíos
            if (string.IsNullOrWhiteSpace(emailList) ||
                string.IsNullOrWhiteSpace(subject) ||
                string.IsNullOrWhiteSpace(body))
            {
                TempData["ErrorMessage"] = "Todos los campos (correos, asunto y cuerpo) son obligatorios.";
                return RedirectToAction(nameof(Index));
            }

            // 2. Separar el string de correos por comas, limpiando espacios en blanco
            //    y descartando posibles entradas vacías
            List<string> listaDeCorreos = emailList
                .Split(',', StringSplitOptions.RemoveEmptyEntries)   // parte por coma
                .Select(e => e.Trim())                              // quita espacios sobrantes
                .Where(e => !string.IsNullOrWhiteSpace(e))          // descarta cadenas vacías
                .ToList();

            if (!listaDeCorreos.Any())
            {
                TempData["ErrorMessage"] = "No se detectaron direcciones de correo válidas.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // 3. Llamar al servicio con la lista ya convertida
                await _emailService.SendBulkEmailAsync(listaDeCorreos, subject, body);
                TempData["SuccessMessage"] = "Correos enviados correctamente.";
            }
            catch (Exception)
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
