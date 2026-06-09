using System.ComponentModel.DataAnnotations;

namespace Gestor_Salon_Belleza.ViewModels
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Campo Requerido")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Entre 2 y 50 caracteres")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Campo Requerido")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Entre 2 y 50 caracteres")]
        public string Apellido { get; set; }

        [Required(ErrorMessage = "Campo Requerido")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Teléfono inválido")]
        public string? Telefono { get; set; }
    }
}
