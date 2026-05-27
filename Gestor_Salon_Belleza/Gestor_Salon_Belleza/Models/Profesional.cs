using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestor_Salon_Belleza.Models
{
    public class Profesional
    {

        [Key]
        [ForeignKey("Usuario")]
        public int Id_Usuario { get; set; }

        public string? UrlImagen { get; set; }

        public string? Descripcion { get; set; }
        
        public Usuario Usuario { get; set; }
        
        public ICollection<Profesional_Servicio> Profesional_Servicios { get; set; }

        public ICollection<Turno> Turnos { get; set; }


    }
}
