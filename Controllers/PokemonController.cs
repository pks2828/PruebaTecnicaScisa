using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MiPokemonApp.Models.Excel;
using MiPokemonApp.Models.ViewModels;
using MiPokemonApp.Services.Implementations;
using MiPokemonApp.Services.Interfaces;
using Newtonsoft.Json;

namespace MiPokemonApp.Controllers
{
    /// <summary>
    /// Controlador principal para gestión de Pokémon
    /// </summary>
    public class PokemonController : Controller
    {
        private readonly IPokemonService _pokemonService;
        private readonly IExcelService _excelService;
        private readonly IEmailService _emailService;
        private readonly ILogger<PokemonController> _logger;

        public PokemonController(
            IPokemonService pokemonService,
            IExcelService excelService,
            IEmailService emailService,
            ILogger<PokemonController> logger) // <-- Inyectado por parámetro
        {
            _pokemonService = pokemonService;
            _excelService = excelService;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Página principal con listado paginado y filtros
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? nameFilter, string? typeFilter, int page = 1)
        {
            try
            {
                _logger.LogInformation("Cargando página {Page} de Pokémon con filtros: nombre={Name}, tipo={Type}",
                    page, nameFilter, typeFilter);

                var viewModel = await _pokemonService
                    .BuildPokemonFilterViewModelAsync(nameFilter, typeFilter, page);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la lista de Pokémon en Index");
                ViewBag.ErrorMessage = "Error al cargar la lista de Pokémon";
                return View(new PokemonFilterViewModel());
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
                _logger.LogInformation("Iniciando ExportToExcel. Recibidos {Count} bytes en excelRows.",
                    string.IsNullOrWhiteSpace(excelRows) ? 0 : excelRows.Length);

                if (string.IsNullOrWhiteSpace(excelRows))
                {
                    _logger.LogWarning("ExportToExcel: excelRows está vacío o nulo.");
                    return BadRequest("No se encontraron datos para exportar.");
                }

                var pokemons = JsonConvert
                    .DeserializeObject<List<PokemonExcelRow>>(excelRows);

                if (pokemons == null || !pokemons.Any())
                {
                    _logger.LogWarning("ExportToExcel: la deserialización devolvió nulo o lista vacía.");
                    return BadRequest("Los datos proporcionados no son válidos.");
                }

                _logger.LogInformation("ExportToExcel: generando archivo Excel para {Count} Pokémon.", pokemons.Count);
                var content = _excelService.GeneratePokemonExcel(pokemons);

                _logger.LogInformation("ExportToExcel: archivo Excel generado correctamente.");
                return File(
                    content,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Pokemons.xlsx"
                );
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "ExportToExcel: error de JSON al deserializar excelRows.");
                return BadRequest("Los datos proporcionados no son válidos.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExportToExcel: error inesperado al generar archivo Excel.");
                TempData["ErrorMessage"] = "Error al generar archivo Excel";
                return RedirectToAction(nameof(Index));
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
                // 1. Validar campos con el servicio
                if (!_emailService.ValidateEmailFields(emailList, subject, body))
                {
                    TempData["ErrorMessage"] = "Todos los campos (correos, asunto y cuerpo) son obligatorios.";
                    return RedirectToAction(nameof(Index));
                }

                // 2. Parsear lista de correos con el servicio
                var emails = _emailService.ParseAndValidateEmailList(emailList);
                if (!emails.Any())
                {
                    TempData["ErrorMessage"] = "No se detectaron direcciones de correo válidas.";
                    return RedirectToAction(nameof(Index));
                }

                // 3. Enviar correos
                await _emailService.SendBulkEmailAsync(emails, subject, body);
                TempData["SuccessMessage"] = "Correos enviados correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correos masivos.");
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
                _logger.LogInformation("Obteniendo detalles del Pokémon con ID {Id}", id);
                var detail = await _pokemonService.GetPokemonDetailAsync(id.ToString());
                return PartialView("_PokemonDetailPartial", detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles del Pokémon con ID {Id}", id);
                return Content("Error al obtener detalles del Pokémon.");
            }
        }

    }
}
