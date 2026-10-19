using API_REST.Aplicacion;
using API_REST.Dtos;
using API_REST.Models;
using Xunit;

namespace Aplicacion.Tests;

public sealed class CasosDeUsoMovimientosTests
{
    [Theory]
    [InlineData(TipoMovimiento.Entrada, 10, 5, 15)]
    [InlineData(TipoMovimiento.Salida, 10, 4, 6)]
    [InlineData(TipoMovimiento.Ajuste, 10, 3, 3)]
    public async Task Registrar_actualiza_stock_y_guarda_historial(
        TipoMovimiento tipo, int stockInicial, int cantidad, int stockEsperado)
    {
        var repositorio = new RepositorioMovimientosFalso(new Producto
        {
            Id = 7,
            Sku = "SKU-7",
            Nombre = "Producto de prueba",
            Stock = stockInicial
        });
        var casos = new CasosDeUsoMovimientos(repositorio);

        var resultado = await casos.RegistrarAsync(7, new CrearMovimientoDto
        {
            Tipo = tipo,
            Cantidad = cantidad,
            Motivo = "  conteo  "
        }, CancellationToken.None);

        Assert.Null(resultado.Error);
        Assert.Equal(stockEsperado, repositorio.Producto!.Stock);
        var movimiento = Assert.Single(repositorio.Transaccion.Movimientos);
        Assert.Equal(stockInicial, movimiento.StockAnterior);
        Assert.Equal(stockEsperado, movimiento.StockResultante);
        Assert.Equal("conteo", movimiento.Motivo);
        Assert.True(repositorio.Transaccion.Confirmada);
    }

    [Fact]
    public async Task Registrar_rechaza_salida_mayor_al_stock_sin_mutar_producto()
    {
        var repositorio = new RepositorioMovimientosFalso(new Producto { Id = 7, Stock = 3 });
        var casos = new CasosDeUsoMovimientos(repositorio);

        var resultado = await casos.RegistrarAsync(7, new CrearMovimientoDto
        {
            Tipo = TipoMovimiento.Salida,
            Cantidad = 4
        }, CancellationToken.None);

        Assert.Equal(409, resultado.Error!.Estado);
        Assert.Equal(3, repositorio.Producto!.Stock);
        Assert.Empty(repositorio.Transaccion.Movimientos);
        Assert.False(repositorio.Transaccion.Confirmada);
    }

    [Fact]
    public async Task Registrar_rechaza_producto_inactivo()
    {
        var repositorio = new RepositorioMovimientosFalso(new Producto { Id = 7, Stock = 3, Activo = false });
        var casos = new CasosDeUsoMovimientos(repositorio);

        var resultado = await casos.RegistrarAsync(7, new CrearMovimientoDto
        {
            Tipo = TipoMovimiento.Entrada,
            Cantidad = 2
        }, CancellationToken.None);

        Assert.Equal(409, resultado.Error!.Estado);
        Assert.Equal(3, repositorio.Producto!.Stock);
        Assert.Empty(repositorio.Transaccion.Movimientos);
        Assert.False(repositorio.Transaccion.Confirmada);
    }

    [Fact]
    public async Task Registrar_rechaza_stock_que_supera_el_limite_de_entero()
    {
        var repositorio = new RepositorioMovimientosFalso(new Producto { Id = 7, Stock = int.MaxValue });
        var casos = new CasosDeUsoMovimientos(repositorio);

        var resultado = await casos.RegistrarAsync(7, new CrearMovimientoDto
        {
            Tipo = TipoMovimiento.Entrada,
            Cantidad = 1
        }, CancellationToken.None);

        Assert.Equal(409, resultado.Error!.Estado);
        Assert.Equal(int.MaxValue, repositorio.Producto!.Stock);
        Assert.Empty(repositorio.Transaccion.Movimientos);
        Assert.False(repositorio.Transaccion.Confirmada);
    }

    [Fact]
    public async Task Registrar_devuelve_404_si_el_producto_no_existe()
    {
        var repositorio = new RepositorioMovimientosFalso(null);
        var casos = new CasosDeUsoMovimientos(repositorio);

        var resultado = await casos.RegistrarAsync(99, new CrearMovimientoDto
        {
            Tipo = TipoMovimiento.Entrada,
            Cantidad = 1
        }, CancellationToken.None);

        Assert.Equal(404, resultado.Error!.Estado);
        Assert.Empty(repositorio.Transaccion.Movimientos);
        Assert.False(repositorio.Transaccion.Confirmada);
    }

    [Fact]
    public async Task Listar_de_producto_fuerza_el_filtro_del_producto_de_la_ruta()
    {
        var repositorio = new RepositorioMovimientosFalso(new Producto { Id = 7 });
        var casos = new CasosDeUsoMovimientos(repositorio);

        var resultado = await casos.ListarDeProductoAsync(7, new ConsultaMovimientos(ProductoId: 99), CancellationToken.None);

        Assert.Null(resultado.Error);
        Assert.Equal(7, repositorio.UltimaConsulta!.ProductoId);
    }

    private sealed class RepositorioMovimientosFalso(Producto? producto) : IMovimientosRepositorio
    {
        public Producto? Producto { get; } = producto;
        public TransaccionFalsa Transaccion { get; } = new(producto);
        public ConsultaMovimientos? UltimaConsulta { get; private set; }

        public Task<ITransaccionMovimiento> IniciarTransaccionAsync(CancellationToken ct) =>
            Task.FromResult<ITransaccionMovimiento>(Transaccion);

        public Task<bool> ProductoExisteAsync(int productoId, CancellationToken ct) =>
            Task.FromResult(Producto?.Id == productoId);

        public Task<PaginaResultado<MovimientoDto>> ListarAsync(ConsultaMovimientos consulta, CancellationToken ct)
        {
            UltimaConsulta = consulta;
            return Task.FromResult(new PaginaResultado<MovimientoDto>([], consulta.Pagina, consulta.TamanoPagina, 0, 0));
        }
    }

    private sealed class TransaccionFalsa(Producto? producto) : ITransaccionMovimiento
    {
        public List<MovimientoInventario> Movimientos { get; } = [];
        public bool Confirmada { get; private set; }

        public Task<Producto?> BloquearProductoAsync(int productoId, CancellationToken ct) =>
            Task.FromResult(producto?.Id == productoId ? producto : null);

        public void Agregar(MovimientoInventario movimiento)
        {
            movimiento.Id = Movimientos.Count + 1;
            Movimientos.Add(movimiento);
        }

        public Task GuardarYConfirmarAsync(CancellationToken ct)
        {
            Confirmada = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}