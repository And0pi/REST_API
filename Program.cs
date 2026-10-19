using System.Text.Json.Serialization;
using API_REST.Endpoints;
using API_REST.Infraestructura;
using API_REST.Aplicacion;
using API_REST.InfraestructuraDatos;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ICasosDeUsoCategorias, CasosDeUsoCategorias>();
builder.Services.AddScoped<ICasosDeUsoProductos, CasosDeUsoProductos>();
builder.Services.AddScoped<ICasosDeUsoMovimientos, CasosDeUsoMovimientos>();
builder.Services.AddInfraestructura(builder.Configuration);

// Enums como texto ("Entrada", "Salida"...) y sin aceptar números.
builder.Services.ConfigureHttpJsonOptions(opciones =>
    opciones.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

// JSON mal formado o parámetros inválidos llegan al manejador de errores en lugar de dar un 400 vacío.
builder.Services.Configure<RouteHandlerOptions>(opciones => opciones.ThrowOnBadRequest = true);
builder.Services.AddProblemDetails();

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
    await app.Services.AplicarMigracionesAsync();

app.MapCategorias();
app.MapProductos();
app.MapMovimientos();

app.Run();
