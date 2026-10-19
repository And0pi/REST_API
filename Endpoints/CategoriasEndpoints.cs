using API_REST.Aplicacion;
using API_REST.Dtos;
using API_REST.Infraestructura;
using Microsoft.AspNetCore.Http.HttpResults;

namespace API_REST.Endpoints;

public static class CategoriasEndpoints
{
    public static void MapCategorias(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/categorias").WithTags("Categorías");
        grupo.MapGet("/", Listar).WithSummary("Lista todas las categorías");
        grupo.MapGet("/{id:int}", Obtener).WithSummary("Consulta una categoría");
        grupo.MapPost("/", Crear).Validar<GuardarCategoriaDto>().WithSummary("Crea una categoría");
        grupo.MapPut("/{id:int}", Actualizar).Validar<GuardarCategoriaDto>().WithSummary("Actualiza una categoría");
        grupo.MapDelete("/{id:int}", Eliminar).WithSummary("Elimina una categoría")
            .WithDescription("Los productos que tuviera asignados quedan sin categoría.");
    }

    private static async Task<Ok<List<CategoriaDto>>> Listar(ICasosDeUsoCategorias casos, CancellationToken ct) =>
        TypedResults.Ok(await casos.ListarAsync(ct));

    private static async Task<Results<Ok<CategoriaDto>, ProblemHttpResult>> Obtener(
        int id, ICasosDeUsoCategorias casos, CancellationToken ct)
    {
        var resultado = await casos.ObtenerAsync(id, ct);
        return resultado.Error is { } error
            ? EndpointResultados.Problema(error)
            : TypedResults.Ok(resultado.Valor!);
    }

    private static async Task<Results<Created<CategoriaDto>, ProblemHttpResult>> Crear(
        GuardarCategoriaDto dto, ICasosDeUsoCategorias casos, CancellationToken ct)
    {
        var resultado = await casos.CrearAsync(dto, ct);
        return resultado.Error is { } error
            ? EndpointResultados.Problema(error)
            : TypedResults.Created($"/api/categorias/{resultado.Valor!.Id}", resultado.Valor);
    }

    private static async Task<Results<Ok<CategoriaDto>, ProblemHttpResult>> Actualizar(
        int id, GuardarCategoriaDto dto, ICasosDeUsoCategorias casos, CancellationToken ct)
    {
        var resultado = await casos.ActualizarAsync(id, dto, ct);
        return resultado.Error is { } error
            ? EndpointResultados.Problema(error)
            : TypedResults.Ok(resultado.Valor!);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Eliminar(
        int id, ICasosDeUsoCategorias casos, CancellationToken ct)
    {
        var resultado = await casos.EliminarAsync(id, ct);
        return resultado.Error is { } error
            ? EndpointResultados.Problema(error)
            : TypedResults.NoContent();
    }
}