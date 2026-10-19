using API_REST.Data;
using API_REST.Dtos;
using API_REST.Infraestructura;
using API_REST.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

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
        grupo.MapDelete("/{id:int}", Eliminar)
             .WithSummary("Elimina una categoría")
             .WithDescription("Los productos que tuviera asignados quedan sin categoría.");
    }

    static async Task<Ok<List<CategoriaDto>>> Listar(InventarioDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.Categorias.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(CategoriaDto.Proyeccion)
            .ToListAsync(ct));

    static async Task<Results<Ok<CategoriaDto>, ProblemHttpResult>> Obtener(int id, InventarioDbContext db, CancellationToken ct) =>
        await BuscarDto(db, id, ct) is { } dto
            ? TypedResults.Ok(dto)
            : Errores.NoEncontrado($"No existe la categoría {id}.");

    static async Task<Results<Created<CategoriaDto>, ProblemHttpResult>> Crear(
        GuardarCategoriaDto dto, InventarioDbContext db, CancellationToken ct)
    {
        var nombre = dto.Nombre.Trim();
        if (await NombreEnUso(db, nombre, null, ct))
            return Errores.Conflicto($"Ya existe una categoría llamada '{nombre}'.");

        var categoria = new Categoria { Nombre = nombre, Descripcion = dto.Descripcion?.Trim(), CreadoEn = DateTime.UtcNow };
        db.Categorias.Add(categoria);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/categorias/{categoria.Id}", (await BuscarDto(db, categoria.Id, ct))!);
    }

    static async Task<Results<Ok<CategoriaDto>, ProblemHttpResult>> Actualizar(
        int id, GuardarCategoriaDto dto, InventarioDbContext db, CancellationToken ct)
    {
        var categoria = await db.Categorias.FindAsync([id], ct);
        if (categoria is null)
            return Errores.NoEncontrado($"No existe la categoría {id}.");

        var nombre = dto.Nombre.Trim();
        if (await NombreEnUso(db, nombre, id, ct))
            return Errores.Conflicto($"Ya existe una categoría llamada '{nombre}'.");

        categoria.Nombre = nombre;
        categoria.Descripcion = dto.Descripcion?.Trim();
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok((await BuscarDto(db, id, ct))!);
    }

    static async Task<Results<NoContent, ProblemHttpResult>> Eliminar(int id, InventarioDbContext db, CancellationToken ct)
    {
        var borradas = await db.Categorias.Where(c => c.Id == id).ExecuteDeleteAsync(ct);
        return borradas > 0 ? TypedResults.NoContent() : Errores.NoEncontrado($"No existe la categoría {id}.");
    }

    static Task<CategoriaDto?> BuscarDto(InventarioDbContext db, int id, CancellationToken ct) =>
        db.Categorias.AsNoTracking().Where(c => c.Id == id).Select(CategoriaDto.Proyeccion).FirstOrDefaultAsync(ct);

    static Task<bool> NombreEnUso(InventarioDbContext db, string nombre, int? excluirId, CancellationToken ct) =>
        db.Categorias.AnyAsync(c => c.Nombre.ToLower() == nombre.ToLower() && c.Id != excluirId, ct);
}
