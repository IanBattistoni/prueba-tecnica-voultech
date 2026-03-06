namespace PruebaTecnicaApi.Models;

public class OrdenProducto
{
    public int Id { get; set; }

    public int OrdenId { get; set; }

    public int ProductoId { get; set; }

    public int Cantidad { get; set; }
}