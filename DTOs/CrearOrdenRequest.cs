using System.ComponentModel.DataAnnotations;

namespace PruebaTecnicaApi.DTOs;

public class CrearOrdenRequest
{
    public int ClienteId { get; set; }

    [Required(ErrorMessage = "clienteNombre es obligatorio.")]
    [StringLength(20, ErrorMessage = "clienteNombre no puede tener más de 20 caracteres.")]
    [RegularExpression(@"^[A-Za-zÁÉÍÓÚáéíóúÑñ\s]+$", ErrorMessage = "clienteNombre solo puede contener letras y espacios.")]
    public string ClienteNombre { get; set; } = "";

    [Required(ErrorMessage = "productos es obligatorio.")]
    [MinLength(1, ErrorMessage = "Debe enviar al menos un producto.")]
    public List<int> Productos { get; set; } = new();
}