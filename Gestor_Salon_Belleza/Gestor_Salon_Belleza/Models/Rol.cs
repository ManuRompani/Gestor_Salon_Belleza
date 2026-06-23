using System.ComponentModel.DataAnnotations;

namespace Gestor_Salon_Belleza.Models
{
    public class Rol
    {
        [Key]
        public int Id_Rol { get; set; }

        [Required(ErrorMessage = "Campo Requerido")]
        public string Rol_Name { get; set; }

        public ICollection<Usuario> Usuarios { get; set; }
    }
}
