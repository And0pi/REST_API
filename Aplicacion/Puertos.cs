using API_REST.Dtos;
using API_REST.Models;

namespace API_REST.Aplicacion;

public sealed record ErrorAplicacion(int Estado, string Detalle, IReadOnlyDictionary<string, string[]>? Errores = null);

public sealed record Resultado<T>(T? Valor, ErrorAplicacion? Error)
{
    public static Resultado<T> Exito(T valor) => new(valor, null);
    public static Resultado<T> Fallo(int estado, string detalle, IReadOnlyDictionary<string, string[]>? errores = null) =>
        new(default, new ErrorAplicacion(estado, detalle, errores));
}

public interface ICategoriasRepositorio
{
    Task<List<CategoriaDto>> ListarAsync(CancellationToken ct);
    Task<CategoriaDto?> BuscarCategoriaDtoAsync(int id, CancellationToken ct);
    Task<Categoria?> BuscarCategoriaAsync(int id, CancellationToken ct);
    Task<bool> NombreEnUsoAsync(string nombre, int? excluirId, CancellationToken ct);
    void Agregar(Categoria categoria);
    Task<bool> EliminarAsync(int id, CancellationToken ct);
    Task GuardarAsync(CancellationToken ct);
}

public interface IProductosRepositorio
{
    Task<PaginaResultado<ProductoDto>> ListarAsync(ConsultaProductos consulta, CancellationToken ct);
    Task<ProductoDto?> BuscarProductoDtoAsync(int id, CancellationToken ct);
    Task<Producto?> BuscarProductoAsync(int id, CancellationToken ct);
    Task<bool> SkuEnUsoAsync(string sku, CancellationToken ct);
    Task<bool> CategoriaExisteAsync(int id, CancellationToken ct);
    Task<bool> TieneMovimientosAsync(int id, CancellationToken ct);
    void Agregar(Producto producto);
    Task EliminarAsync(Producto producto, CancellationToken ct);
    Task GuardarAsync(CancellationToken ct);
}

public interface ITransaccionMovimiento : IAsyncDisposable
{
    Task<Producto?> BloquearProductoAsync(int productoId, CancellationToken ct);
    void Agregar(MovimientoInventario movimiento);
    Task GuardarYConfirmarAsync(CancellationToken ct);
}

public interface IMovimientosRepositorio
{
    Task<ITransaccionMovimiento> IniciarTransaccionAsync(CancellationToken ct);
    Task<bool> ProductoExisteAsync(int productoId, CancellationToken ct);
    Task<PaginaResultado<MovimientoDto>> ListarAsync(ConsultaMovimientos consulta, CancellationToken ct);
}

public interface ICasosDeUsoCategorias
{
    Task<List<CategoriaDto>> ListarAsync(CancellationToken ct);
    Task<Resultado<CategoriaDto>> ObtenerAsync(int id, CancellationToken ct);
    Task<Resultado<CategoriaDto>> CrearAsync(GuardarCategoriaDto dto, CancellationToken ct);
    Task<Resultado<CategoriaDto>> ActualizarAsync(int id, GuardarCategoriaDto dto, CancellationToken ct);
    Task<Resultado<bool>> EliminarAsync(int id, CancellationToken ct);
}

public interface ICasosDeUsoProductos
{
    Task<PaginaResultado<ProductoDto>> ListarAsync(ConsultaProductos consulta, CancellationToken ct);
    Task<Resultado<ProductoDto>> ObtenerAsync(int id, CancellationToken ct);
    Task<Resultado<ProductoDto>> CrearAsync(CrearProductoDto dto, CancellationToken ct);
    Task<Resultado<ProductoDto>> ActualizarAsync(int id, ActualizarProductoDto dto, CancellationToken ct);
    Task<Resultado<bool>> EliminarAsync(int id, bool permanente, CancellationToken ct);
    Task<Resultado<ProductoDto>> ActivarAsync(int id, CancellationToken ct);
}

public interface ICasosDeUsoMovimientos
{
    Task<Resultado<MovimientoDto>> RegistrarAsync(int productoId, CrearMovimientoDto dto, CancellationToken ct);
    Task<Resultado<PaginaResultado<MovimientoDto>>> ListarDeProductoAsync(int productoId, ConsultaMovimientos consulta, CancellationToken ct);
    Task<PaginaResultado<MovimientoDto>> ListarAsync(ConsultaMovimientos consulta, CancellationToken ct);
}