using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;

namespace API_REST.Infraestructura;

public static class Validacion
{
    /// <summary>
    /// Valida con DataAnnotations (e IValidatableObject) los argumentos de tipo <typeparamref name="T"/>
    /// antes de ejecutar el endpoint. Si hay errores responde 400 con un ValidationProblem.
    /// </summary>
    public static RouteHandlerBuilder Validar<T>(this RouteHandlerBuilder builder) where T : class =>
        builder
            .AddEndpointFilter(async (contexto, siguiente) =>
            {
                foreach (var argumento in contexto.Arguments.OfType<T>())
                {
                    var resultados = new List<ValidationResult>();
                    if (!Validator.TryValidateObject(argumento, new ValidationContext(argumento), resultados, validateAllProperties: true))
                        return TypedResults.ValidationProblem(Agrupar(resultados), title: "Los datos enviados no son válidos.");
                }
                return await siguiente(contexto);
            })
            .ProducesValidationProblem();

    private static Dictionary<string, string[]> Agrupar(IEnumerable<ValidationResult> resultados) =>
        resultados
            .SelectMany(r => r.MemberNames.DefaultIfEmpty("").Select(campo => (Campo: CamelCase(campo), Mensaje: r.ErrorMessage ?? "Valor no válido.")))
            .GroupBy(x => x.Campo)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Mensaje).ToArray());

    private static string CamelCase(string nombre) => JsonNamingPolicy.CamelCase.ConvertName(nombre);
}

/// <summary>Respuestas de error con formato ProblemDetails (RFC 9457).</summary>
public static class Errores
{
    public static ProblemHttpResult NoEncontrado(string detalle) =>
        TypedResults.Problem(detalle, statusCode: StatusCodes.Status404NotFound, title: "Recurso no encontrado.");

    public static ProblemHttpResult Conflicto(string detalle) =>
        TypedResults.Problem(detalle, statusCode: StatusCodes.Status409Conflict, title: "La operación entra en conflicto con el estado actual.");

    public static ValidationProblem Validacion(string campo, string mensaje) =>
        TypedResults.ValidationProblem(
            new Dictionary<string, string[]> { [campo] = [mensaje] },
            title: "Los datos enviados no son válidos.");
}
