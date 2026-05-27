using System.ComponentModel.DataAnnotations.Schema;

namespace Gestor_Salon_Belleza.Models
{
    public class Turno_Servicio
    {
        [ForeignKey("Turno")]
        public int Id_Turno { get; set; }

        [ForeignKey("Servicio")]
        public int Id_Servicio { get; set; }

        public Turno Turno { get; set; }
        public Servicio Servicio { get; set; }
    }
}
