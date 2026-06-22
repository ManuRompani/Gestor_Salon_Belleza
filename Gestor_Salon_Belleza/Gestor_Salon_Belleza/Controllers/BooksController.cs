using Gestor_Salon_Belleza.Data;
using Gestor_Salon_Belleza.Models;
using Gestor_Salon_Belleza.Utils.Enumerables;
using Gestor_Salon_Belleza.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Gestor_Salon_Belleza.Controllers
{
    public class BooksController : Controller
    {
        private readonly AppDBContext _context;

        public BooksController(AppDBContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Cliente")]
        [HttpGet]
        public async Task<IActionResult> SelectProfessional(int serviceId)
        {
            var service = await _context.Servicios.FindAsync(serviceId);
            if (service == null) return RedirectToAction("Services", "Home");

            var professionals = await _context.Profesionales
                .Include(p => p.Usuario)
                .Include(p => p.Profesional_Servicios)
                .Where(p => p.Profesional_Servicios.Any(ps => ps.Id_Servicio == serviceId)
                            && !p.Usuario.Eliminado)
                .Select(p => new ProfessionalItem
                {
                    Id = p.Id_Usuario,
                    FullName = p.Usuario.Nombre + " " + p.Usuario.Apellido,
                    ImageUrl = p.UrlImagen,
                    Description = p.Descripcion
                })
                .ToListAsync();

            var model = new BookAppointmentViewModel
            {
                ServiceId = serviceId,
                ServiceName = service.Nombre,
                DurationMinutes = service.Duracion_Minutos,
                Professionals = professionals
            };

            return View(model);
        }

        [Authorize(Roles = "Cliente")]
        [HttpGet]
        public async Task<IActionResult> SelectSchedule(int serviceId, int professionalId, DateTime? date)
        {
            var service = await _context.Servicios.FindAsync(serviceId);
            var professional = await _context.Profesionales
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id_Usuario == professionalId);

            if (service == null || professional == null)
                return RedirectToAction("Services", "Home");

            var selectedDate = date ?? DateTime.Today;

            var occupiedSlots = await _context.Turnos
                .Where(t => t.Id_Profesional == professionalId
                            && t.FechaHora.Date == selectedDate.Date
                            && t.Estado == false)
                .Select(t => t.FechaHora)
                .ToListAsync();

            var slots = GenerateTimeSlots(selectedDate, service.Duracion_Minutos, occupiedSlots);

            var model = new BookAppointmentViewModel
            {
                ServiceId = serviceId,
                ServiceName = service.Nombre,
                ProfessionalId = professionalId,
                ProfessionalName = professional.Usuario.Nombre + " " + professional.Usuario.Apellido,
                DurationMinutes = service.Duracion_Minutos,
                PreferredDate = selectedDate,
                AvailableSlots = slots
            };

            return View(model);
        }

        [Authorize(Roles = "Cliente")]
        [HttpGet]
        public async Task<IActionResult> SelectServiceForProfessional(int professionalId)
        {
            var professional = await _context.Profesionales
                .Include(p => p.Usuario)
                .Include(p => p.Profesional_Servicios).ThenInclude(ps => ps.Servicio)
                .FirstOrDefaultAsync(p => p.Id_Usuario == professionalId && !p.Usuario.Eliminado);

            if (professional == null) return RedirectToAction("Specialists", "Home");

            var model = new BookAppointmentViewModel
            {
                ProfessionalId = professionalId,
                ProfessionalName = professional.Usuario.Nombre + " " + professional.Usuario.Apellido,
                Professionals = professional.Profesional_Servicios.Select(ps => new ProfessionalItem
                {
                    Id = ps.Servicio.Id_Servicio,
                    FullName = ps.Servicio.Nombre,
                    Description = ps.Servicio.Descripcion,
                    DurationMinutes = ps.Servicio.Duracion_Minutos
                }).ToList()
            };

            return View(model);
        }

        [Authorize(Roles = "Cliente")]
        [HttpGet]
        public async Task<IActionResult> Confirm(int serviceId, int professionalId, DateTime dateTime)
        {
            var service = await _context.Servicios.FindAsync(serviceId);
            var professional = await _context.Profesionales
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id_Usuario == professionalId);

            if (service == null || professional == null)
                return RedirectToAction("Services", "Home");

            var model = new BookAppointmentViewModel
            {
                ServiceId = serviceId,
                ServiceName = service.Nombre,
                ProfessionalId = professionalId,
                ProfessionalName = professional.Usuario.Nombre + " " + professional.Usuario.Apellido,
                DurationMinutes = service.Duracion_Minutos,
                PreferredDate = dateTime,
                PreferredTime = dateTime.ToString("HH:mm"),
                Precio = service.Precio
            };

            return View(model);
        }

        [Authorize(Roles = "Cliente")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int serviceId, int professionalId, string preferredDate, string preferredTime)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            if (usuario == null) return RedirectToAction("Login", "Account");

            var cliente = await _context.Clientes.FindAsync(usuario.Id_Usuario);
            if (cliente == null) return RedirectToAction("RedirectByRol", "Account");

            if (!DateTime.TryParse(preferredDate, out var dateTime))
                return RedirectToAction("MyAppointments");

            var turno = new Turno
            {
                Id_Cliente = cliente.Id_Usuario,
                Id_Profesional = professionalId,
                FechaHora = dateTime,
                Estado = false
            };
            
                _context.Turnos.Add(turno);
                await _context.SaveChangesAsync();

                _context.Turno_Servicios.Add(new Turno_Servicio
                {
                    Id_Turno = turno.Id_Turno,
                    Id_Servicio = serviceId
                });

                await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Success), new { id = turno.Id_Turno });
        }

        [Authorize(Roles = "Cliente")]
        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            var turno = await _context.Turnos
                .Include(t => t.Profesional).ThenInclude(p => p.Usuario)
                .Include(t => t.TurnoServicios).ThenInclude(ts => ts.Servicio)
                .FirstOrDefaultAsync(t => t.Id_Turno == id);

            if (turno == null) return RedirectToAction("Index", "Home");

            return View(turno);
        }

        [Authorize(Roles = "Cliente")]
        [HttpGet]
        public async Task<IActionResult> MyAppointments()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            if (usuario == null) return RedirectToAction("Login", "Account");

            var turnos = await _context.Turnos
                .Include(t => t.Profesional).ThenInclude(p => p.Usuario)
                .Include(t => t.TurnoServicios).ThenInclude(ts => ts.Servicio)
                .Where(t => t.Id_Cliente == usuario.Id_Usuario)
                .OrderByDescending(t => t.FechaHora)
                .ToListAsync();

            return View(turnos);
        }

        [Authorize(Roles = "Cliente")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var turno = await _context.Turnos.FindAsync(id);
            if (turno == null) return RedirectToAction(nameof(MyAppointments));

            turno.Estado = true;
            turno.FechaCancelacion = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Exito"] = "Turno cancelado correctamente.";
            return RedirectToAction(nameof(MyAppointments));
        }

        private List<TimeSlotItem> GenerateTimeSlots(DateTime date, int durationMinutes, List<DateTime> occupiedSlots)
        {
            var slots = new List<TimeSlotItem>();
            var start = date.Date.AddHours(9);
            var end = date.Date.AddHours(18);

            for (var time = start; time.AddMinutes(durationMinutes) <= end; time = time.AddMinutes(durationMinutes))
            {
                var isOccupied = occupiedSlots.Any(o =>
                    o < time.AddMinutes(durationMinutes) && o.AddMinutes(durationMinutes) > time);

                slots.Add(new TimeSlotItem
                {
                    DateTime = time,
                    Display = time.ToString("HH:mm") + " - " + time.AddMinutes(durationMinutes).ToString("HH:mm"),
                    Available = !isOccupied
                });
            }

            return slots;
        }
    }
}