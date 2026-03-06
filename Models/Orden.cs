using System.ComponentModel.DataAnnotations.Schema;

namespace PruebaTecnicaApi.Models;

[Table("ordenes", Schema = "public")]
public class Orden
{
    [Column("id")]
    public int Id { get; set; }

    [Column("cliente_id")]
    public int ClienteId { get; set; }

    [Column("cliente_nombre")]
    public string ClienteNombre { get; set; } = "";

    [Column("fecha_creacion")]
    public DateTime? FechaCreacion { get; set; }

    [Column("total")]
    public decimal? Total { get; set; }
}