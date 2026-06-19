using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Gestor_Salon_Belleza.ViewModels
{
    public class ProfesionalViewModel
    {
        public int? Id_Usuario { get; set; }

        [Required(ErrorMessage = "Campo Requerido")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Entre 2 y 50 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Campo Requerido")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Entre 2 y 50 caracteres")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "Campo Requerido")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Teléfono inválido")]
        public string? Telefono { get; set; }

        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden")]
        public string? ConfirmarPassword { get; set; }

        public string? UrlImagen { get; set; }

        // Recibe el archivo real desde el formulario.
        [Display(Name = "Imagen")]
        public IFormFile? ImagenArchivo { get; set; }

        public string? Descripcion { get; set; }

        public List<int> IdsServiciosSeleccionados { get; set; } = new();

        public List<SelectListItem> ServiciosDisponibles { get; set; } = new();
    }
}
