using System.ComponentModel.DataAnnotations;

namespace Gestor_Salon_Belleza.ViewModels
{
    public class BookAppointmentViewModel
    {
        public int ServiceId { get; set; }
        public string? ServiceName { get; set; }
        public int? ProfessionalId { get; set; }
        public string? ProfessionalName { get; set; }
        public DateTime? PreferredDate { get; set; }
        public string? PreferredTime { get; set; }
        public int DurationMinutes { get; set; }
        public decimal Precio { get; set; }
        public List<ProfessionalItem>? Professionals { get; set; }
        public List<TimeSlotItem>? AvailableSlots { get; set; }
    }

    public class ProfessionalItem
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
    }

    public class TimeSlotItem
    {
        public DateTime DateTime { get; set; }
        public string Display { get; set; }
        public bool Available { get; set; }
    }
}