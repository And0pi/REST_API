using API_REST.Models;
using Microsoft.EntityFrameworkCore;

namespace API_REST.Data;

public class InventarioDbContext(DbContextOptions<InventarioDbContext> options) : DbContext(options)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoInventario> Movimientos => Set<MovimientoInventario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categoria>(e =>
        {
            e.Property(c => c.Nombre).HasMaxLength(100).IsRequired();
            e.Property(c => c.Descripcion).HasMaxLength(500);
            e.HasIndex(c => c.Nombre).IsUnique();
        });

        modelBuilder.Entity<Producto>(e =>
        {
            e.Property(p => p.Sku).HasMaxLength(50).IsRequired();
            e.Property(p => p.Nombre).HasMaxLength(150).IsRequired();
            e.Property(p => p.Descripcion).HasMaxLength(1000);
            e.Property(p => p.Precio).HasPrecision(12, 2);
            e.HasIndex(p => p.Sku).IsUnique();
            e.HasIndex(p => p.Activo);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("ck_productos_precio", "precio >= 0");
                t.HasCheckConstraint("ck_productos_stock", "stock >= 0");
            });

            // Al borrar una categoría, sus productos quedan sin categoría.
            e.HasOne(p => p.Categoria)
             .WithMany(c => c.Productos)
             .HasForeignKey(p => p.CategoriaId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MovimientoInventario>(e =>
        {
            e.ToTable("movimientos_inventario");
            e.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(m => m.Motivo).HasMaxLength(250);
            e.HasIndex(m => new { m.ProductoId, m.Fecha });
            e.HasIndex(m => m.Fecha);

            // Un producto con historial no se puede borrar físicamente (solo desactivar).
            e.HasOne(m => m.Producto)
             .WithMany(p => p.Movimientos)
             .HasForeignKey(m => m.ProductoId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
