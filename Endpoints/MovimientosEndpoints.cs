using API_REST.Aplicacion;
using API_REST.Dtos;
using API_REST.Infraestructura;
using Microsoft.AspNetCore.Http.HttpResults;

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
            .WithTags("Movimientos de inventario").WithSummary("Consulta movimientos de inventario")
            .WithDescription("Filtros: productoId, tipo, desde, hasta (fechas ISO 8601). Ordenado del más reciente al más antiguo.");
    }

    private static async Task<Results<Created<MovimientoDto>, ProblemHttpResult>> Registrar(
        int productoId, CrearMovimientoDto dto, ICasosDeUsoMovimientos casos, CancellationToken ct)
    {
        var resultado = await casos.RegistrarAsync(productoId, dto, ct);
        return resultado.Error is { } error
            ? EndpointResultados.Problema(error)
            : TypedResults.Created($"/api/movimientos?productoId={productoId}", resultado.Valor!);
    }

    private static async Task<Results<Ok<PaginaResultado<MovimientoDto>>, ProblemHttpResult>> ListarDeProducto(
        int productoId, [AsParameters] ConsultaMovimientos consulta, ICasosDeUsoMovimientos casos, CancellationToken ct)
    {
        var resultado = await casos.ListarDeProductoAsync(productoId, consulta, ct);
        return resultado.Error is { } error
            ? EndpointResultados.Problema(error)
            : TypedResults.Ok(resultado.Valor!);
    }

    private static async Task<Ok<PaginaResultado<MovimientoDto>>> Listar(
        [AsParameters] ConsultaMovimientos consulta, ICasosDeUsoMovimientos casos, CancellationToken ct) =>
        TypedResults.Ok(await casos.ListarAsync(consulta, ct));
}