using API_REST.Aplicacion;
using API_REST.Data;
using API_REST.Infraestructura;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API_REST.InfraestructuraDatos;

public static class Dependencias
{
    public static IServiceCollection AddInfraestructura(this IServiceCollection services, IConfiguration configuration)
    {
        var cadenaConexion = configuration.GetConnectionString("Inventario")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'ConnectionStrings:Inventario'.");
        services.AddDbContext<InventarioDbContext>(opciones =>
            opciones.UseNpgsql(cadenaConexion).UseSnakeCaseNamingConvention());
        services.AddExceptionHandler<ManejadorErrores>();
        services.AddScoped<RepositorioInventario>();
        services.AddScoped<ICategoriasRepositorio>(sp => sp.GetRequiredService<RepositorioInventario>());
        services.AddScoped<IProductosRepositorio>(sp => sp.GetRequiredService<RepositorioInventario>());
        services.AddScoped<IMovimientosRepositorio>(sp => sp.GetRequiredService<RepositorioInventario>());
        return services;
    }

    public static async Task AplicarMigracionesAsync(this IServiceProvider servicios, CancellationToken ct = default)
    {
        await using var scope = servicios.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<InventarioDbContext>().Database.MigrateAsync(ct);
    }
}