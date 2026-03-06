using System.ComponentModel.DataAnnotations;

namespace PruebaTecnicaApi.DTOs;

public class CrearProductoRequest
{
    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre del producto no puede tener más de 50 caracteres.")]
    [RegularExpression(@"^[A-Za-zÁÉÍÓÚáéíóúÑñ\s]+$", ErrorMessage = "El nombre del producto solo puede contener letras y espacios.")]
    public string NombreProducto { get; set; } = "";
    
    [Required(ErrorMessage = "El precio del producto es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor que 0.")]
    public decimal PrecioProducto { get; set; }
}