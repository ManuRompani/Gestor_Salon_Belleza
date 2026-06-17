using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestor_Salon_Belleza.Models
{
    public class Turno
    {
        [Key]
        public int Id_Turno { get; set; }

        [Required(ErrorMessage = "Campo Requerido")]
        [ForeignKey("Cliente")]
        public int Id_Cliente { get; set; }

        [Required(ErrorMessage = "Campo Requerido")]
        [ForeignKey("Profesional")]
        public int Id_Profesional { get; set; }

        public bool Estado { get; set; } = false;

        [Required(ErrorMessage = "Campo Requerido")]
        [DataType(DataType.DateTime)]
        public DateTime FechaHora { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [DataType(DataType.DateTime)]
        public DateTime? FechaCancelacion { get; set; }

        public Cliente Cliente { get; set; }
        public Profesional Profesional { get; set; }

        public ICollection<Turno_Servicio> TurnoServicios { get; set; }

    }
}
