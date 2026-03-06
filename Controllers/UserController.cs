using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Text.RegularExpressions;
namespace PruebaTecnicaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly string _connectionString;

    public UsersController(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection") ?? "";
    }

    // GET /api/users
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var clientes = new List<object>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = @"SELECT id, nombre
                    FROM public.clientes
                    ORDER BY id";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            clientes.Add(new
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }

        return Ok(clientes);
    }

    // GET /api/users/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = @"SELECT id, nombre
                    FROM public.clientes
                    WHERE id = @id";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return NotFound(new { message = $"Cliente {id} no existe" });

        var cliente = new
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1)
        };

        return Ok(cliente);
    }

    // POST /api/users
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest body)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(new { message = "El nombre es obligatorio" });

        if (body.Name.Length > 20)
            return BadRequest(new { message = "El nombre no puede tener más de 20 caracteres" });

        if (!Regex.IsMatch(body.Name, @"^[A-Za-zÁÉÍÓÚáéíóúÑñ\s]+$"))
            return BadRequest(new { message = "El nombre solo puede contener letras y espacios" });

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // verifica si ya existe
        var existeSql = @"SELECT id FROM public.clientes WHERE nombre = @nombre";

        await using var existeCmd = new NpgsqlCommand(existeSql, conn);
        existeCmd.Parameters.AddWithValue("nombre", body.Name);

        var existente = await existeCmd.ExecuteScalarAsync();

        if (existente != null)
        {
            return Ok(new
            {
                Id = existente,
                Name = body.Name,
                message = "Usuario ya existente, usando id existente"
            });
        }

        // crea usuario si no existe
        var insertSql = @"INSERT INTO public.clientes(nombre)
                        VALUES(@nombre)
                        RETURNING id";

        await using var insertCmd = new NpgsqlCommand(insertSql, conn);
        insertCmd.Parameters.AddWithValue("nombre", body.Name);

        var id = await insertCmd.ExecuteScalarAsync();

        return Created($"/api/users/{id}", new
        {
            Id = id,
            Name = body.Name
        });
    }
}

public record CreateUserRequest(string Name);