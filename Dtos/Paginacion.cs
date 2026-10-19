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
}
