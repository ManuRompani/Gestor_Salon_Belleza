using System.ComponentModel.DataAnnotations.Schema;

namespace Gestor_Salon_Belleza.Models
{
    public class Profesional_Servicio
    {
        [ForeignKey("Profesional")]
        public int Id_Profesional { get; set; }
        [ForeignKey("Servicio")]
        public int Id_Servicio { get; set; }

        public Profesional Profesional { get; set; }
        public Servicio Servicio { get; set; }

    }
}
