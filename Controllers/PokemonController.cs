using Microsoft.AspNetCore.Mvc;
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

        public PokemonController(
            IPokemonService pokemonService,
            IExcelService excelService,
            IEmailService emailService)
        {
            _pokemonService = pokemonService;
            _excelService = excelService;
            _emailService = emailService;
        }

        /// <summary>
        /// Página principal con listado paginado y filtros
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? nameFilter, string? typeFilter, int page = 1)
        {
            try
            {
                var viewModel = await _pokemonService
                    .BuildPokemonFilterViewModelAsync(nameFilter, typeFilter, page);

                return View(viewModel);
            }
            catch
            {
                // En caso de error, devolvemos un ViewModel vacío similar a antes
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
                if (string.IsNullOrWhiteSpace(excelRows))
                    return BadRequest("No se encontraron datos para exportar.");

                var pokemons = JsonConvert
                    .DeserializeObject<List<PokemonExcelRow>>(excelRows);

                if (pokemons == null || !pokemons.Any())
                    return BadRequest("Los datos proporcionados no son válidos.");

                var content = _excelService.GeneratePokemonExcel(pokemons);
                return File(
                    content,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Pokemons.xlsx"
                );
            }
            catch (JsonException)
            {
                return BadRequest("Los datos proporcionados no son válidos.");
            }
            catch
            {
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
                if (!_pokemonService.ValidateEmailFields(emailList, subject, body))
                {
                    TempData["ErrorMessage"] = "Todos los campos (correos, asunto y cuerpo) son obligatorios.";
                    return RedirectToAction(nameof(Index));
                }

                // 2. Parsear lista de correos con el servicio
                var emails = _pokemonService.ParseAndValidateEmailList(emailList);
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
            catch
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
                var detail = await _pokemonService
                    .GetPokemonDetailAsync(id.ToString());
                return PartialView("_PokemonDetailPartial", detail);
            }
            catch
            {
                return Content("Error al obtener detalles del Pokémon.");
            }
        }
    }
}
