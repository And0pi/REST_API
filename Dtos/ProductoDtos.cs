using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using API_REST.Models;

namespace API_REST.Dtos;

public class CrearProductoDto
{
    [Required(ErrorMessage = "El SKU es obligatorio.")]
    [StringLength(50, ErrorMessage = "El SKU no puede superar los {1} caracteres.")]
    [RegularExpression(@"^[A-Za-z0-9._\-]+$", ErrorMessage = "El SKU solo admite letras, números, puntos, guiones y guiones bajos.")]
    public string Sku { get; init; } = "";

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
    public string Nombre { get; init; } = "";

    [StringLength(1000, ErrorMessage = "La descripción no puede superar los {1} caracteres.")]
    public string? Descripcion { get; init; }

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0, 9_999_999_999.99, ErrorMessage = "El precio debe estar entre {1} y {2}.")]
    public decimal? Precio { get; init; }

    [Range(0, 1_000_000, ErrorMessage = "El stock inicial debe estar entre {1} y {2}.")]
    public int StockInicial { get; init; }

    public int? CategoriaId { get; init; }
}

public class ActualizarProductoDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
    public string Nombre { get; init; } = "";

    [StringLength(1000, ErrorMessage = "La descripción no puede superar los {1} caracteres.")]
    public string? Descripcion { get; init; }

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0, 9_999_999_999.99, ErrorMessage = "El precio debe estar entre {1} y {2}.")]
    public decimal? Precio { get; init; }

    public int? CategoriaId { get; init; }
}

public record ConsultaProductos(
    [property: Range(1, PaginacionExtensions.MaxPagina, ErrorMessage = "La página debe estar entre {1} y {2}.")]
    int Pagina = 1,
    [property: Range(1, PaginacionExtensions.MaxTamanoPagina, ErrorMessage = "El tamaño de página debe estar entre {1} y {2}.")]
    int TamanoPagina = 20,
    // Busca por nombre o SKU (sin distinguir mayúsculas).
    string? Buscar = null,
    int? CategoriaId = null,
    // Solo productos con stock menor o igual a este valor (útil para ver qué reponer).
    [property: Range(0, int.MaxValue, ErrorMessage = "El stock máximo no puede ser negativo.")]
    int? StockMaximo = null,
    bool IncluirInactivos = false);

public record ProductoDto(
    int Id,
    string Sku,
    string Nombre,
    string? Descripcion,
    decimal Precio,
    int Stock,
    bool Activo,
    int? CategoriaId,
    string? CategoriaNombre,
    DateTime CreadoEn,
    DateTime ActualizadoEn)
{
    public static readonly Expression<Func<Producto, ProductoDto>> Proyeccion = p => new ProductoDto(
        p.Id, p.Sku, p.Nombre, p.Descripcion, p.Precio, p.Stock, p.Activo,
        p.CategoriaId, p.Categoria != null ? p.Categoria.Nombre : null,
        p.CreadoEn, p.ActualizadoEn);
}
