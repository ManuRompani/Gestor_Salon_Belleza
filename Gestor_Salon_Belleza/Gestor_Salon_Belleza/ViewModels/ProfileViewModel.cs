using System.ComponentModel.DataAnnotations;

namespace Gestor_Salon_Belleza.ViewModels
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Campo requerido")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Entre 2 y 50 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Campo requerido")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Entre 2 y 50 caracteres")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "Campo requerido")]
        [EmailAddress(ErrorMessage = "Email invalido")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Telefono invalido")]
        public string? Telefono { get; set; }

        // Estos campos son opcionales:
        // solo se completan cuando el cliente quiere reemplazar su contrasena actual.
        [DataType(DataType.Password)]
        public string? NuevaPassword { get; set; }

        [DataType(DataType.Password)]
        public string? ConfirmarNuevaPassword { get; set; }
    }
}
