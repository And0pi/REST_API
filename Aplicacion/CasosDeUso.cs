using API_REST.Dtos;
using API_REST.Models;

namespace API_REST.Aplicacion;

public sealed class CasosDeUsoCategorias(ICategoriasRepositorio repositorio) : ICasosDeUsoCategorias
{
    public Task<List<CategoriaDto>> ListarAsync(CancellationToken ct) => repositorio.ListarAsync(ct);

    public async Task<Resultado<CategoriaDto>> ObtenerAsync(int id, CancellationToken ct) =>
        await repositorio.BuscarCategoriaDtoAsync(id, ct) is { } dto
            ? Resultado<CategoriaDto>.Exito(dto)
            : Resultado<CategoriaDto>.Fallo(404, $"No existe la categoría {id}.");

    public async Task<Resultado<CategoriaDto>> CrearAsync(GuardarCategoriaDto dto, CancellationToken ct)
    {
        var nombre = dto.Nombre.Trim();
        if (await repositorio.NombreEnUsoAsync(nombre, null, ct))
            return Resultado<CategoriaDto>.Fallo(409, $"Ya existe una categoría llamada '{nombre}'.");

        var categoria = new Categoria { Nombre = nombre, Descripcion = dto.Descripcion?.Trim(), CreadoEn = DateTime.UtcNow };
        repositorio.Agregar(categoria);
        await repositorio.GuardarAsync(ct);
        return Resultado<CategoriaDto>.Exito((await repositorio.BuscarCategoriaDtoAsync(categoria.Id, ct))!);
    }

    public async Task<Resultado<CategoriaDto>> ActualizarAsync(int id, GuardarCategoriaDto dto, CancellationToken ct)
    {
        var categoria = await repositorio.BuscarCategoriaAsync(id, ct);
        if (categoria is null)
            return Resultado<CategoriaDto>.Fallo(404, $"No existe la categoría {id}.");

        var nombre = dto.Nombre.Trim();
        if (await repositorio.NombreEnUsoAsync(nombre, id, ct))
            return Resultado<CategoriaDto>.Fallo(409, $"Ya existe una categoría llamada '{nombre}'.");

        categoria.Nombre = nombre;
        categoria.Descripcion = dto.Descripcion?.Trim();
        await repositorio.GuardarAsync(ct);
        return Resultado<CategoriaDto>.Exito((await repositorio.BuscarCategoriaDtoAsync(id, ct))!);
    }

    public async Task<Resultado<bool>> EliminarAsync(int id, CancellationToken ct) =>
        await repositorio.EliminarAsync(id, ct)
            ? Resultado<bool>.Exito(true)
            : Resultado<bool>.Fallo(404, $"No existe la categoría {id}.");
}

public sealed class CasosDeUsoProductos(IProductosRepositorio repositorio) : ICasosDeUsoProductos
{
    public Task<PaginaResultado<ProductoDto>> ListarAsync(ConsultaProductos consulta, CancellationToken ct) =>
        repositorio.ListarAsync(consulta, ct);

    public async Task<Resultado<ProductoDto>> ObtenerAsync(int id, CancellationToken ct) =>
        await repositorio.BuscarProductoDtoAsync(id, ct) is { } dto
            ? Resultado<ProductoDto>.Exito(dto)
            : Resultado<ProductoDto>.Fallo(404, $"No existe el producto {id}.");

    public async Task<Resultado<ProductoDto>> CrearAsync(CrearProductoDto dto, CancellationToken ct)
    {
        var sku = dto.Sku.Trim();
        if (await repositorio.SkuEnUsoAsync(sku, ct))
            return Resultado<ProductoDto>.Fallo(409, $"Ya existe un producto con el SKU '{sku}'.");
        if (dto.CategoriaId is int categoriaId && !await repositorio.CategoriaExisteAsync(categoriaId, ct))
            return Resultado<ProductoDto>.Fallo(400, $"No existe la categoría {categoriaId}.",
                new Dictionary<string, string[]> { ["categoriaId"] = [$"No existe la categoría {categoriaId}."] });

        var ahora = DateTime.UtcNow;
        var producto = new Producto
        {
            Sku = sku,
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim(),
            Precio = dto.Precio!.Value,
            Stock = dto.StockInicial,
            CategoriaId = dto.CategoriaId,
            CreadoEn = ahora,
            ActualizadoEn = ahora
        };
        if (dto.StockInicial > 0)
        {
            producto.Movimientos.Add(new MovimientoInventario
            {
                Tipo = TipoMovimiento.Entrada,
                Cantidad = dto.StockInicial,
                StockAnterior = 0,
                StockResultante = dto.StockInicial,
                Motivo = "Stock inicial",
                Fecha = ahora
            });
        }

        repositorio.Agregar(producto);
        await repositorio.GuardarAsync(ct);
        return Resultado<ProductoDto>.Exito((await repositorio.BuscarProductoDtoAsync(producto.Id, ct))!);
    }

    public async Task<Resultado<ProductoDto>> ActualizarAsync(int id, ActualizarProductoDto dto, CancellationToken ct)
    {
        var producto = await repositorio.BuscarProductoAsync(id, ct);
        if (producto is null)
            return Resultado<ProductoDto>.Fallo(404, $"No existe el producto {id}.");
        if (dto.CategoriaId is int categoriaId && !await repositorio.CategoriaExisteAsync(categoriaId, ct))
            return Resultado<ProductoDto>.Fallo(400, $"No existe la categoría {categoriaId}.",
                new Dictionary<string, string[]> { ["categoriaId"] = [$"No existe la categoría {categoriaId}."] });

        producto.Nombre = dto.Nombre.Trim();
        producto.Descripcion = dto.Descripcion?.Trim();
        producto.Precio = dto.Precio!.Value;
        producto.CategoriaId = dto.CategoriaId;
        producto.ActualizadoEn = DateTime.UtcNow;
        await repositorio.GuardarAsync(ct);
        return Resultado<ProductoDto>.Exito((await repositorio.BuscarProductoDtoAsync(id, ct))!);
    }

    public async Task<Resultado<bool>> EliminarAsync(int id, bool permanente, CancellationToken ct)
    {
        var producto = await repositorio.BuscarProductoAsync(id, ct);
        if (producto is null)
            return Resultado<bool>.Fallo(404, $"No existe el producto {id}.");

        if (permanente)
        {
            if (await repositorio.TieneMovimientosAsync(id, ct))
                return Resultado<bool>.Fallo(409, "El producto tiene movimientos de inventario y no puede eliminarse; desactívalo en su lugar.");
            await repositorio.EliminarAsync(producto, ct);
        }
        else if (producto.Activo)
        {
            producto.Activo = false;
            producto.ActualizadoEn = DateTime.UtcNow;
            await repositorio.GuardarAsync(ct);
        }
        return Resultado<bool>.Exito(true);
    }

    public async Task<Resultado<ProductoDto>> ActivarAsync(int id, CancellationToken ct)
    {
        var producto = await repositorio.BuscarProductoAsync(id, ct);
        if (producto is null)
            return Resultado<ProductoDto>.Fallo(404, $"No existe el producto {id}.");
        if (!producto.Activo)
        {
            producto.Activo = true;
            producto.ActualizadoEn = DateTime.UtcNow;
            await repositorio.GuardarAsync(ct);
        }
        return Resultado<ProductoDto>.Exito((await repositorio.BuscarProductoDtoAsync(id, ct))!);
    }
}

public sealed class CasosDeUsoMovimientos(IMovimientosRepositorio repositorio) : ICasosDeUsoMovimientos
{
    public async Task<Resultado<MovimientoDto>> RegistrarAsync(int productoId, CrearMovimientoDto dto, CancellationToken ct)
    {
        await using var transaccion = await repositorio.IniciarTransaccionAsync(ct);
        var producto = await transaccion.BloquearProductoAsync(productoId, ct);
        if (producto is null)
            return Resultado<MovimientoDto>.Fallo(404, $"No existe el producto {productoId}.");
        if (!producto.Activo)
            return Resultado<MovimientoDto>.Fallo(409, "El producto está desactivado y no admite movimientos.");

        var tipo = dto.Tipo!.Value;
        var cantidad = dto.Cantidad!.Value;
        var anterior = producto.Stock;
        long resultante = tipo switch
        {
            TipoMovimiento.Entrada => (long)anterior + cantidad,
            TipoMovimiento.Salida => (long)anterior - cantidad,
            _ => cantidad
        };
        if (resultante < 0)
            return Resultado<MovimientoDto>.Fallo(409, $"Stock insuficiente: hay {anterior} unidades y se intentan retirar {cantidad}.");
        if (resultante > int.MaxValue)
            return Resultado<MovimientoDto>.Fallo(409, "El stock resultante supera el máximo permitido.");

        var ahora = DateTime.UtcNow;
        producto.Stock = (int)resultante;
        producto.ActualizadoEn = ahora;
        var movimiento = new MovimientoInventario
        {
            ProductoId = productoId,
            Tipo = tipo,
            Cantidad = cantidad,
            StockAnterior = anterior,
            StockResultante = producto.Stock,
            Motivo = string.IsNullOrWhiteSpace(dto.Motivo) ? null : dto.Motivo.Trim(),
            Fecha = ahora
        };
        transaccion.Agregar(movimiento);
        await transaccion.GuardarYConfirmarAsync(ct);

        return Resultado<MovimientoDto>.Exito(new MovimientoDto(movimiento.Id, producto.Id, producto.Sku,
            producto.Nombre, tipo, cantidad, anterior, producto.Stock, movimiento.Motivo, ahora));
    }

    public async Task<Resultado<PaginaResultado<MovimientoDto>>> ListarDeProductoAsync(
        int productoId, ConsultaMovimientos consulta, CancellationToken ct)
    {
        if (!await repositorio.ProductoExisteAsync(productoId, ct))
            return Resultado<PaginaResultado<MovimientoDto>>.Fallo(404, $"No existe el producto {productoId}.");
        return Resultado<PaginaResultado<MovimientoDto>>.Exito(
            await repositorio.ListarAsync(consulta with { ProductoId = productoId }, ct));
    }

    public Task<PaginaResultado<MovimientoDto>> ListarAsync(ConsultaMovimientos consulta, CancellationToken ct) =>
        repositorio.ListarAsync(consulta, ct);
}