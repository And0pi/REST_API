using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using API_REST.Models;

namespace API_REST.Dtos;

public class GuardarCategoriaDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
    public string Nombre { get; init; } = "";

    [StringLength(500, ErrorMessage = "La descripción no puede superar los {1} caracteres.")]
    public string? Descripcion { get; init; }
}

public record CategoriaDto(int Id, string Nombre, string? Descripcion, int CantidadProductos, DateTime CreadoEn)
{
    public static readonly Expression<Func<Categoria, CategoriaDto>> Proyeccion = c =>
        new CategoriaDto(c.Id, c.Nombre, c.Descripcion, c.Productos.Count(p => p.Activo), c.CreadoEn);
}
