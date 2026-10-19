using API_REST.Aplicacion;
using API_REST.Dtos;
using API_REST.Infraestructura;
using Microsoft.AspNetCore.Http.HttpResults;

namespace API_REST.Endpoints;

public static class ProductosEndpoints
{
    public static void MapProductos(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/productos").WithTags("Productos");
        grupo.MapGet("/", Listar).Validar<ConsultaProductos>().WithSummary("Lista productos con paginación")
            .WithDescription("Por defecto solo muestra productos activos. Filtros: buscar, categoriaId, stockMaximo, incluirInactivos.");
        grupo.MapGet("/{id:int}", Obtener).WithSummary("Consulta un producto por su id");
        grupo.MapPost("/", Crear).Validar<CrearProductoDto>().WithSummary("Registra un producto")
            .WithDescription("Si se indica stockInicial > 0 se registra automáticamente una entrada de inventario.");
        grupo.MapPut("/{id:int}", Actualizar).Validar<ActualizarProductoDto>()
            .WithSummary("Actualiza nombre, precio, descripción y categoría")
            .WithDescription("El stock no se modifica aquí: usa los movimientos de inventario.");
        grupo.MapDelete("/{id:int}", Eliminar)
            .WithSummary("Desactiva un producto (o lo elimina con ?permanente=true)")
            .WithDescription("El borrado permanente solo se permite si el producto no tiene movimientos de inventario.");
        grupo.MapPost("/{id:int}/activar", Activar).WithSummary("Reactiva un producto desactivado");
    }

    private static async Task<Ok<PaginaResultado<ProductoDto>>> Listar(
        [AsParameters] ConsultaProductos consulta, ICasosDeUsoProductos casos, CancellationToken ct) =>
        TypedResults.Ok(await casos.ListarAsync(consulta, ct));

    private static async Task<Results<Ok<ProductoDto>, ProblemHttpResult>> Obtener(
        int id, ICasosDeUsoProductos casos, CancellationToken ct)
    {
        var resultado = await casos.ObtenerAsync(id, ct);
        return resultado.Error is { } error
            ? EndpointResultados.Problema(error)
            : TypedResults.Ok(resultado.Valor!);
    }

    private static async Task<Results<Created<ProductoDto>, ValidationProblem, ProblemHttpResult>> Crear(
        CrearProductoDto dto, ICasosDeUsoProductos casos, CancellationToken ct)
    {
        var resultado = await casos.CrearAsync(dto, ct);
        if (resultado.Error is { } error)
            return error.Errores is not null ? EndpointResultados.Validacion(error) : EndpointResultados.Problema(error);
        return TypedResults.Created($"/api/productos/{resultado.Valor!.Id}", resultado.Valor);
    }

    private static async Task<Results<Ok<ProductoDto>, ValidationProblem, ProblemHttpResult>> Actualizar(
        int id, ActualizarProductoDto dto, ICasosDeUsoProductos casos, CancellationToken ct)
    {
        var resultado = await casos.ActualizarAsync(id, dto, ct);
        if (resultado.Error is { } error)
            return error.Errores is not null ? EndpointResultados.Validacion(error) : EndpointResultados.Problema(error);
        return TypedResults.Ok(resultado.Valor!);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Eliminar(
        int id, ICasosDeUsoProductos casos, CancellationToken ct, bool permanente = false)
    {
        var resultado = await casos.EliminarAsync(id, permanente, ct);
        return resultado.Error is { } error
            ? EndpointResultados.Problema(error)
            : TypedResults.NoContent();
    }

    private static async Task<Results<Ok<ProductoDto>, ProblemHttpResult>> Activar(
        int id, ICasosDeUsoProductos casos, CancellationToken ct)
    {
        var resultado = await casos.ActivarAsync(id, ct);
        return resultado.Error is { } error
            ? EndpointResultados.Problema(error)
            : TypedResults.Ok(resultado.Valor!);
    }
}