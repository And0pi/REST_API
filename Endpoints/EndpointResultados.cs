using API_REST.Aplicacion;
using API_REST.Infraestructura;
using Microsoft.AspNetCore.Http.HttpResults;

namespace API_REST.Endpoints;

internal static class EndpointResultados
{
    public static ProblemHttpResult Problema(ErrorAplicacion error) => error.Estado switch
    {
        404 => Errores.NoEncontrado(error.Detalle),
        409 => Errores.Conflicto(error.Detalle),
        _ => TypedResults.Problem(error.Detalle, statusCode: error.Estado)
    };

    public static ValidationProblem Validacion(ErrorAplicacion error) =>
        TypedResults.ValidationProblem(error.Errores ?? new Dictionary<string, string[]>(),
            title: "Los datos enviados no son válidos.");
}