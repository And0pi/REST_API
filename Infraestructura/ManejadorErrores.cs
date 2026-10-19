using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace API_REST.Infraestructura;

/// <summary>
/// Convierte cualquier excepción no controlada en una respuesta ProblemDetails coherente,
/// sin filtrar detalles internos al cliente.
/// </summary>
public class ManejadorErrores(IProblemDetailsService problemDetails, ILogger<ManejadorErrores> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken ct)
    {
        var (estado, titulo, detalle) = Clasificar(excepcion);

        if (estado >= 500)
            logger.LogError(excepcion, "Error no controlado en {Metodo} {Ruta}", contexto.Request.Method, contexto.Request.Path);
        else
            logger.LogInformation("Solicitud rechazada ({Estado}): {Mensaje}", estado, excepcion.Message);

        contexto.Response.StatusCode = estado;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            Exception = excepcion,
            ProblemDetails = { Status = estado, Title = titulo, Detail = detalle }
        });
    }

    private static (int Estado, string Titulo, string? Detalle) Clasificar(Exception ex) => ex switch
    {
        BadHttpRequestException { InnerException: JsonException json } =>
            (400, "El cuerpo de la solicitud no es un JSON válido.", DescribirJson(json)),

        BadHttpRequestException bad =>
            (bad.StatusCode, "La solicitud no es válida.", DescribirSolicitud(bad)),

        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
            (409, "La operación entra en conflicto con el estado actual.", "Ya existe un registro con ese valor único."),

        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } } =>
            (409, "La operación entra en conflicto con el estado actual.", "El registro está relacionado con otros datos."),

        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.CheckViolation } } =>
            (409, "La operación entra en conflicto con el estado actual.", "Los datos no cumplen las reglas del inventario."),

        _ when EsFalloDeConexion(ex) =>
            (503, "La base de datos no está disponible.", "Inténtalo de nuevo en unos instantes."),

        _ => (500, "Ocurrió un error inesperado.", null)
    };

    private static string DescribirJson(JsonException json)
    {
        var campo = json.Path?.TrimStart('$', '.');
        return string.IsNullOrEmpty(campo)
            ? "Revisa el formato del JSON enviado."
            : $"Revisa el valor del campo '{campo}': tiene un tipo incorrecto o un valor no permitido.";
    }

    // Los mensajes de ASP.NET Core vienen en inglés; se traducen los casos habituales.
    private static string DescribirSolicitud(BadHttpRequestException ex)
    {
        if (ex.Message.Contains("no body was provided") || ex.Message.Contains("Required parameter"))
            return "Falta el cuerpo de la solicitud (JSON).";

        var parametro = Regex.Match(ex.Message, "Failed to bind parameter \"(?:\\S+ )?(\\w+)\" from \"(.*)\"");
        if (parametro.Success)
            return $"El valor '{parametro.Groups[2].Value}' no es válido para el parámetro '{JsonNamingPolicy.CamelCase.ConvertName(parametro.Groups[1].Value)}'.";

        return "Revisa los parámetros y el cuerpo de la solicitud.";
    }

    private static bool EsFalloDeConexion(Exception ex)
    {
        for (var actual = ex; actual is not null; actual = actual.InnerException)
            if (actual is NpgsqlException and not PostgresException || actual is TimeoutException)
                return true;
        return false;
    }
}
