namespace Gestor_Salon_Belleza.ViewModels
{
    public class ProfesionalTurnoViewModel
    {
        public int Id_Turno { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string ServicioNombre { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaCancelacion { get; set; }
        public int DuracionMinutos { get; set; }
        public decimal Precio { get; set; }
    }
}
