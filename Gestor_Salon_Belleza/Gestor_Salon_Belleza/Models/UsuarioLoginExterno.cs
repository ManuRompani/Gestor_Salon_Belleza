using System.ComponentModel.DataAnnotations;

namespace Gestor_Salon_Belleza.Models
{
    public class UsuarioLoginExterno
    {
        [Key]
        public int Id_LoginExterno { get; set; }

        [Required]
        public int Id_Usuario { get; set; }

        [Required]
        [StringLength(50)]
        public string Proveedor { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string ProviderKey { get; set; } = null!;

        //Se utiliza para saber cuando el usuario se asocio a google.
        //No tiene nada que ver con la otra fecha que marca si el usuario esta dado de baja
        [Required]
        public DateTime FechaAlta { get; set; } = DateTime.Now;

        public Usuario Usuario { get; set; } = null!;
    }
}
