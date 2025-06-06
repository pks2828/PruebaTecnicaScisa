using Microsoft.Extensions.Caching.Memory;
using MiPokemonApp.Helpers;
using MiPokemonApp.Models.PokeApi;
using MiPokemonApp.Models.ViewModels;
using MiPokemonApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiPokemonApp.Services.Implementations
{
    /// <summary>
    /// Implementación de IPokemonService:
    /// encapsula toda la lógica que antes estaba en PokemonController.
    /// </summary>
    public class PokemonService : IPokemonService
    {
        private readonly IPokeApiService _pokeApiService;
        private readonly IMemoryCache _memoryCache;
        private const int PAGE_SIZE = 20;
        private const string CACHE_PREFIX = "Pokemons";

        public PokemonService(
            IPokeApiService pokeApiService,
            IMemoryCache memoryCache)
        {
            _pokeApiService = pokeApiService;
            _memoryCache = memoryCache;
        }

        /// <inheritdoc/>
        public async Task<PokemonFilterViewModel> BuildPokemonFilterViewModelAsync(
            string? nameFilter,
            string? typeFilter,
            int page)
        {
            // 1. Intentar recuperar del cache
            var cacheKey = BuildCacheKey(nameFilter, typeFilter, page);
            if (_memoryCache.TryGetValue(cacheKey, out PokemonFilterViewModel cachedVm))
                return cachedVm;

            // 2. Crear ViewModel base y cargar tipos
            var vm = new PokemonFilterViewModel
            {
                NameFilter   = nameFilter,
                SelectedType = typeFilter,
                PageNumber   = page,
                PageSize     = PAGE_SIZE,
                TypeOptions  = await _pokeApiService.GetAllTypesAsync()
            };

            // 3. Obtener listado crudo (básico) según filtro de tipo o paginación simple
            var (basicList, totalCount) = await FetchPokemonListAsync(typeFilter, page);

            // 4. Construir items de grilla y paginar
            var paginated = await BuildAndPaginateGridItemsAsync(basicList, vm);

            // 5. Asignar al ViewModel
            vm.Pokemons       = paginated;
            vm.TotalCount     = paginated.TotalCount;
            vm.PageNumbers    = paginated.PageNumbers;
            vm.HasPreviousPage = paginated.HasPreviousPage;
            vm.HasNextPage     = paginated.HasNextPage;

            // 6. Cachear y retornar
            _memoryCache.Set(cacheKey, vm, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(5)
            });

            return vm;
        }

        /// <inheritdoc/>
        public async Task<PokemonDetailViewModel> GetPokemonDetailAsync(string nameOrId)
        {
            // Delegamos directamente a PokeApiService
            return await _pokeApiService.GetPokemonDetailAsync(nameOrId);
        }

        /// <inheritdoc/>
        public List<string> ParseAndValidateEmailList(string emailList)
        {
            if (string.IsNullOrWhiteSpace(emailList))
                return new List<string>();

            return emailList
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();
        }

        /// <inheritdoc/>
        public bool ValidateEmailFields(string? emailList, string? subject, string? body)
        {
            return !string.IsNullOrWhiteSpace(emailList)
                && !string.IsNullOrWhiteSpace(subject)
                && !string.IsNullOrWhiteSpace(body);
        }

        #region Métodos privados auxiliares

        private static string BuildCacheKey(string? name, string? type, int page)
        {
            var n = string.IsNullOrWhiteSpace(name) ? "" : name.Trim().ToLowerInvariant();
            var t = string.IsNullOrWhiteSpace(type) ? "" : type.Trim().ToLowerInvariant();
            return $"{CACHE_PREFIX}_{n}_{t}_Page{page}";
        }

        private async Task<(List<PokemonBasicInfo>, int)> FetchPokemonListAsync(string? typeFilter, int page)
        {
            if (!string.IsNullOrEmpty(typeFilter))
            {
                var byType = await _pokeApiService.GetPokemonsByTypeFullAsync(typeFilter);
                return (byType, byType.Count);
            }

            var offset = (page - 1) * PAGE_SIZE;
            var listResponse = await _pokeApiService.GetPokemonListAsync(offset, PAGE_SIZE);
            return (listResponse.Results, listResponse.Count);
        }

        private async Task<PaginatedList<PokemonGridItemViewModel>> BuildAndPaginateGridItemsAsync(
            List<PokemonBasicInfo> basicList,
            PokemonFilterViewModel vm)
        {
            // 1. Transformar cada PokemonBasicInfo en PokemonGridItemViewModel
            var allGridItems = new List<PokemonGridItemViewModel>();
            foreach (var basic in basicList)
            {
                var segments = basic.Url.TrimEnd('/').Split('/');
                if (!int.TryParse(segments.Last(), out int id)) 
                    continue;

                var imageUrl = $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork/{id}.png";
                var types = await _pokeApiService.GetPokemonTypesAsync(id);

                allGridItems.Add(new PokemonGridItemViewModel
                {
                    Id       = id,
                    Name     = basic.Name,
                    ImageUrl = imageUrl,
                    Types    = types
                });
            }

            // 2. Aplicar filtro de nombre (si existe)
            var query = allGridItems.AsQueryable();
            if (!string.IsNullOrEmpty(vm.NameFilter))
                query = query.Where(p => p.Name.Contains(vm.NameFilter!, StringComparison.OrdinalIgnoreCase));

            // 3. Crear lista paginada usando helper PaginatedList
            return await PaginatedList<PokemonGridItemViewModel>
                .CreateAsync(query, vm.PageNumber, vm.PageSize);
        }

        #endregion
    }
}
