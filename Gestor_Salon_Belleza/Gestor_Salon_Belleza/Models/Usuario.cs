using System.ComponentModel.DataAnnotations;

namespace Gestor_Salon_Belleza.Models
{
    public class Usuario
    {
        [Key]
        public int Id_Usuario { get; set; }

        [Required(ErrorMessage = "Campo Requerido")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Entre 2 y 50 caracteres")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Campo Requerido")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Entre 2 y 50 caracteres")]
        public string Apellido { get; set; }
        [Required(ErrorMessage = "Campo Requerido")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; }
                
        [Required(ErrorMessage = "Campo Requerido")]
        [Phone(ErrorMessage = "Teléfono inválido")]
        public string Telefono { get; set; }
        [Required(ErrorMessage = "Campo Requerido")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mínimo 6 caracteres")]
        public string Password { get; set; }
        

        public bool Eliminado { get; set; }

        [Required]
        public int Id_Rol { get; set; }

        public Rol Rol { get; set; }
    }
}
