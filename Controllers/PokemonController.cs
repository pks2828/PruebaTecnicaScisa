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
    /// <summary>
    /// Controlador principal para gestión de Pokémon
    /// </summary>
    public class PokemonController : Controller
    {
        private readonly IPokeApiService _pokeApiService;
        private readonly IExcelService _excelService;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _memoryCache;

        private const int PAGE_SIZE = 20;
        private const int FETCH_LIMIT_WITH_FILTERS = 100;
        private const string ERROR_GENERAL = "Ocurrió un error inesperado. Intenta de nuevo más tarde.";
        private const string ERROR_NO_DATA = "No se encontraron datos para exportar.";
        private const string ERROR_INVALID_DATA = "Los datos proporcionados no son válidos.";

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

        /// <summary>
        /// Página principal con listado paginado y filtros
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? nameFilter, string? typeFilter, int page = 1)
        {
            try
            {
                // Intentar obtener datos del cache
                var cacheKey = BuildCacheKey(nameFilter, typeFilter, page);
                if (_memoryCache.TryGetValue(cacheKey, out PokemonFilterViewModel cachedVm))
                {
                    return View(cachedVm);
                }

                // Construir ViewModel base
                var viewModel = CreateBaseViewModel(nameFilter, typeFilter, page);

                // Cargar tipos disponibles
                viewModel.TypeOptions = await _pokeApiService.GetAllTypesAsync();

                // Obtener datos de Pokémon según filtros
                var pokemonData = await GetPokemonData(typeFilter, page);
                var gridItems = await BuildGridItems(pokemonData.Items);

                // Aplicar paginación
                await ApplyPagination(viewModel, gridItems, nameFilter, typeFilter, page, pokemonData.TotalCount);

                // Guardar en cache y retornar vista
                CacheViewModel(cacheKey, viewModel);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error al cargar la lista de Pokémon");
            }
        }

        /// <summary>
        /// Exporta datos seleccionados a Excel
        /// </summary>
        [HttpPost]
        public IActionResult ExportToExcel(string excelRows)
        {
            try
            {
                // Validar datos de entrada
                if (string.IsNullOrWhiteSpace(excelRows))
                {
                    return BadRequest(ERROR_NO_DATA);
                }

                // Deserializar y validar contenido
                var pokemons = JsonConvert.DeserializeObject<List<PokemonExcelRow>>(excelRows);
                if (pokemons == null || !pokemons.Any())
                {
                    return BadRequest(ERROR_INVALID_DATA);
                }

                // Generar archivo Excel
                var content = _excelService.GeneratePokemonExcel(pokemons);
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Pokemons.xlsx");
            }
            catch (JsonException)
            {
                return BadRequest(ERROR_INVALID_DATA);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error al generar archivo Excel");
            }
        }

        /// <summary>
        /// Envía correos masivos a múltiples destinatarios
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendBulkEmail(
            [FromForm] string emailList,
            [FromForm] string subject,
            [FromForm] string body)
        {
            try
            {
                // Validar campos requeridos
                if (!ValidateEmailFields(emailList, subject, body))
                {
                    TempData["ErrorMessage"] = "Todos los campos (correos, asunto y cuerpo) son obligatorios.";
                    return RedirectToAction(nameof(Index));
                }

                // Procesar lista de correos
                var emails = ParseEmailList(emailList);
                if (!emails.Any())
                {
                    TempData["ErrorMessage"] = "No se detectaron direcciones de correo válidas.";
                    return RedirectToAction(nameof(Index));
                }

                // Enviar correos
                await _emailService.SendBulkEmailAsync(emails, subject, body);
                TempData["SuccessMessage"] = "Correos enviados correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al enviar correos masivos.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Obtiene detalles de un Pokémon específico
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                var detail = await _pokeApiService.GetPokemonDetailAsync(id.ToString());
                return PartialView("_PokemonDetailPartial", detail);
            }
            catch (Exception)
            {
                return Content("Error al obtener detalles del Pokémon.");
            }
        }

        #region Métodos Privados

        /// <summary>
        /// Construye clave única para cache basada en filtros y página
        /// </summary>
        private static string BuildCacheKey(string? nameFilter, string? typeFilter, int page)
        {
            var normalizedName = string.IsNullOrWhiteSpace(nameFilter) ? "" : nameFilter.Trim().ToLowerInvariant();
            var normalizedType = string.IsNullOrWhiteSpace(typeFilter) ? "" : typeFilter.Trim().ToLowerInvariant();
            return $"Pokemons_{normalizedName}_{normalizedType}_Page{page}";
        }

        /// <summary>
        /// Crea ViewModel base con configuración inicial
        /// </summary>
        private static PokemonFilterViewModel CreateBaseViewModel(string? nameFilter, string? typeFilter, int page)
        {
            return new PokemonFilterViewModel
            {
                NameFilter = nameFilter,
                SelectedType = typeFilter,
                PageNumber = page,
                PageSize = PAGE_SIZE
            };
        }

        /// <summary>
        /// Obtiene datos de Pokémon según filtros aplicados
        /// </summary>
        private async Task<(List<PokemonBasicInfo> Items, int TotalCount)> GetPokemonData(string? typeFilter, int page)
        {
            if (!string.IsNullOrEmpty(typeFilter))
            {
                var pokemonsByType = await _pokeApiService.GetPokemonsByTypeFullAsync(typeFilter);
                return (pokemonsByType, pokemonsByType.Count);
            }

            var offset = (page - 1) * PAGE_SIZE;
            var listResponse = await _pokeApiService.GetPokemonListAsync(offset, PAGE_SIZE);
            return (listResponse.Results, listResponse.Count);
        }

        /// <summary>
        /// Construye elementos de grilla con información completa
        /// </summary>
        private async Task<List<PokemonGridItemViewModel>> BuildGridItems(List<PokemonBasicInfo> pokemonsBasic)
        {
            var gridItems = new List<PokemonGridItemViewModel>();

            foreach (var basic in pokemonsBasic)
            {
                var segments = basic.Url.TrimEnd('/').Split('/');
                if (!int.TryParse(segments.Last(), out int id)) continue;

                var imageUrl = $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork/{id}.png";
                var types = await _pokeApiService.GetPokemonTypesAsync(id);

                gridItems.Add(new PokemonGridItemViewModel
                {
                    Id = id,
                    Name = basic.Name,
                    ImageUrl = imageUrl,
                    Types = types
                });
            }

            return gridItems;
        }

        /// <summary>
        /// Aplica paginación y filtros al ViewModel
        /// </summary>
        private async Task ApplyPagination(PokemonFilterViewModel viewModel, List<PokemonGridItemViewModel> gridItems,
            string? nameFilter, string? typeFilter, int page, int totalCount)
        {
            var hasFilters = !string.IsNullOrEmpty(nameFilter) || !string.IsNullOrEmpty(typeFilter);

            if (hasFilters)
            {
                var filteredItems = gridItems.AsQueryable();

                if (!string.IsNullOrEmpty(nameFilter))
                {
                    filteredItems = filteredItems.Where(p => p.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));
                }

                var paginated = await PaginatedList<PokemonGridItemViewModel>.CreateAsync(filteredItems, page, PAGE_SIZE);
                SetPaginationProperties(viewModel, paginated);
            }
            else
            {
                var paginated = await PaginatedList<PokemonGridItemViewModel>.CreateFromPage(
                    gridItems, totalCount, page, PAGE_SIZE);
                SetPaginationProperties(viewModel, paginated);
            }
        }

        /// <summary>
        /// Establece propiedades de paginación en el ViewModel
        /// </summary>
        private static void SetPaginationProperties(PokemonFilterViewModel viewModel, PaginatedList<PokemonGridItemViewModel> paginated)
        {
            viewModel.Pokemons = paginated;
            viewModel.TotalCount = paginated.TotalCount;
            viewModel.PageNumbers = paginated.PageNumbers;
            viewModel.HasPreviousPage = paginated.HasPreviousPage;
            viewModel.HasNextPage = paginated.HasNextPage;
        }

        /// <summary>
        /// Guarda ViewModel en cache con configuración de expiración
        /// </summary>
        private void CacheViewModel(string cacheKey, PokemonFilterViewModel viewModel)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(5)
            };
            _memoryCache.Set(cacheKey, viewModel, cacheOptions);
        }

        /// <summary>
        /// Valida campos requeridos para envío de email
        /// </summary>
        private static bool ValidateEmailFields(string? emailList, string? subject, string? body)
        {
            return !string.IsNullOrWhiteSpace(emailList) &&
                   !string.IsNullOrWhiteSpace(subject) &&
                   !string.IsNullOrWhiteSpace(body);
        }

        /// <summary>
        /// Procesa y limpia lista de correos electrónicos
        /// </summary>
        private static List<string> ParseEmailList(string emailList)
        {
            return emailList
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(email => email.Trim())
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .ToList();
        }

        /// <summary>
        /// Maneja errores de forma consistente
        /// </summary>
        private IActionResult HandleError(Exception ex, string userMessage)
        {
            // Log del error (aquí se podría integrar con un sistema de logging)
            // _logger.LogError(ex, userMessage);

            ViewBag.ErrorMessage = userMessage;
            return View(new PokemonFilterViewModel
            {
                PageNumber = 1,
                PageSize = PAGE_SIZE,
                TypeOptions = new List<string>()
            });
        }

        #endregion
    }
}