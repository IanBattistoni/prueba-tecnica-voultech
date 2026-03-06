using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PruebaTecnicaApi.DTOs;

namespace PruebaTecnicaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdenesController : ControllerBase
{
    private readonly string _connectionString;

    public OrdenesController(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection");
    }

    // GET /ordenes
    [HttpGet]
    public async Task<IActionResult> GetOrdenes()
    {
        var ordenes = new List<object>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = "SELECT id, cliente_id, cliente_nombre, fecha_creacion, total FROM ordenes";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            ordenes.Add(new
            {
                Id = reader.GetInt32(0),
                ClienteId = reader.GetInt32(1),
                ClienteNombre = reader.GetString(2),
                FechaCreacion = reader.GetDateTime(3),
                Total = reader.GetDecimal(4)
            });
        }

        return Ok(ordenes);
    }

    // GET /ordenes/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrden(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = "SELECT id, cliente_id, cliente_nombre, fecha_creacion, total FROM ordenes WHERE id = @id";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return NotFound();

        var orden = new
        {
            Id = reader.GetInt32(0),
            ClienteId = reader.GetInt32(1),
            ClienteNombre = reader.GetString(2),
            FechaCreacion = reader.GetDateTime(3),
            Total = reader.GetDecimal(4)
        };

        return Ok(orden);
    }

    // POST /ordenes
    [HttpPost]
    public async Task<IActionResult> CrearOrden([FromBody] CrearOrdenRequest request)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        decimal total = 0;

        // calcula total
        foreach (var productoId in request.Productos)
        {
            var sql = "SELECT precio_producto FROM productos WHERE id = @id";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", productoId);

            var precio = (decimal?)await cmd.ExecuteScalarAsync();

            if (precio == null)
                return BadRequest($"Producto {productoId} no existe");

            total += precio.Value;
        }

        // crea orden
        var insertSql = @"INSERT INTO ordenes(cliente_id, cliente_nombre, total)
                        VALUES(@cliente_id, @cliente_nombre, @total)
                        RETURNING id";

        await using var insertCmd = new NpgsqlCommand(insertSql, conn);

        insertCmd.Parameters.AddWithValue("cliente_id", request.ClienteId);
        insertCmd.Parameters.AddWithValue("cliente_nombre", request.ClienteNombre);
        insertCmd.Parameters.AddWithValue("total", total);

        var ordenId = (int)await insertCmd.ExecuteScalarAsync();

        // guarda relación orden-productos
        foreach (var productoId in request.Productos)
        {
            var insertRelacionSql = @"
                INSERT INTO orden_productos (orden_id, producto_id)
                VALUES (@orden_id, @producto_id)";

            await using var insertRelacionCmd = new NpgsqlCommand(insertRelacionSql, conn);

            insertRelacionCmd.Parameters.AddWithValue("orden_id", ordenId);
            insertRelacionCmd.Parameters.AddWithValue("producto_id", productoId);

            await insertRelacionCmd.ExecuteNonQueryAsync();
        }

        return Ok(new { ordenId, total });
    }
    // DELETE /ordenes/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrden(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = "DELETE FROM ordenes WHERE id = @id";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        await cmd.ExecuteNonQueryAsync();

        return NoContent();
    }
}