using System.Text.Json.Serialization;
using API_REST.Data;
using API_REST.Endpoints;
using API_REST.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var cadenaConexion = builder.Configuration.GetConnectionString("Inventario")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'ConnectionStrings:Inventario'.");

builder.Services.AddDbContext<InventarioDbContext>(opciones =>
    opciones.UseNpgsql(cadenaConexion).UseSnakeCaseNamingConvention());

// Enums como texto ("Entrada", "Salida"...) y sin aceptar números.
builder.Services.ConfigureHttpJsonOptions(opciones =>
    opciones.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

// JSON mal formado o parámetros inválidos llegan al manejador de errores en lugar de dar un 400 vacío.
builder.Services.Configure<RouteHandlerOptions>(opciones => opciones.ThrowOnBadRequest = true);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorErrores>();

builder.Services.AddOpenApi();

// Permite consumir la API desde una aplicación web en otro dominio.
// Sin orígenes configurados se aceptan todos.
var origenesPermitidos = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
builder.Services.AddCors(opciones => opciones.AddDefaultPolicy(politica =>
{
    if (origenesPermitidos.Length == 0) politica.AllowAnyOrigin();
    else politica.WithOrigins(origenesPermitidos);
    politica.AllowAnyHeader().AllowAnyMethod();
}));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opciones => opciones.WithTitle("API de Inventario"));
    app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
}

if (app.Configuration.GetValue("BaseDatos:AplicarMigracionesAlIniciar", false))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<InventarioDbContext>().Database.MigrateAsync();
}

app.MapCategorias();
app.MapProductos();
app.MapMovimientos();

app.Run();
