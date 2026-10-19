# API de Inventario

API REST para que un pequeño negocio registre productos, los organice en categorías, controle existencias y consulte el historial de movimientos de inventario. Hecha con ASP.NET Core 9 (Minimal APIs), Entity Framework Core y PostgreSQL.

## Requisitos

- .NET SDK 9
- PostgreSQL 13 o superior

## Puesta en marcha

1. Configura la conexión en `appsettings.json` (`ConnectionStrings:Inventario`). Por defecto:
   `Host=localhost;Port=5432;Database=inventario;Username=postgres;Password=postgres`

   Para no guardar la contraseña en el archivo puedes usar *user secrets*:
   ```
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:Inventario" "Host=localhost;Database=inventario;Username=postgres;Password=TU_CLAVE"
   ```
   o la variable de entorno `ConnectionStrings__Inventario`.

2. Ejecuta:
   ```
   dotnet run
   ```
   En desarrollo, la base de datos y las tablas se crean solas al arrancar (`BaseDatos:AplicarMigracionesAlIniciar`).
   En producción aplica las migraciones a mano: `dotnet tool restore` y `dotnet ef database update`.

3. Abre http://localhost:5259 para ver la documentación interactiva (Scalar). Solo está disponible en desarrollo.

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/categorias` | Lista categorías (con número de productos activos) |
| GET | `/api/categorias/{id}` | Consulta una categoría |
| POST | `/api/categorias` | Crea una categoría |
| PUT | `/api/categorias/{id}` | Actualiza una categoría |
| DELETE | `/api/categorias/{id}` | Elimina una categoría; sus productos quedan sin categoría |
| GET | `/api/productos` | Lista paginada. Filtros: `pagina`, `tamanoPagina` (máx. 100), `buscar`, `categoriaId`, `stockMaximo`, `incluirInactivos` |
| GET | `/api/productos/{id}` | Consulta un producto |
| POST | `/api/productos` | Registra un producto (`stockInicial` opcional) |
| PUT | `/api/productos/{id}` | Actualiza nombre, precio, descripción y categoría |
| DELETE | `/api/productos/{id}` | Desactiva el producto. Con `?permanente=true` lo borra, solo si no tiene movimientos |
| POST | `/api/productos/{id}/activar` | Reactiva un producto |
| POST | `/api/productos/{id}/movimientos` | Registra `Entrada`, `Salida` o `Ajuste` |
| GET | `/api/productos/{id}/movimientos` | Historial de un producto |
| GET | `/api/movimientos` | Movimientos paginados. Filtros: `productoId`, `tipo`, `desde`, `hasta` |

Hay ejemplos listos para lanzar en [API_REST.http](API_REST.http).

## Reglas de negocio

- **El stock solo cambia mediante movimientos.** `PUT` no lo modifica, así cada unidad queda registrada en el historial.
  - `Entrada` suma `cantidad`.
  - `Salida` resta `cantidad`; se rechaza (409) si no hay stock suficiente.
  - `Ajuste` fija el stock al valor contado (`cantidad` = unidades reales).
- **Los movimientos simultáneos sobre un mismo producto se serializan** (bloqueo de fila), por lo que el stock nunca queda negativo ni se pierden actualizaciones.
- **Desactivar en lugar de borrar.** Un producto desactivado conserva su historial, no aparece en los listados por defecto y no admite movimientos.
- **Únicos:** el SKU de producto y el nombre de categoría (este último sin distinguir mayúsculas).

## Errores

Todas las respuestas de error usan el formato estándar *ProblemDetails* (RFC 9457) con mensajes en español:

| Código | Cuándo |
|---|---|
| 400 | Datos inválidos (con el detalle por campo en `errors`), JSON mal formado o parámetros incorrectos |
| 404 | El recurso no existe |
| 409 | SKU o nombre duplicado, stock insuficiente, producto desactivado |
| 503 | La base de datos no está disponible |
| 500 | Error inesperado (el detalle solo queda en el log del servidor) |

```json
{
  "title": "Los datos enviados no son válidos.",
  "status": 400,
  "errors": {
    "precio": ["El precio debe estar entre 0 y 9999999999.99."],
    "nombre": ["El nombre es obligatorio."]
  }
}
```

## Estructura

```
Program.cs                 Configuración: base de datos, JSON, errores, CORS, OpenAPI
Models/                    Entidades: Categoria, Producto, MovimientoInventario
Data/                      DbContext y migraciones de EF Core
Dtos/                      Contratos de entrada/salida con sus validaciones
Endpoints/                 Rutas de categorías, productos y movimientos
Infraestructura/           Filtro de validación y manejador global de errores
```

## CORS

Para consumir la API desde una web en otro dominio, añade los orígenes en `Cors:OrigenesPermitidos`, por ejemplo `["https://mitienda.com"]`. Si la lista está vacía se aceptan todos los orígenes, lo que es cómodo en desarrollo pero conviene restringir en producción.
