namespace PruebaTecnicaApi.DTOs;

public class CrearOrdenRequest
{
    public int ClienteId { get; set; }

    public string ClienteNombre { get; set; } = "";

    public List<int> Productos { get; set; } = new();
}