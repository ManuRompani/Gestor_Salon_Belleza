using System.ComponentModel.DataAnnotations.Schema;

namespace Gestor_Salon_Belleza.Models
{
    public class Cliente
    {

        [ForeignKey("Usuario")]
        public int Id_Usuario { get; set; } 
        public string? Notas { get; set; }

        public Usuario Usuario { get; set; }
        public ICollection<Turno> Turnos { get; set; }
    }
}
