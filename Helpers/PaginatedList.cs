using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiPokemonApp.Helpers
{
    /// <summary>
    /// Clase que encapsula una lista paginada y el cálculo de páginas.
    /// </summary>
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }   // Página actual (1-based)
        public int TotalPages { get; private set; }  // Número total de páginas
        public int PageSize { get; private set; }    // Tamaño de página
        public int TotalCount { get; private set; }  // Conteo total de elementos

        /// <summary>
        /// Lista de números de página que se mostrarán en el paginador (p. ej. [3,4,5,6,7]).
        /// </summary>
        public List<int> PageNumbers { get; private set; }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        private PaginatedList(IEnumerable<T> items, int count, int pageIndex, int pageSize, int maxPagesToShow)
        {
            TotalCount = count;
            PageSize = pageSize;
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            // Añadir los elementos de la página actual
            AddRange(items);

            // Calcular el rango de páginas a mostrar (ventaneo/window)
            int halfWindow = maxPagesToShow / 2;
            int startPage = Math.Max(1, PageIndex - halfWindow);
            int endPage = Math.Min(TotalPages, startPage + maxPagesToShow - 1);

            if (endPage - startPage + 1 < maxPagesToShow)
                startPage = Math.Max(1, endPage - maxPagesToShow + 1);

            PageNumbers = Enumerable.Range(startPage, endPage - startPage + 1).ToList();
        }

        /// <summary>
        /// Crea la lista paginada a partir de un IQueryable fuente.
        /// </summary>
        public static async Task<PaginatedList<T>> CreateAsync(
            IQueryable<T> source,
            int pageIndex,
            int pageSize,
            int maxPagesToShow = 5)
        {
            // 1. Contar total de elementos en la fuente
            int count = source.Count();

            // 2. Obtener solo los elementos de la página actual
            var items = source
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 3. Construir y devolver el PaginatedList
            return await Task.FromResult(
               new PaginatedList<T>(items, count, pageIndex, pageSize, maxPagesToShow)
            );
        }

        /// <summary>
        /// Crea la lista paginada a partir de los items ya “cortados” (página actual) y el conteo total real.
        /// Útil cuando ya tienes en memoria solo los elementos de la página (por ejemplo, allGridItems con Skip/Take manual),
        /// pero quieres que el helper calcule el rango de páginas (PageNumbers) con ventana.
        /// </summary>
        public static Task<PaginatedList<T>> CreateFromPage(
            IEnumerable<T> pageItems,
            int totalCount,
            int pageIndex,
            int pageSize,
            int maxPagesToShow = 5)
        {
            // Como el constructor de PaginatedList es privado, podemos invocarlo aquí:
            return Task.FromResult(
                new PaginatedList<T>(pageItems, totalCount, pageIndex, pageSize, maxPagesToShow)
            );
        }

        // (Opcional) Si quieres mantener una sobrecarga para IEnumerable completo:
        public static Task<PaginatedList<T>> CreateAsync(
            IEnumerable<T> source,
            int pageIndex,
            int pageSize,
            int maxPagesToShow = 5)
        {
            int count = source.Count();
            var items = source
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return Task.FromResult(
                new PaginatedList<T>(items, count, pageIndex, pageSize, maxPagesToShow)
            );
        }
    }
}
