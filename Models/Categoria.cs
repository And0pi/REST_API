namespace API_REST.Models;

public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public DateTime CreadoEn { get; set; }

    public List<Producto> Productos { get; set; } = [];
}
