using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MiPokemonApp.Helpers;
using MiPokemonApp.Models.Excel;
using MiPokemonApp.Models.PokeApi;
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
        private readonly IMemoryCache _memoryCache;


        public PokemonController(
            IPokeApiService pokeApiService,
            IExcelService excelService,
            IMemoryCache memoryCache,
            IEmailService emailService)
        {
            _pokeApiService = pokeApiService;
            _excelService = excelService;
            _emailService = emailService;
            _memoryCache = memoryCache;

        }
        [HttpGet]
        public async Task<IActionResult> Index(string? nameFilter, string? typeFilter, int page = 1)
        {
            const int PageSize = 20;

            // 1. Montar la clave única para el cache:
            // Incluimos filtros y página. Para evitar nulls, usamos string.Empty cuando falte.
            var normalizedName = string.IsNullOrWhiteSpace(nameFilter) ? "" : nameFilter.Trim().ToLowerInvariant();
            var normalizedType = string.IsNullOrWhiteSpace(typeFilter) ? "" : typeFilter.Trim().ToLowerInvariant();
            var cacheKey = $"Pokemons_{normalizedName}_{normalizedType}_Page{page}";

            // 2. Intentar leer del cache:
            if (_memoryCache.TryGetValue(cacheKey, out PokemonFilterViewModel cachedVm))
            {
                // Si existe en cache, devolvemos la vista con el ViewModel ya cargado.
                return View(cachedVm);
            }

            // 3. Si no está en cache, construimos el ViewModel desde cero:
            var vm = new PokemonFilterViewModel
            {
                NameFilter = nameFilter,
                SelectedType = typeFilter,
                PageNumber = page,
                PageSize = PageSize
            };

            try
            {
                // 3.1. Cargar lista de tipos (no cacheada aquí; asumo que GetAllTypesAsync ya usa su propio cache interno).
                vm.TypeOptions = await _pokeApiService.GetAllTypesAsync();

                // 3.2. Fijar fetchLimit y offset según filtros:
                int fetchLimit = string.IsNullOrEmpty(nameFilter) && string.IsNullOrEmpty(typeFilter)
                    ? PageSize
                    : 100;
                int offset = (page - 1) * PageSize;
                int actualOffset = string.IsNullOrEmpty(nameFilter) && string.IsNullOrEmpty(typeFilter)
                    ? offset
                    : 0;

                // 3.3. Traer la lista principal (sin filtrar) desde PokeAPI:
                List<PokemonBasicInfo> pokemonsBasic;

                if (!string.IsNullOrEmpty(typeFilter))
                {
                    // Obtenemos todos los pokémon de ese tipo directamente (sin paginar aún)
                    pokemonsBasic = await _pokeApiService.GetPokemonsByTypeFullAsync(typeFilter);
                }
                else
                {
                    var listResponse = await _pokeApiService.GetPokemonListAsync(actualOffset, fetchLimit);
                    pokemonsBasic = listResponse.Results;
                    vm.TotalCount = listResponse.Count;
                }
                // 3.4. Generar los GridItems (await cada llamada a GetPokemonTypesAsync):
                var allGridItems = new List<PokemonGridItemViewModel>();
                foreach (var basic in pokemonsBasic)
                {
                    var segments = basic.Url.TrimEnd('/').Split('/');
                    if (!int.TryParse(segments.Last(), out int id)) continue;

                    string imageUrl = $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork/{id}.png";
                    var types = await _pokeApiService.GetPokemonTypesAsync(id);

                    allGridItems.Add(new PokemonGridItemViewModel
                    {
                        Id = id,
                        Name = basic.Name,
                        ImageUrl = imageUrl,
                        Types = types
                    });
                }

                // 3.5. Si no hay filtro, paginar directamente desde la página actual:
                if (!string.IsNullOrEmpty(nameFilter) || !string.IsNullOrEmpty(typeFilter))
                {
                    var filteredItems = allGridItems.AsQueryable();

                    if (!string.IsNullOrEmpty(nameFilter))
                        filteredItems = filteredItems
                            .Where(p => p.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));

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
                else
                {
                    var paginated = await PaginatedList<PokemonGridItemViewModel>.CreateFromPage(
                        pageItems: allGridItems,
                        totalCount: vm.TotalCount,
                        pageIndex: page,
                        pageSize: PageSize
                    );

                    vm.Pokemons = paginated;
                    vm.TotalCount = paginated.TotalCount;
                    vm.PageNumbers = paginated.PageNumbers;
                    vm.HasPreviousPage = paginated.HasPreviousPage;
                    vm.HasNextPage = paginated.HasNextPage;
                }


                // 4. Guardar el ViewModel completo en el cache:
                //    Para que tenga sentido almacenar estado de paginación, guardamos todo `vm`.
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                };
                _memoryCache.Set(cacheKey, vm, cacheOptions);

                return View(vm);
            }
            catch (Exception)
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
