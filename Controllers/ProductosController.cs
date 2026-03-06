using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PruebaTecnicaApi.DTOs;

namespace PruebaTecnicaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly string _connectionString;

    public ProductosController(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection") ?? "";
    }

    // GET /api/productos
    [HttpGet]
    public async Task<IActionResult> GetProductos()
    {
        var productos = new List<object>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = @"SELECT id, nombre_producto, precio_producto
                    FROM public.productos
                    ORDER BY id";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            productos.Add(new
            {
                Id = reader.GetInt32(0),
                NombreProducto = reader.GetString(1),
                PrecioProducto = reader.GetDecimal(2)
            });
        }

        return Ok(productos);
    }

    // POST /api/productos
    [HttpPost]
    public async Task<IActionResult> CrearProducto([FromBody] CrearProductoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NombreProducto))
            return BadRequest(new { message = "El nombre del producto es obligatorio." });

        if (request.PrecioProducto <= 0)
            return BadRequest(new { message = "El precio del producto debe ser mayor a 0." });

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = @"INSERT INTO public.productos(nombre_producto, precio_producto)
                    VALUES(@nombre_producto, @precio_producto)
                    RETURNING id";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("nombre_producto", request.NombreProducto);
        cmd.Parameters.AddWithValue("precio_producto", request.PrecioProducto);

        var id = await cmd.ExecuteScalarAsync();

        return Created($"/api/productos/{id}", new
        {
            Id = id,
            request.NombreProducto,
            request.PrecioProducto
        });
    }
}