using API_REST.Data;
using API_REST.Dtos;
using API_REST.Infraestructura;
using API_REST.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace API_REST.Endpoints;

public static class ProductosEndpoints
{
    public static void MapProductos(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/productos").WithTags("Productos");

        grupo.MapGet("/", Listar).Validar<ConsultaProductos>()
             .WithSummary("Lista productos con paginación")
             .WithDescription("Por defecto solo muestra productos activos. Filtros: buscar, categoriaId, stockMaximo, incluirInactivos.");
        grupo.MapGet("/{id:int}", Obtener).WithSummary("Consulta un producto por su id");
        grupo.MapPost("/", Crear).Validar<CrearProductoDto>()
             .WithSummary("Registra un producto")
             .WithDescription("Si se indica stockInicial > 0 se registra automáticamente una entrada de inventario.");
        grupo.MapPut("/{id:int}", Actualizar).Validar<ActualizarProductoDto>()
             .WithSummary("Actualiza nombre, precio, descripción y categoría")
             .WithDescription("El stock no se modifica aquí: usa los movimientos de inventario.");
        grupo.MapDelete("/{id:int}", Eliminar)
             .WithSummary("Desactiva un producto (o lo elimina con ?permanente=true)")
             .WithDescription("El borrado permanente solo se permite si el producto no tiene movimientos de inventario.");
        grupo.MapPost("/{id:int}/activar", Activar).WithSummary("Reactiva un producto desactivado");
    }

    static async Task<Ok<PaginaResultado<ProductoDto>>> Listar(
        [AsParameters] ConsultaProductos consulta, InventarioDbContext db, CancellationToken ct)
    {
        var query = db.Productos.AsNoTracking();

        if (!consulta.IncluirInactivos)
            query = query.Where(p => p.Activo);
        if (consulta.CategoriaId is int categoriaId)
            query = query.Where(p => p.CategoriaId == categoriaId);
        if (consulta.StockMaximo is int stockMaximo)
            query = query.Where(p => p.Stock <= stockMaximo);
        if (!string.IsNullOrWhiteSpace(consulta.Buscar))
        {
            var patron = $"%{consulta.Buscar.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.Nombre, patron) || EF.Functions.ILike(p.Sku, patron));
        }

        var pagina = await query
            .OrderBy(p => p.Nombre).ThenBy(p => p.Id)
            .Select(ProductoDto.Proyeccion)
            .PaginarAsync(consulta.Pagina, consulta.TamanoPagina, ct);

        return TypedResults.Ok(pagina);
    }

    static async Task<Results<Ok<ProductoDto>, ProblemHttpResult>> Obtener(int id, InventarioDbContext db, CancellationToken ct) =>
        await BuscarDto(db, id, ct) is { } dto
            ? TypedResults.Ok(dto)
            : Errores.NoEncontrado($"No existe el producto {id}.");

    static async Task<Results<Created<ProductoDto>, ValidationProblem, ProblemHttpResult>> Crear(
        CrearProductoDto dto, InventarioDbContext db, CancellationToken ct)
    {
        var sku = dto.Sku.Trim();
        if (await db.Productos.AnyAsync(p => p.Sku == sku, ct))
            return Errores.Conflicto($"Ya existe un producto con el SKU '{sku}'.");
        if (dto.CategoriaId is int categoriaId && !await db.Categorias.AnyAsync(c => c.Id == categoriaId, ct))
            return Errores.Validacion("categoriaId", $"No existe la categoría {categoriaId}.");

        var ahora = DateTime.UtcNow;
        var producto = new Producto
        {
            Sku = sku,
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim(),
            Precio = dto.Precio!.Value,
            Stock = dto.StockInicial,
            CategoriaId = dto.CategoriaId,
            CreadoEn = ahora,
            ActualizadoEn = ahora
        };
        if (dto.StockInicial > 0)
        {
            producto.Movimientos.Add(new MovimientoInventario
            {
                Tipo = TipoMovimiento.Entrada,
                Cantidad = dto.StockInicial,
                StockAnterior = 0,
                StockResultante = dto.StockInicial,
                Motivo = "Stock inicial",
                Fecha = ahora
            });
        }

        db.Productos.Add(producto);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/productos/{producto.Id}", (await BuscarDto(db, producto.Id, ct))!);
    }

    static async Task<Results<Ok<ProductoDto>, ValidationProblem, ProblemHttpResult>> Actualizar(
        int id, ActualizarProductoDto dto, InventarioDbContext db, CancellationToken ct)
    {
        var producto = await db.Productos.FindAsync([id], ct);
        if (producto is null)
            return Errores.NoEncontrado($"No existe el producto {id}.");
        if (dto.CategoriaId is int categoriaId && !await db.Categorias.AnyAsync(c => c.Id == categoriaId, ct))
            return Errores.Validacion("categoriaId", $"No existe la categoría {categoriaId}.");

        producto.Nombre = dto.Nombre.Trim();
        producto.Descripcion = dto.Descripcion?.Trim();
        producto.Precio = dto.Precio!.Value;
        producto.CategoriaId = dto.CategoriaId;
        producto.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok((await BuscarDto(db, id, ct))!);
    }

    static async Task<Results<NoContent, ProblemHttpResult>> Eliminar(
        int id, InventarioDbContext db, CancellationToken ct, bool permanente = false)
    {
        var producto = await db.Productos.FindAsync([id], ct);
        if (producto is null)
            return Errores.NoEncontrado($"No existe el producto {id}.");

        if (permanente)
        {
            if (await db.Movimientos.AnyAsync(m => m.ProductoId == id, ct))
                return Errores.Conflicto("El producto tiene movimientos de inventario y no puede eliminarse; desactívalo en su lugar.");
            db.Productos.Remove(producto);
        }
        else if (producto.Activo)
        {
            producto.Activo = false;
            producto.ActualizadoEn = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    static async Task<Results<Ok<ProductoDto>, ProblemHttpResult>> Activar(int id, InventarioDbContext db, CancellationToken ct)
    {
        var producto = await db.Productos.FindAsync([id], ct);
        if (producto is null)
            return Errores.NoEncontrado($"No existe el producto {id}.");

        if (!producto.Activo)
        {
            producto.Activo = true;
            producto.ActualizadoEn = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return TypedResults.Ok((await BuscarDto(db, id, ct))!);
    }

    static Task<ProductoDto?> BuscarDto(InventarioDbContext db, int id, CancellationToken ct) =>
        db.Productos.AsNoTracking().Where(p => p.Id == id).Select(ProductoDto.Proyeccion).FirstOrDefaultAsync(ct);
}
