using API_REST.Data;
using API_REST.Dtos;
using API_REST.Infraestructura;
using API_REST.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace API_REST.Endpoints;

public static class MovimientosEndpoints
{
    public static void MapMovimientos(this IEndpointRouteBuilder app)
    {
        var porProducto = app.MapGroup("/api/productos/{productoId:int}/movimientos").WithTags("Movimientos de inventario");

        porProducto.MapPost("/", Registrar).Validar<CrearMovimientoDto>()
            .WithSummary("Registra una entrada, salida o ajuste de stock")
            .WithDescription("Entrada suma 'cantidad', Salida la resta y Ajuste fija el stock al valor contado.");
        porProducto.MapGet("/", ListarDeProducto).Validar<ConsultaMovimientos>()
            .WithSummary("Historial de movimientos de un producto");

        app.MapGet("/api/movimientos", Listar).Validar<ConsultaMovimientos>()
            .WithTags("Movimientos de inventario")
            .WithSummary("Consulta movimientos de inventario")
            .WithDescription("Filtros: productoId, tipo, desde, hasta (fechas ISO 8601). Ordenado del más reciente al más antiguo.");
    }

    static async Task<Results<Created<MovimientoDto>, ProblemHttpResult>> Registrar(
        int productoId, CrearMovimientoDto dto, InventarioDbContext db, CancellationToken ct)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        // Bloquea la fila del producto para que dos movimientos simultáneos no pisen el stock.
        var producto = (await db.Productos
            .FromSql($"SELECT * FROM productos WHERE id = {productoId} FOR UPDATE")
            .ToListAsync(ct)).FirstOrDefault();

        if (producto is null)
            return Errores.NoEncontrado($"No existe el producto {productoId}.");
        if (!producto.Activo)
            return Errores.Conflicto("El producto está desactivado y no admite movimientos.");

        var tipo = dto.Tipo!.Value;
        var cantidad = dto.Cantidad!.Value;
        var anterior = producto.Stock;
        long resultante = tipo switch
        {
            TipoMovimiento.Entrada => (long)anterior + cantidad,
            TipoMovimiento.Salida => (long)anterior - cantidad,
            _ => cantidad
        };

        if (resultante < 0)
            return Errores.Conflicto($"Stock insuficiente: hay {anterior} unidades y se intentan retirar {cantidad}.");
        if (resultante > int.MaxValue)
            return Errores.Conflicto("El stock resultante supera el máximo permitido.");

        var ahora = DateTime.UtcNow;
        producto.Stock = (int)resultante;
        producto.ActualizadoEn = ahora;
        var movimiento = new MovimientoInventario
        {
            ProductoId = productoId,
            Tipo = tipo,
            Cantidad = cantidad,
            StockAnterior = anterior,
            StockResultante = producto.Stock,
            Motivo = string.IsNullOrWhiteSpace(dto.Motivo) ? null : dto.Motivo.Trim(),
            Fecha = ahora
        };
        db.Movimientos.Add(movimiento);

        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);

        var resultado = new MovimientoDto(movimiento.Id, producto.Id, producto.Sku, producto.Nombre, tipo,
            cantidad, anterior, producto.Stock, movimiento.Motivo, ahora);
        return TypedResults.Created($"/api/movimientos?productoId={productoId}", resultado);
    }

    static async Task<Results<Ok<PaginaResultado<MovimientoDto>>, ProblemHttpResult>> ListarDeProducto(
        int productoId, [AsParameters] ConsultaMovimientos consulta, InventarioDbContext db, CancellationToken ct)
    {
        if (!await db.Productos.AnyAsync(p => p.Id == productoId, ct))
            return Errores.NoEncontrado($"No existe el producto {productoId}.");

        return TypedResults.Ok(await Consultar(db, consulta with { ProductoId = productoId }, ct));
    }

    static async Task<Ok<PaginaResultado<MovimientoDto>>> Listar(
        [AsParameters] ConsultaMovimientos consulta, InventarioDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await Consultar(db, consulta, ct));

    static Task<PaginaResultado<MovimientoDto>> Consultar(InventarioDbContext db, ConsultaMovimientos consulta, CancellationToken ct)
    {
        var query = db.Movimientos.AsNoTracking();

        if (consulta.ProductoId is int productoId)
            query = query.Where(m => m.ProductoId == productoId);
        if (consulta.Tipo is TipoMovimiento tipo)
            query = query.Where(m => m.Tipo == tipo);
        if (consulta.Desde is DateTimeOffset desde)
            query = query.Where(m => m.Fecha >= desde.UtcDateTime);
        if (consulta.Hasta is DateTimeOffset hasta)
            query = query.Where(m => m.Fecha <= hasta.UtcDateTime);

        return query
            .OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id)
            .Select(MovimientoDto.Proyeccion)
            .PaginarAsync(consulta.Pagina, consulta.TamanoPagina, ct);
    }
}
