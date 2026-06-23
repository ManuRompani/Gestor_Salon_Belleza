using System.ComponentModel.DataAnnotations;

namespace Gestor_Salon_Belleza.ViewModels
{
    public class ServicesViewModel
    {
        public int Id { get; set; } 

        [Required(ErrorMessage = "El nombre del servicio es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        [Range(1, 1440, ErrorMessage = "La duración debe ser entre 1 y 1440 minutos (24 hs).")]
        public int Duracion { get; set; }

        [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser un valor positivo mayor a 0.")]
        public decimal Precio { get; set; }

        public bool Activo { get; set; }
    }
}
