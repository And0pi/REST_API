using API_REST.Aplicacion;
using API_REST.Data;
using API_REST.Dtos;
using API_REST.Models;
using Microsoft.EntityFrameworkCore;

namespace API_REST.InfraestructuraDatos;

internal sealed class RepositorioInventario(InventarioDbContext db) :
    ICategoriasRepositorio, IProductosRepositorio, IMovimientosRepositorio
{
    public Task<List<CategoriaDto>> ListarAsync(CancellationToken ct) => db.Categorias.AsNoTracking()
        .OrderBy(c => c.Nombre).Select(CategoriaDto.Proyeccion).ToListAsync(ct);

    public Task<CategoriaDto?> BuscarCategoriaDtoAsync(int id, CancellationToken ct) => db.Categorias.AsNoTracking()
        .Where(c => c.Id == id).Select(CategoriaDto.Proyeccion).FirstOrDefaultAsync(ct);

    public Task<Categoria?> BuscarCategoriaAsync(int id, CancellationToken ct) => db.Categorias.FindAsync([id], ct).AsTask();

    public Task<bool> NombreEnUsoAsync(string nombre, int? excluirId, CancellationToken ct) =>
        db.Categorias.AnyAsync(c => c.Nombre.ToLower() == nombre.ToLower() && c.Id != excluirId, ct);

    public void Agregar(Categoria categoria) => db.Categorias.Add(categoria);

    public async Task<bool> EliminarAsync(int id, CancellationToken ct) =>
        await db.Categorias.Where(c => c.Id == id).ExecuteDeleteAsync(ct) > 0;

    public async Task<PaginaResultado<ProductoDto>> ListarAsync(ConsultaProductos consulta, CancellationToken ct)
    {
        var query = db.Productos.AsNoTracking();
        if (!consulta.IncluirInactivos) query = query.Where(p => p.Activo);
        if (consulta.CategoriaId is int categoriaId) query = query.Where(p => p.CategoriaId == categoriaId);
        if (consulta.StockMaximo is int stockMaximo) query = query.Where(p => p.Stock <= stockMaximo);
        if (!string.IsNullOrWhiteSpace(consulta.Buscar))
        {
            var patron = $"%{consulta.Buscar.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.Nombre, patron) || EF.Functions.ILike(p.Sku, patron));
        }
        return await PaginarAsync(query.OrderBy(p => p.Nombre).ThenBy(p => p.Id).Select(ProductoDto.Proyeccion),
            consulta.Pagina, consulta.TamanoPagina, ct);
    }

    public Task<ProductoDto?> BuscarProductoDtoAsync(int id, CancellationToken ct) => db.Productos.AsNoTracking()
        .Where(p => p.Id == id).Select(ProductoDto.Proyeccion).FirstOrDefaultAsync(ct);

    public Task<Producto?> BuscarProductoAsync(int id, CancellationToken ct) => db.Productos.FindAsync([id], ct).AsTask();

    public Task<bool> SkuEnUsoAsync(string sku, CancellationToken ct) => db.Productos.AnyAsync(p => p.Sku == sku, ct);

    public Task<bool> CategoriaExisteAsync(int id, CancellationToken ct) => db.Categorias.AnyAsync(c => c.Id == id, ct);

    public Task<bool> TieneMovimientosAsync(int id, CancellationToken ct) => db.Movimientos.AnyAsync(m => m.ProductoId == id, ct);

    public void Agregar(Producto producto) => db.Productos.Add(producto);

    public async Task EliminarAsync(Producto producto, CancellationToken ct)
    {
        db.Productos.Remove(producto);
        await db.SaveChangesAsync(ct);
    }

    public Task GuardarAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task<ITransaccionMovimiento> IniciarTransaccionAsync(CancellationToken ct) =>
        new TransaccionMovimiento(db, await db.Database.BeginTransactionAsync(ct));

    public Task<bool> ProductoExisteAsync(int productoId, CancellationToken ct) =>
        db.Productos.AnyAsync(p => p.Id == productoId, ct);

    public async Task<PaginaResultado<MovimientoDto>> ListarAsync(ConsultaMovimientos consulta, CancellationToken ct)
    {
        var query = db.Movimientos.AsNoTracking();
        if (consulta.ProductoId is int productoId) query = query.Where(m => m.ProductoId == productoId);
        if (consulta.Tipo is TipoMovimiento tipo) query = query.Where(m => m.Tipo == tipo);
        if (consulta.Desde is DateTimeOffset desde) query = query.Where(m => m.Fecha >= desde.UtcDateTime);
        if (consulta.Hasta is DateTimeOffset hasta) query = query.Where(m => m.Fecha <= hasta.UtcDateTime);
        return await PaginarAsync(query.OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id)
            .Select(MovimientoDto.Proyeccion), consulta.Pagina, consulta.TamanoPagina, ct);
    }

    private static async Task<PaginaResultado<T>> PaginarAsync<T>(IQueryable<T> query, int pagina, int tamanoPagina, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var items = await query.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToListAsync(ct);
        var totalPaginas = (int)Math.Ceiling(total / (double)tamanoPagina);
        return new PaginaResultado<T>(items, pagina, tamanoPagina, total, totalPaginas);
    }

    private sealed class TransaccionMovimiento(InventarioDbContext contexto, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaccion)
        : ITransaccionMovimiento
    {
        public async Task<Producto?> BloquearProductoAsync(int productoId, CancellationToken ct) =>
            (await contexto.Productos.FromSql($"SELECT * FROM productos WHERE id = {productoId} FOR UPDATE").ToListAsync(ct)).FirstOrDefault();

        public void Agregar(MovimientoInventario movimiento) => contexto.Movimientos.Add(movimiento);

        public async Task GuardarYConfirmarAsync(CancellationToken ct)
        {
            await contexto.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
        }

        public ValueTask DisposeAsync() => transaccion.DisposeAsync();
    }
}