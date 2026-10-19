using Microsoft.EntityFrameworkCore;

namespace API_REST.Dtos;

public record PaginaResultado<T>(
    IReadOnlyList<T> Items,
    int Pagina,
    int TamanoPagina,
    int TotalItems,
    int TotalPaginas);

public static class PaginacionExtensions
{
    public const int MaxPagina = 1_000_000;
    public const int MaxTamanoPagina = 100;

    public static async Task<PaginaResultado<T>> PaginarAsync<T>(
        this IQueryable<T> query, int pagina, int tamanoPagina, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(ct);

        var totalPaginas = (int)Math.Ceiling(total / (double)tamanoPagina);
        return new PaginaResultado<T>(items, pagina, tamanoPagina, total, totalPaginas);
    }
}
