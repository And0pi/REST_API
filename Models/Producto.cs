namespace API_REST.Models;

public class Producto
{
    public int Id { get; set; }

    /// <summary>Código único del producto (código de barras, referencia interna...).</summary>
    public string Sku { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }

    /// <summary>Existencias actuales. Solo cambia a través de movimientos de inventario.</summary>
    public int Stock { get; set; }

    /// <summary>Los productos desactivados no admiten movimientos y no aparecen en los listados por defecto.</summary>
    public bool Activo { get; set; } = true;

    public int? CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public DateTime CreadoEn { get; set; }
    public DateTime ActualizadoEn { get; set; }

    public List<MovimientoInventario> Movimientos { get; set; } = [];
}
