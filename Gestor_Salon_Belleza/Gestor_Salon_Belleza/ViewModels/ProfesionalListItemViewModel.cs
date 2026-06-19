namespace Gestor_Salon_Belleza.ViewModels
{
    public class ProfesionalListItemViewModel
    {
        public int Id_Usuario { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Telefono { get; set; }

        public string? Descripcion { get; set; }

        public string Servicios { get; set; } = string.Empty;

        public bool Eliminado { get; set; }
    }
}
