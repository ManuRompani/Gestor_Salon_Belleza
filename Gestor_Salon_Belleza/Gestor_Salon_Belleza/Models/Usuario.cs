using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mínimo 8 caracteres")]
        public string Password { get; set; }


        public bool Eliminado { get; set; } = false;

        [Required]
        [ForeignKey(nameof(Rol))]
        public int Id_Rol { get; set; }

        
        public Rol Rol { get; set; }

        // Relacion con el alta de google
        // La hago de muchos ya que podria tener con otro medio, ej, LinkedIn...(ponele)
        public ICollection<UsuarioLoginExterno> LoginsExternos { get; set; } = new List<UsuarioLoginExterno>();
    }
}
