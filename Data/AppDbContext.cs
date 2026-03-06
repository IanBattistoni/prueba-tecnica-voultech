using Microsoft.EntityFrameworkCore;
using PruebaTecnicaApi.Models;

namespace PruebaTecnicaApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Orden> Ordenes { get; set; }

    //public DbSet<Producto> Productos { get; set; }

    //public DbSet<OrdenProducto> OrdenProductos { get; set; }
}