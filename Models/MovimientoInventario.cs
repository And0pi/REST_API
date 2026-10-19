namespace API_REST.Models;

public enum TipoMovimiento
{
    /// <summary>Suma unidades al stock (compra, devolución de cliente...).</summary>
    Entrada,
    /// <summary>Resta unidades del stock (venta, merma...).</summary>
    Salida,
    /// <summary>Fija el stock al valor contado físicamente.</summary>
    Ajuste
}

public class MovimientoInventario
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public TipoMovimiento Tipo { get; set; }

    /// <summary>Unidades movidas (Entrada/Salida) o stock contado (Ajuste).</summary>
    public int Cantidad { get; set; }
    public int StockAnterior { get; set; }
    public int StockResultante { get; set; }
    public string? Motivo { get; set; }
    public DateTime Fecha { get; set; }
}
