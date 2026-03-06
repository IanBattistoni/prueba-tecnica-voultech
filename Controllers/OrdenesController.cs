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

        var sql = @"SELECT id, cliente_id, cliente_nombre, fecha_creacion, total
                    FROM public.ordenes
                    WHERE id = @id";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return NotFound(new
            {
                message = $"La orden {id} no existe."
            });
        }

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

        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            // valida cliente
            var existeClienteSql = "SELECT COUNT(*) FROM public.clientes WHERE id = @cliente_id";
            await using (var existeClienteCmd = new NpgsqlCommand(existeClienteSql, conn, transaction))
            {
                existeClienteCmd.Parameters.AddWithValue("cliente_id", request.ClienteId);
                var existeCliente = (long)await existeClienteCmd.ExecuteScalarAsync();

                if (existeCliente == 0)
                    return BadRequest(new { message = $"El cliente {request.ClienteId} no existe." });
            }

            // valida calculo total
            decimal total = 0;

            foreach (var productoId in request.Productos)
            {
                var precioSql = "SELECT precio_producto FROM public.productos WHERE id = @id";

                await using var precioCmd = new NpgsqlCommand(precioSql, conn, transaction);
                precioCmd.Parameters.AddWithValue("id", productoId);

                var precio = (decimal?)await precioCmd.ExecuteScalarAsync();

                if (precio == null)
                    return BadRequest(new { message = $"Producto {productoId} no existe." });

                total += precio.Value;
            }

            // crea orden
            var insertSql = @"
                INSERT INTO public.ordenes (cliente_id, cliente_nombre, total)
                VALUES (@cliente_id, @cliente_nombre, @total)
                RETURNING id";

            int ordenId;

            await using (var insertCmd = new NpgsqlCommand(insertSql, conn, transaction))
            {
                insertCmd.Parameters.AddWithValue("cliente_id", request.ClienteId);
                insertCmd.Parameters.AddWithValue("cliente_nombre", request.ClienteNombre);
                insertCmd.Parameters.AddWithValue("total", total);

                ordenId = (int)await insertCmd.ExecuteScalarAsync();
            }

            // guarda relacion orden-productos
            foreach (var productoId in request.Productos)
            {
                var insertRelacionSql = @"
                    INSERT INTO public.orden_productos (orden_id, producto_id)
                    VALUES (@orden_id, @producto_id)";

                await using var insertRelacionCmd = new NpgsqlCommand(insertRelacionSql, conn, transaction);
                insertRelacionCmd.Parameters.AddWithValue("orden_id", ordenId);
                insertRelacionCmd.Parameters.AddWithValue("producto_id", productoId);

                await insertRelacionCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();

            return Ok(new
            {
                ordenId,
                total
            });
        }
        catch (PostgresException ex)
        {
            await transaction.RollbackAsync();

            return BadRequest(new
            {
                message = "Error de base de datos.",
                detail = ex.MessageText
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            return StatusCode(500, new
            {
                message = "Error interno del servidor.",
                detail = ex.Message
            });
        }
    }

    // DELETE /ordenes/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrden(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            // verifica que exita la orden
            var existeSql = "SELECT COUNT(*) FROM public.ordenes WHERE id = @id";
            await using (var existeCmd = new NpgsqlCommand(existeSql, conn, transaction))
            {
                existeCmd.Parameters.AddWithValue("id", id);
                var existe = (long)await existeCmd.ExecuteScalarAsync();

                if (existe == 0)
                    return NotFound(new { message = $"La orden {id} no existe." });
            }

            // borrar relaciones orden-productos
            var deleteRelacionesSql = "DELETE FROM public.orden_productos WHERE orden_id = @id";
            await using (var deleteRelacionesCmd = new NpgsqlCommand(deleteRelacionesSql, conn, transaction))
            {
                deleteRelacionesCmd.Parameters.AddWithValue("id", id);
                await deleteRelacionesCmd.ExecuteNonQueryAsync();
            }

            // borra orden
            var deleteOrdenSql = "DELETE FROM public.ordenes WHERE id = @id";
            await using (var deleteOrdenCmd = new NpgsqlCommand(deleteOrdenSql, conn, transaction))
            {
                deleteOrdenCmd.Parameters.AddWithValue("id", id);
                await deleteOrdenCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();

            return Ok(new
            {
                message = $"Orden {id} eliminada correctamente."
            });
        }
        catch (PostgresException ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new
            {
                message = "Error de base de datos.",
                detail = ex.MessageText
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new
            {
                message = "Error interno del servidor.",
                detail = ex.Message
            });
        }
    }
    //PUT /ordenes/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> EditarOrden(int id, [FromBody] CrearOrdenRequest request)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            // verifica que exista la orden
            var existeOrdenSql = "SELECT COUNT(*) FROM public.ordenes WHERE id = @id";
            await using (var existeOrdenCmd = new NpgsqlCommand(existeOrdenSql, conn, transaction))
            {
                existeOrdenCmd.Parameters.AddWithValue("id", id);
                var existeOrden = (long)await existeOrdenCmd.ExecuteScalarAsync();

                if (existeOrden == 0)
                    return NotFound(new { message = $"La orden {id} no existe." });
            }

            // verifica que exista el cliente
            var existeClienteSql = "SELECT COUNT(*) FROM public.clientes WHERE id = @cliente_id";
            await using (var existeClienteCmd = new NpgsqlCommand(existeClienteSql, conn, transaction))
            {
                existeClienteCmd.Parameters.AddWithValue("cliente_id", request.ClienteId);
                var existeCliente = (long)await existeClienteCmd.ExecuteScalarAsync();

                if (existeCliente == 0)
                    return BadRequest(new { message = $"El cliente {request.ClienteId} no existe." });
            }

            // recalcula total
            decimal total = 0;

            foreach (var productoId in request.Productos)
            {
                var precioSql = "SELECT precio_producto FROM public.productos WHERE id = @id";

                await using var precioCmd = new NpgsqlCommand(precioSql, conn, transaction);
                precioCmd.Parameters.AddWithValue("id", productoId);

                var precio = (decimal?)await precioCmd.ExecuteScalarAsync();

                if (precio == null)
                    return BadRequest(new { message = $"Producto {productoId} no existe." });

                total += precio.Value;
            }

            // actualiza orden
            var updateOrdenSql = @"
                UPDATE public.ordenes
                SET cliente_id = @cliente_id,
                    cliente_nombre = @cliente_nombre,
                    total = @total
                WHERE id = @id";

            await using (var updateOrdenCmd = new NpgsqlCommand(updateOrdenSql, conn, transaction))
            {
                updateOrdenCmd.Parameters.AddWithValue("id", id);
                updateOrdenCmd.Parameters.AddWithValue("cliente_id", request.ClienteId);
                updateOrdenCmd.Parameters.AddWithValue("cliente_nombre", request.ClienteNombre);
                updateOrdenCmd.Parameters.AddWithValue("total", total);

                await updateOrdenCmd.ExecuteNonQueryAsync();
            }

            // borrar relaciones anteriores
            var deleteRelacionesSql = "DELETE FROM public.orden_productos WHERE orden_id = @orden_id";
            await using (var deleteRelacionesCmd = new NpgsqlCommand(deleteRelacionesSql, conn, transaction))
            {
                deleteRelacionesCmd.Parameters.AddWithValue("orden_id", id);
                await deleteRelacionesCmd.ExecuteNonQueryAsync();
            }

            // insertar nuevas relaciones orden-productos
            foreach (var productoId in request.Productos)
            {
                var insertRelacionSql = @"
                    INSERT INTO public.orden_productos (orden_id, producto_id)
                    VALUES (@orden_id, @producto_id)";

                await using var insertRelacionCmd = new NpgsqlCommand(insertRelacionSql, conn, transaction);
                insertRelacionCmd.Parameters.AddWithValue("orden_id", id);
                insertRelacionCmd.Parameters.AddWithValue("producto_id", productoId);

                await insertRelacionCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();

            return Ok(new
            {
                ordenId = id,
                clienteId = request.ClienteId,
                clienteNombre = request.ClienteNombre,
                total
            });
        }
        catch (PostgresException ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new
            {
                message = "Error de base de datos.",
                detail = ex.MessageText
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new
            {
                message = "Error interno del servidor.",
                detail = ex.Message
            });
        }
    }

    // GET /ordenes/cliente/{clienteId}
    [HttpGet("cliente/{clienteId:int}")]
    public async Task<IActionResult> GetOrdenesPorCliente(int clienteId)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // verifica que exista el usuario
            var existeClienteSql = "SELECT COUNT(*) FROM public.clientes WHERE id = @cliente_id";
            await using (var existeClienteCmd = new NpgsqlCommand(existeClienteSql, conn))
            {
                existeClienteCmd.Parameters.AddWithValue("cliente_id", clienteId);
                var existeCliente = (long)await existeClienteCmd.ExecuteScalarAsync();

                if (existeCliente == 0)
                    return NotFound(new { message = $"El cliente {clienteId} no existe." });
            }

            var ordenes = new List<object>();

            var sql = @"
                SELECT id, cliente_id, cliente_nombre, fecha_creacion, total
                FROM public.ordenes
                WHERE cliente_id = @cliente_id
                ORDER BY fecha_creacion DESC, id DESC";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("cliente_id", clienteId);

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
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Error interno del servidor.",
                detail = ex.Message
            });
        }
    }
}