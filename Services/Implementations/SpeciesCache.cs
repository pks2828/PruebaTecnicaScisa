using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;

namespace MiPokemonApp.Services.Implementations
{
    public class SpeciesCache
    {
        private readonly IMemoryCache _memoryCache;
        private const string SpeciesCachePrefix = "Species_";
        private const string TypeOptionsCacheKey = "TypeOptions";

        public SpeciesCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        // Cache para especie individual por ID
        public void SetSpeciesName(int id, string name)
        {
            var options = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(12));
            _memoryCache.Set($"{SpeciesCachePrefix}{id}", name, options);
        }

        public bool TryGetSpeciesName(int id, out string speciesName)
        {
            return _memoryCache.TryGetValue($"{SpeciesCachePrefix}{id}", out speciesName);
        }

        // Cache para lista completa de tipos
        public void SetTypeOptions(List<string> types)
        {
            var options = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(12));
            _memoryCache.Set(TypeOptionsCacheKey, types, options);
        }

        public bool TryGetTypeOptions(out List<string> types)
        {
            return _memoryCache.TryGetValue(TypeOptionsCacheKey, out types);
        }
    }
}
