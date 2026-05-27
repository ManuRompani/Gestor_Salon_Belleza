using System.ComponentModel.DataAnnotations;

namespace Gestor_Salon_Belleza.Models
{
    public class Servicio
    {

        [Key]
        public int Id_Servicio { get; set; }


        [Required(ErrorMessage = "Campo Requerido")]
        public string Nombre { get; set; }


        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "Campo Requerido")]
        [Range(1, 480, ErrorMessage = "Entre 1 y 480 minutos")]
        public decimal Duracion_Minutos { get; set; }

        [Required(ErrorMessage = "Campo Requerido")]
        [Range(0.01, 999999, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        public ICollection<Profesional_Servicio> Profesional_Servicios { get; set; }
        public ICollection<Turno> Turnos { get; set; }

    }
}
