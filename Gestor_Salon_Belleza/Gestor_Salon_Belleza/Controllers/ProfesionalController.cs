using Gestor_Salon_Belleza.Data;
using Gestor_Salon_Belleza.Models;
using Gestor_Salon_Belleza.Utils.Enumerables;
using Gestor_Salon_Belleza.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Gestor_Salon_Belleza.Controllers
{
    [Authorize(Roles = "Administrador,Profesional")]
    public class ProfesionalController : Controller
    {
        private const string VistaForm = "Form";

        private const string ModoCreate = "Create";
        private const string ModoDetails = "Details";
        private const string ModoEdit = "Edit";
        private const string ModoDelete = "Delete";
        private const string ModoMiPerfil = "MiPerfil";
        private const string ModoEditMiPerfil = "EditMiPerfil";
        private const string ModoBajaMiPerfil = "BajaMiPerfil";

        private const long TamanoMaximoImagen = 2 * 1024 * 1024;

        private static readonly string[] ExtensionesImagenPermitidas =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        private readonly AppDBContext _context;
        private readonly ILogger<ProfesionalController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfesionalController(
            AppDBContext context,
            ILogger<ProfesionalController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AdminIndex()
        {
            var profesionales = await _context.Profesionales
                .AsNoTracking()
                .Include(p => p.Usuario)
                .Include(p => p.Profesional_Servicios)
                    .ThenInclude(ps => ps.Servicio)
                .Where(p => !p.Usuario.Eliminado)
                .OrderBy(p => p.Usuario.Apellido)
                .ThenBy(p => p.Usuario.Nombre)
                .ToListAsync();

            var model = profesionales.Select(p => new ProfesionalListItemViewModel
            {
                Id_Usuario = p.Id_Usuario,
                NombreCompleto = $"{p.Usuario.Nombre} {p.Usuario.Apellido}",
                Email = p.Usuario.Email,
                Telefono = p.Usuario.Telefono,
                Descripcion = p.Descripcion,
                Servicios = string.Join(", ", p.Profesional_Servicios.Select(ps => ps.Servicio.Nombre)),
                Eliminado = p.Usuario.Eliminado
            }).ToList();

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create()
        {
            return await Formulario(new ProfesionalViewModel(), ModoCreate);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create(ProfesionalViewModel model)
        {
            await ValidarFormulario(model, ModoCreate);

            if (!ModelState.IsValid)
            {
                return await Formulario(model, ModoCreate);
            }

            var idsServiciosSeleccionados = ObtenerIdsServiciosNormalizados(model);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            string? rutaImagenGuardada = null;

            try
            {
                rutaImagenGuardada = await GuardarImagenAsync(model.ImagenArchivo);

                var usuario = new Usuario
                {
                    Nombre = model.Nombre.Trim(),
                    Apellido = model.Apellido.Trim(),
                    Email = model.Email.Trim(),
                    Telefono = model.Telefono,
                    // El profesional se guarda con hash para que el login compare
                    // contra un valor seguro y no contra texto plano.
                    Password = string.Empty,
                    Id_Rol = (int)EnumRoles.Profesional,
                    Eliminado = false
                };

                usuario.Password = HashearPassword(usuario, model.Password!);

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                var profesional = new Profesional
                {
                    Id_Usuario = usuario.Id_Usuario,
                    UrlImagen = rutaImagenGuardada,
                    Descripcion = model.Descripcion
                };

                _context.Profesionales.Add(profesional);

                AgregarServiciosAProfesional(profesional.Id_Usuario, idsServiciosSeleccionados);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(AdminIndex));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                EliminarImagenFisica(rutaImagenGuardada);

                _logger.LogError(ex, "Error al crear profesional.");

                ModelState.AddModelError(string.Empty, "Ocurrió un error al crear el profesional.");
                return await Formulario(model, ModoCreate);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Details(int id)
        {
            return await FormularioDesdeProfesional(id, ModoDetails);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id)
        {
            return await FormularioDesdeProfesional(id, ModoEdit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(ProfesionalViewModel model)
        {
            if (model.Id_Usuario == null)
            {
                return BadRequest();
            }

            await ValidarFormulario(model, ModoEdit);

            if (!ModelState.IsValid)
            {
                return await Formulario(model, ModoEdit);
            }

            bool actualizado = await ActualizarProfesional(model.Id_Usuario.Value, model, ModoEdit);

            if (!actualizado)
            {
                return await Formulario(model, ModoEdit);
            }

            return RedirectToAction(nameof(Details), new { id = model.Id_Usuario.Value });
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            return await FormularioDesdeProfesional(id, ModoDelete);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(ProfesionalViewModel model)
        {
            if (model.Id_Usuario == null)
            {
                return BadRequest();
            }

            bool eliminado = await EliminarUsuarioLogicamente(model.Id_Usuario.Value);

            if (!eliminado)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(AdminIndex));
        }

        [HttpGet]
        [Authorize(Roles = "Profesional")]
        public async Task<IActionResult> MiPerfil()
        {
            int? idUsuario = await ObtenerIdUsuarioLogueado();

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return await FormularioDesdeProfesional(idUsuario.Value, ModoMiPerfil);
        }

        [HttpGet]
        [Authorize(Roles = "Profesional")]
        public async Task<IActionResult> EditarMiPerfil()
        {
            int? idUsuario = await ObtenerIdUsuarioLogueado();

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return await FormularioDesdeProfesional(idUsuario.Value, ModoEditMiPerfil);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Profesional")]
        public async Task<IActionResult> EditarMiPerfil(ProfesionalViewModel model)
        {
            int? idUsuario = await ObtenerIdUsuarioLogueado();

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            model.Id_Usuario = idUsuario.Value;

            await ValidarFormulario(model, ModoEditMiPerfil);

            if (!ModelState.IsValid)
            {
                return await Formulario(model, ModoEditMiPerfil);
            }

            bool actualizado = await ActualizarProfesional(idUsuario.Value, model, ModoEditMiPerfil);

            if (!actualizado)
            {
                return await Formulario(model, ModoEditMiPerfil);
            }

            return RedirectToAction(nameof(MiPerfil));
        }

        [HttpGet]
        [Authorize(Roles = "Profesional")]
        public async Task<IActionResult> BajaMiPerfil()
        {
            int? idUsuario = await ObtenerIdUsuarioLogueado();

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return await FormularioDesdeProfesional(idUsuario.Value, ModoBajaMiPerfil);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Profesional")]
        public async Task<IActionResult> BajaMiPerfil(ProfesionalViewModel model)
        {
            int? idUsuario = await ObtenerIdUsuarioLogueado();

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            bool eliminado = await EliminarUsuarioLogicamente(idUsuario.Value);

            if (!eliminado)
            {
                return NotFound();
            }

            HttpContext.Session.Clear();

            return RedirectToAction("Login", "Account");
        }

        private async Task<IActionResult> Formulario(ProfesionalViewModel model, string modo)
        {
            await CargarServiciosDisponibles(model);

            ViewBag.Modo = modo;

            return View(VistaForm, model);
        }

        [HttpGet]
        public async Task<IActionResult> MisTurnos(string? estado, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            var idUsuario = await ObtenerIdUsuarioLogueado();
            if (idUsuario == null) return RedirectToAction("Login", "Account");

            var query = _context.Turnos
                .Include(t => t.Cliente).ThenInclude(c => c.Usuario)
                .Include(t => t.TurnoServicios).ThenInclude(ts => ts.Servicio)
                .Where(t => t.Id_Profesional == idUsuario.Value)
                .AsQueryable();

            if (estado == "activos")
                query = query.Where(t => !t.Estado);
            else if (estado == "cancelados")
                query = query.Where(t => t.Estado);

            if (fechaDesde.HasValue)
                query = query.Where(t => t.FechaHora >= fechaDesde.Value);
            if (fechaHasta.HasValue)
                query = query.Where(t => t.FechaHora <= fechaHasta.Value.AddDays(1));

            var turnos = await query
                .OrderBy(t => t.FechaHora)
                .Select(t => new ProfesionalTurnoViewModel
                {
                    Id_Turno = t.Id_Turno,
                    ClienteNombre = t.Cliente.Usuario.Nombre + " " + t.Cliente.Usuario.Apellido,
                    ServicioNombre = t.TurnoServicios.Any() ? t.TurnoServicios.First().Servicio.Nombre : "Sin servicio",
                    FechaHora = t.FechaHora,
                    Estado = t.Estado ? "Cancelado" : "Activo",
                    FechaCancelacion = t.FechaCancelacion,
                    DuracionMinutos = t.TurnoServicios.Any() ? t.TurnoServicios.First().Servicio.Duracion_Minutos : 0,
                    Precio = t.TurnoServicios.Any() ? t.TurnoServicios.First().Servicio.Precio : 0
                })
                .ToListAsync();

            ViewBag.EstadoFiltro = estado;
            ViewBag.FechaDesde = fechaDesde?.ToString("yyyy-MM-dd");
            ViewBag.FechaHasta = fechaHasta?.ToString("yyyy-MM-dd");

            return View(turnos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Profesional")]
        public async Task<IActionResult> CancelarTurno(int id)
        {
            var idUsuario = await ObtenerIdUsuarioLogueado();
            if (idUsuario == null) return RedirectToAction("Login", "Account");

            var turno = await _context.Turnos.FindAsync(id);
            if (turno == null || turno.Id_Profesional != idUsuario.Value)
                return RedirectToAction(nameof(MisTurnos));

            turno.Estado = true;
            turno.FechaCancelacion = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Exito"] = "Turno cancelado correctamente.";
            return RedirectToAction(nameof(MisTurnos));
        }

        [HttpGet]
        [Authorize(Roles = "Profesional")]
        public async Task<IActionResult> ReprogramarTurno(int id, DateTime? date)
        {
            var idUsuario = await ObtenerIdUsuarioLogueado();
            if (idUsuario == null) return RedirectToAction("Login", "Account");

            var turno = await _context.Turnos
                .Include(t => t.Cliente).ThenInclude(c => c.Usuario)
                .Include(t => t.TurnoServicios).ThenInclude(ts => ts.Servicio)
                .FirstOrDefaultAsync(t => t.Id_Turno == id && t.Id_Profesional == idUsuario.Value);

            if (turno == null || turno.Estado)
                return RedirectToAction(nameof(MisTurnos));

            var servicio = turno.TurnoServicios.FirstOrDefault()?.Servicio;
            var duracion = servicio?.Duracion_Minutos ?? 45;
            var selectedDate = date ?? turno.FechaHora.Date;

            var occupiedSlots = await _context.Turnos
                .Where(t => t.Id_Profesional == idUsuario.Value
                            && t.FechaHora.Date == selectedDate.Date
                            && t.Estado == false
                            && t.Id_Turno != id)
                .Select(t => t.FechaHora)
                .ToListAsync();

            var slots = GenerateTimeSlotsProfesional(selectedDate, duracion, occupiedSlots);

            ViewBag.TurnoId = turno.Id_Turno;
            ViewBag.ClienteNombre = turno.Cliente.Usuario.Nombre + " " + turno.Cliente.Usuario.Apellido;
            ViewBag.ServicioNombre = servicio?.Nombre ?? "";
            ViewBag.Duracion = duracion;
            ViewBag.FechaActual = turno.FechaHora.ToString("dd/MM/yyyy HH:mm");
            ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");

            return View(slots);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Profesional")]
        public async Task<IActionResult> ReprogramarTurno(int id, string nuevaFecha, string nuevaHora)
        {
            var idUsuario = await ObtenerIdUsuarioLogueado();
            if (idUsuario == null) return RedirectToAction("Login", "Account");

            var turno = await _context.Turnos
                .Include(t => t.TurnoServicios).ThenInclude(ts => ts.Servicio)
                .FirstOrDefaultAsync(t => t.Id_Turno == id && t.Id_Profesional == idUsuario.Value);

            if (turno == null || turno.Estado)
                return RedirectToAction(nameof(MisTurnos));

            if (string.IsNullOrEmpty(nuevaFecha) || string.IsNullOrEmpty(nuevaHora))
            {
                TempData["Error"] = "Seleccioná una fecha y hora.";
                return RedirectToAction(nameof(ReprogramarTurno), new { id });
            }

            var nuevaFechaHora = DateTime.Parse($"{nuevaFecha} {nuevaHora}");

            var duracion = turno.TurnoServicios.FirstOrDefault()?.Servicio.Duracion_Minutos ?? 45;

            var ocupado = await _context.Turnos
                .AnyAsync(t => t.Id_Profesional == idUsuario.Value
                              && t.FechaHora.Date == nuevaFechaHora.Date
                              && t.Estado == false
                              && t.Id_Turno != id
                              && t.FechaHora < nuevaFechaHora.AddMinutes(duracion)
                              && t.FechaHora.AddMinutes(duracion) > nuevaFechaHora);

            if (ocupado)
            {
                TempData["Error"] = "Ese horario ya está ocupado. Elegí otro.";
                return RedirectToAction(nameof(ReprogramarTurno), new { id, date = nuevaFecha });
            }

            turno.FechaHora = nuevaFechaHora;
            await _context.SaveChangesAsync();

            TempData["Exito"] = "Turno reprogramado correctamente.";
            return RedirectToAction(nameof(MisTurnos));
        }

        private List<TimeSlotItem> GenerateTimeSlotsProfesional(DateTime date, int durationMinutes, List<DateTime> occupiedSlots)
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

        private async Task<IActionResult> FormularioDesdeProfesional(int idUsuario, string modo)
        {
            var model = await ObtenerProfesionalViewModel(idUsuario);

            if (model == null)
            {
                return NotFound();
            }

            return await Formulario(model, modo);
        }

        private async Task<int?> ObtenerIdUsuarioLogueado()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email)) return null;
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            return usuario?.Id_Usuario;
        }

        private async Task<ProfesionalViewModel?> ObtenerProfesionalViewModel(int idUsuario)
        {
            var profesional = await _context.Profesionales
                .AsNoTracking()
                .Include(p => p.Usuario)
                .Include(p => p.Profesional_Servicios)
                .FirstOrDefaultAsync(p => p.Id_Usuario == idUsuario && !p.Usuario.Eliminado);

            if (profesional == null)
            {
                return null;
            }

            return new ProfesionalViewModel
            {
                Id_Usuario = profesional.Id_Usuario,
                Nombre = profesional.Usuario.Nombre,
                Apellido = profesional.Usuario.Apellido,
                Email = profesional.Usuario.Email,
                Telefono = profesional.Usuario.Telefono,
                UrlImagen = profesional.UrlImagen,
                Descripcion = profesional.Descripcion,
                IdsServiciosSeleccionados = profesional.Profesional_Servicios
                    .Select(ps => ps.Id_Servicio)
                    .ToList()
            };
        }

        private async Task CargarServiciosDisponibles(ProfesionalViewModel model)
        {
            model.ServiciosDisponibles = await _context.Servicios
                .AsNoTracking()
                .OrderBy(s => s.Nombre)
                .Select(s => new SelectListItem
                {
                    Value = s.Id_Servicio.ToString(),
                    Text = s.Nombre
                })
                .ToListAsync();
        }

        private async Task ValidarFormulario(ProfesionalViewModel model, string modo)
        {
            ValidarPassword(model, modo);
            ValidarServiciosSeleccionados(model);
            ValidarImagen(model.ImagenArchivo);

            await ValidarEmailUnico(model);
            await ValidarServiciosExistentes(model);
        }

        private void ValidarPassword(ProfesionalViewModel model, string modo)
        {
            bool esCreacion = modo == ModoCreate;

            bool quiereCambiarPassword =
                !string.IsNullOrWhiteSpace(model.Password) ||
                !string.IsNullOrWhiteSpace(model.ConfirmarPassword);

            if (!esCreacion && !quiereCambiarPassword)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(
                    nameof(model.Password),
                    esCreacion ? "La contraseña es obligatoria." : "Debe ingresar la nueva contraseña.");
            }
            else if (model.Password.Length < 8)
            {
                ModelState.AddModelError(nameof(model.Password), "Mínimo 8 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(model.ConfirmarPassword))
            {
                ModelState.AddModelError(
                    nameof(model.ConfirmarPassword),
                    esCreacion ? "Debe confirmar la contraseña." : "Debe confirmar la nueva contraseña.");
            }

            if (!string.IsNullOrWhiteSpace(model.Password) &&
                !string.IsNullOrWhiteSpace(model.ConfirmarPassword) &&
                model.Password != model.ConfirmarPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmarPassword), "Las contraseñas no coinciden.");
            }
        }

        private void ValidarServiciosSeleccionados(ProfesionalViewModel model)
        {
            if (model.IdsServiciosSeleccionados == null || !model.IdsServiciosSeleccionados.Any())
            {
                ModelState.AddModelError(nameof(model.IdsServiciosSeleccionados), "Debe seleccionar al menos un servicio.");
            }
        }

        private async Task ValidarEmailUnico(ProfesionalViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                return;
            }

            string email = model.Email.Trim();

            bool emailYaExiste = await _context.Usuarios.AnyAsync(u =>
                u.Email == email &&
                !u.Eliminado &&
                (model.Id_Usuario == null || u.Id_Usuario != model.Id_Usuario.Value));

            if (emailYaExiste)
            {
                ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario registrado con ese email.");
            }
        }

        private async Task ValidarServiciosExistentes(ProfesionalViewModel model)
        {
            var idsServicios = ObtenerIdsServiciosNormalizados(model);

            if (!idsServicios.Any())
            {
                return;
            }

            int cantidadServiciosExistentes = await _context.Servicios
                .CountAsync(s => idsServicios.Contains(s.Id_Servicio));

            if (cantidadServiciosExistentes != idsServicios.Count)
            {
                ModelState.AddModelError(nameof(model.IdsServiciosSeleccionados), "Uno o más servicios seleccionados no son válidos.");
            }
        }

        private static List<int> ObtenerIdsServiciosNormalizados(ProfesionalViewModel model)
        {
            return model.IdsServiciosSeleccionados?
                .Distinct()
                .ToList() ?? new List<int>();
        }

        private void ValidarImagen(IFormFile? imagen)
        {
            if (imagen == null || imagen.Length == 0)
            {
                return;
            }

            if (imagen.Length > TamanoMaximoImagen)
            {
                ModelState.AddModelError(nameof(ProfesionalViewModel.ImagenArchivo), "La imagen no puede superar los 2 MB.");
                return;
            }

            string extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();

            if (!ExtensionesImagenPermitidas.Contains(extension))
            {
                ModelState.AddModelError(nameof(ProfesionalViewModel.ImagenArchivo), "La imagen debe ser JPG, JPEG, PNG o WEBP.");
                return;
            }

            if (!imagen.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(ProfesionalViewModel.ImagenArchivo), "El archivo seleccionado no es una imagen válida.");
            }
        }

        private async Task<bool> ActualizarProfesional(int idUsuario, ProfesionalViewModel model, string modo)
        {
            var profesional = await _context.Profesionales
                .Include(p => p.Usuario)
                .Include(p => p.Profesional_Servicios)
                .FirstOrDefaultAsync(p => p.Id_Usuario == idUsuario && !p.Usuario.Eliminado);

            if (profesional == null)
            {
                ModelState.AddModelError(string.Empty, "No se encontró el profesional.");
                return false;
            }

            var idsServiciosSeleccionados = ObtenerIdsServiciosNormalizados(model);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            string? imagenAnterior = null;
            string? nuevaImagenGuardada = null;
            bool cambioImagen = false;

            try
            {
                profesional.Usuario.Nombre = model.Nombre.Trim();
                profesional.Usuario.Apellido = model.Apellido.Trim();
                profesional.Usuario.Email = model.Email.Trim();
                profesional.Usuario.Telefono = model.Telefono;

                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    // En edición solo reemplazamos la contraseña si el formulario trae
                    // un nuevo valor. Antes de guardar lo convertimos a hash.
                    profesional.Usuario.Password = HashearPassword(profesional.Usuario, model.Password);
                }

                if (model.ImagenArchivo != null && model.ImagenArchivo.Length > 0)
                {
                    imagenAnterior = profesional.UrlImagen;
                    nuevaImagenGuardada = await GuardarImagenAsync(model.ImagenArchivo);

                    profesional.UrlImagen = nuevaImagenGuardada;
                    cambioImagen = true;
                }

                profesional.Descripcion = model.Descripcion;

                _context.Profesional_Servicios.RemoveRange(profesional.Profesional_Servicios);
                AgregarServiciosAProfesional(profesional.Id_Usuario, idsServiciosSeleccionados);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (cambioImagen)
                {
                    EliminarImagenFisica(imagenAnterior);
                }

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                EliminarImagenFisica(nuevaImagenGuardada);

                _logger.LogError(ex, "Error al actualizar profesional. Modo: {Modo}", modo);

                ModelState.AddModelError(string.Empty, "Ocurrió un error al actualizar el profesional.");

                return false;
            }
        }

        private void AgregarServiciosAProfesional(int idProfesional, IEnumerable<int> idsServicios)
        {
            foreach (int idServicio in idsServicios)
            {
                _context.Profesional_Servicios.Add(new Profesional_Servicio
                {
                    Id_Profesional = idProfesional,
                    Id_Servicio = idServicio
                });
            }
        }

        private async Task<bool> EliminarUsuarioLogicamente(int idUsuario)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id_Usuario == idUsuario && !u.Eliminado);

            if (usuario == null)
            {
                return false;
            }

            usuario.Eliminado = true;

            await _context.SaveChangesAsync();

            return true;
        }

        private async Task<string?> GuardarImagenAsync(IFormFile? imagen)
        {
            if (imagen == null || imagen.Length == 0)
            {
                return null;
            }

            string extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();
            string nombreArchivo = $"{Guid.NewGuid():N}{extension}";

            string carpetaRelativa = Path.Combine("uploads", "profesionales");
            string carpetaFisica = Path.Combine(ObtenerWebRootPath(), carpetaRelativa);

            Directory.CreateDirectory(carpetaFisica);

            string rutaFisica = Path.Combine(carpetaFisica, nombreArchivo);

            await using var stream = new FileStream(rutaFisica, FileMode.Create);
            await imagen.CopyToAsync(stream);

            return "/" + Path.Combine(carpetaRelativa, nombreArchivo).Replace("\\", "/");
        }

        private void EliminarImagenFisica(string? rutaRelativa)
        {
            if (string.IsNullOrWhiteSpace(rutaRelativa))
            {
                return;
            }

            string rutaNormalizada = rutaRelativa
                .TrimStart('/')
                .Replace("/", Path.DirectorySeparatorChar.ToString());

            string rutaFisica = Path.Combine(ObtenerWebRootPath(), rutaNormalizada);

            if (System.IO.File.Exists(rutaFisica))
            {
                System.IO.File.Delete(rutaFisica);
            }
        }

        private string ObtenerWebRootPath()
        {
            return _webHostEnvironment.WebRootPath
                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        private static string HashearPassword(Usuario usuario, string passwordPlano)
        {
            var hasher = new PasswordHasher<Usuario>();
            return hasher.HashPassword(usuario, passwordPlano);
        }
    }
}
