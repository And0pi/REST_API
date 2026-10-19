using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using API_REST.Models;

namespace API_REST.Dtos;

public class CrearMovimientoDto : IValidatableObject
{
    [Required(ErrorMessage = "El tipo es obligatorio (Entrada, Salida o Ajuste).")]
    public TipoMovimiento? Tipo { get; init; }

    /// <summary>Unidades a sumar/restar, o el stock contado si el tipo es Ajuste.</summary>
    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(0, 1_000_000, ErrorMessage = "La cantidad debe estar entre {1} y {2}.")]
    public int? Cantidad { get; init; }

    [StringLength(250, ErrorMessage = "El motivo no puede superar los {1} caracteres.")]
    public string? Motivo { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((Tipo is TipoMovimiento.Entrada or TipoMovimiento.Salida) && Cantidad == 0)
            yield return new ValidationResult("La cantidad debe ser mayor que 0 en entradas y salidas.", [nameof(Cantidad)]);
    }
}

public record ConsultaMovimientos(
    [property: Range(1, PaginacionExtensions.MaxPagina, ErrorMessage = "La página debe estar entre {1} y {2}.")]
    int Pagina = 1,
    [property: Range(1, PaginacionExtensions.MaxTamanoPagina, ErrorMessage = "El tamaño de página debe estar entre {1} y {2}.")]
    int TamanoPagina = 20,
    int? ProductoId = null,
    TipoMovimiento? Tipo = null,
    DateTimeOffset? Desde = null,
    DateTimeOffset? Hasta = null) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Desde > Hasta)
            yield return new ValidationResult("'desde' no puede ser posterior a 'hasta'.", [nameof(Desde)]);
    }
}

public record MovimientoDto(
    int Id,
    int ProductoId,
    string ProductoSku,
    string ProductoNombre,
    TipoMovimiento Tipo,
    int Cantidad,
    int StockAnterior,
    int StockResultante,
    string? Motivo,
    DateTime Fecha)
{
    public static readonly Expression<Func<MovimientoInventario, MovimientoDto>> Proyeccion = m => new MovimientoDto(
        m.Id, m.ProductoId, m.Producto.Sku, m.Producto.Nombre, m.Tipo,
        m.Cantidad, m.StockAnterior, m.StockResultante, m.Motivo, m.Fecha);
}
