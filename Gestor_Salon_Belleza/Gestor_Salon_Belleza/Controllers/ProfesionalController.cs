using Gestor_Salon_Belleza.Data;
using Gestor_Salon_Belleza.Models;
using Gestor_Salon_Belleza.Utils.Enumerables;
using Gestor_Salon_Belleza.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Gestor_Salon_Belleza.Controllers
{
    public class ProfesionalController : Controller
    {
        private readonly AppDBContext _context;
        private readonly ILogger<ProfesionalController> _logger;

        public ProfesionalController(AppDBContext context, ILogger<ProfesionalController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =========================
        // ADMIN - LISTADO
        // =========================

        [HttpGet]
        public async Task<IActionResult> AdminIndex()
        {
            var profesionales = await _context.Profesionales
                .Include(p => p.Usuario)
                .Include(p => p.Profesional_Servicios)
                    .ThenInclude(ps => ps.Servicio)
                .Where(p => !p.Usuario.Eliminado)
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

        // =========================
        // ADMIN - CREATE
        // =========================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new ProfesionalViewModel();

            await CargarServiciosDisponibles(model);

            ViewBag.Modo = "Create";

            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProfesionalViewModel model)
        {
            ValidarPasswordCreacion(model);
            ValidarServiciosSeleccionados(model);

            if (!ModelState.IsValid)
            {
                await CargarServiciosDisponibles(model);
                ViewBag.Modo = "Create";
                return View("Form", model);
            }

            bool emailYaExiste = await _context.Usuarios
                .AnyAsync(u => u.Email == model.Email && !u.Eliminado);

            if (emailYaExiste)
            {
                ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario registrado con ese email.");
                await CargarServiciosDisponibles(model);
                ViewBag.Modo = "Create";
                return View("Form", model);
            }

            var idsServiciosSeleccionados = model.IdsServiciosSeleccionados
                .Distinct()
                .ToList();

            bool serviciosValidos = await ValidarServiciosExistentes(idsServiciosSeleccionados);

            if (!serviciosValidos)
            {
                ModelState.AddModelError(nameof(model.IdsServiciosSeleccionados), "Uno o más servicios seleccionados no son válidos.");
                await CargarServiciosDisponibles(model);
                ViewBag.Modo = "Create";
                return View("Form", model);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var usuario = new Usuario
                {
                    Nombre = model.Nombre,
                    Apellido = model.Apellido,
                    Email = model.Email,
                    Telefono = model.Telefono,
                    Password = model.Password,
                    Id_Rol = (int)EnumRoles.Profesional,
                    Eliminado = false
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                var profesional = new Profesional
                {
                    Id_Usuario = usuario.Id_Usuario,
                    UrlImagen = model.UrlImagen,
                    Descripcion = model.Descripcion
                };

                _context.Profesionales.Add(profesional);
                await _context.SaveChangesAsync();

                foreach (var idServicio in idsServiciosSeleccionados)
                {
                    var profesionalServicio = new Profesional_Servicio
                    {
                        Id_Profesional = profesional.Id_Usuario,
                        Id_Servicio = idServicio
                    };

                    _context.Profesional_Servicios.Add(profesionalServicio);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "Error al crear profesional.");

                ModelState.AddModelError(string.Empty, "Ocurrió un error al crear el profesional.");
                await CargarServiciosDisponibles(model);
                ViewBag.Modo = "Create";

                return View("Form", model);
            }
        }

        // =========================
        // ADMIN - READ
        // =========================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var model = await ObtenerProfesionalViewModel(id);

            if (model == null)
            {
                return NotFound();
            }

            ViewBag.Modo = "Details";

            return View("Form", model);
        }

        // =========================
        // ADMIN - UPDATE
        // =========================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await ObtenerProfesionalViewModel(id);

            if (model == null)
            {
                return NotFound();
            }

            ViewBag.Modo = "Edit";

            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfesionalViewModel model)
        {
            if (model.Id_Usuario == null)
            {
                return BadRequest();
            }

            ValidarPasswordEdicion(model);
            ValidarServiciosSeleccionados(model);

            if (!ModelState.IsValid)
            {
                await CargarServiciosDisponibles(model);
                ViewBag.Modo = "Edit";
                return View("Form", model);
            }

            var resultado = await ActualizarProfesional(model.Id_Usuario.Value, model, "Edit");

            if (!resultado.Exito)
            {
                await CargarServiciosDisponibles(model);
                ViewBag.Modo = "Edit";
                return View("Form", model);
            }

            return RedirectToAction(nameof(Details), new { id = model.Id_Usuario.Value });
        }

        // =========================
        // ADMIN - DELETE LÓGICO
        // =========================

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var model = await ObtenerProfesionalViewModel(id);

            if (model == null)
            {
                return NotFound();
            }

            ViewBag.Modo = "Delete";

            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(ProfesionalViewModel model)
        {
            if (model.Id_Usuario == null)
            {
                return BadRequest();
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id_Usuario == model.Id_Usuario.Value && !u.Eliminado);

            if (usuario == null)
            {
                return NotFound();
            }

            usuario.Eliminado = true;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // PROFESIONAL - MI PERFIL
        // =========================

        [HttpGet]
        public async Task<IActionResult> MiPerfil()
        {
            int? idUsuario = ObtenerIdUsuarioLogueado();

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await ObtenerProfesionalViewModel(idUsuario.Value);

            if (model == null)
            {
                return NotFound();
            }

            ViewBag.Modo = "MiPerfil";

            return View("Form", model);
        }

        [HttpGet]
        public async Task<IActionResult> EditarMiPerfil()
        {
            int? idUsuario = ObtenerIdUsuarioLogueado();

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await ObtenerProfesionalViewModel(idUsuario.Value);

            if (model == null)
            {
                return NotFound();
            }

            ViewBag.Modo = "EditMiPerfil";

            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarMiPerfil(ProfesionalViewModel model)
        {
            int? idUsuario = ObtenerIdUsuarioLogueado();

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            model.Id_Usuario = idUsuario.Value;

            ValidarPasswordEdicion(model);
            ValidarServiciosSeleccionados(model);

            if (!ModelState.IsValid)
            {
                await CargarServiciosDisponibles(model);
                ViewBag.Modo = "EditMiPerfil";
                return View("Form", model);
            }

            var resultado = await ActualizarProfesional(idUsuario.Value, model, "EditMiPerfil");

            if (!resultado.Exito)
            {
                await CargarServiciosDisponibles(model);
                ViewBag.Modo = "EditMiPerfil";
                return View("Form", model);
            }

            return RedirectToAction(nameof(MiPerfil));
        }

        [HttpGet]
        public async Task<IActionResult> BajaMiPerfil()
        {
            int? idUsuario = ObtenerIdUsuarioLogueado();

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await ObtenerProfesionalViewModel(idUsuario.Value);

            if (model == null)
            {
                return NotFound();
            }

            ViewBag.Modo = "BajaMiPerfil";

            return View("Form", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BajaMiPerfil(ProfesionalViewModel model)
        {
            int? idUsuario = ObtenerIdUsuarioLogueado();

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id_Usuario == idUsuario.Value && !u.Eliminado);

            if (usuario == null)
            {
                return NotFound();
            }

            usuario.Eliminado = true;

            await _context.SaveChangesAsync();

            HttpContext.Session.Clear();

            return RedirectToAction("Login", "Account");
        }

        // =========================
        // MÉTODOS PRIVADOS
        // =========================

        private int? ObtenerIdUsuarioLogueado()
        {
            return HttpContext.Session.GetInt32("Id_Usuario");
        }

        private async Task<ProfesionalViewModel?> ObtenerProfesionalViewModel(int idUsuario)
        {
            var profesional = await _context.Profesionales
                .Include(p => p.Usuario)
                .Include(p => p.Profesional_Servicios)
                .FirstOrDefaultAsync(p => p.Id_Usuario == idUsuario && !p.Usuario.Eliminado);

            if (profesional == null)
            {
                return null;
            }

            var model = new ProfesionalViewModel
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

            await CargarServiciosDisponibles(model);

            return model;
        }

        private async Task CargarServiciosDisponibles(ProfesionalViewModel model)
        {
            model.ServiciosDisponibles = await _context.Servicios
                .OrderBy(s => s.Nombre)
                .Select(s => new SelectListItem
                {
                    Value = s.Id_Servicio.ToString(),
                    Text = s.Nombre
                })
                .ToListAsync();
        }

        private void ValidarPasswordCreacion(ProfesionalViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "La contraseña es obligatoria.");
            }
            else if (model.Password.Length < 8)
            {
                ModelState.AddModelError(nameof(model.Password), "Mínimo 8 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(model.ConfirmarPassword))
            {
                ModelState.AddModelError(nameof(model.ConfirmarPassword), "Debe confirmar la contraseña.");
            }

            if (!string.IsNullOrWhiteSpace(model.Password) &&
                !string.IsNullOrWhiteSpace(model.ConfirmarPassword) &&
                model.Password != model.ConfirmarPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmarPassword), "Las contraseñas no coinciden.");
            }
        }

        private void ValidarPasswordEdicion(ProfesionalViewModel model)
        {
            bool quiereCambiarPassword =
                !string.IsNullOrWhiteSpace(model.Password) ||
                !string.IsNullOrWhiteSpace(model.ConfirmarPassword);

            if (!quiereCambiarPassword)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "Debe ingresar la nueva contraseña.");
            }
            else if (model.Password.Length < 8)
            {
                ModelState.AddModelError(nameof(model.Password), "Mínimo 8 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(model.ConfirmarPassword))
            {
                ModelState.AddModelError(nameof(model.ConfirmarPassword), "Debe confirmar la nueva contraseña.");
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

        private async Task<bool> ValidarServiciosExistentes(List<int> idsServicios)
        {
            if (idsServicios == null || !idsServicios.Any())
            {
                return false;
            }

            int cantidadServiciosExistentes = await _context.Servicios
                .CountAsync(s => idsServicios.Contains(s.Id_Servicio));

            return cantidadServiciosExistentes == idsServicios.Count;
        }

        private async Task<(bool Exito, string? Error)> ActualizarProfesional(int idUsuario, ProfesionalViewModel model, string modo)
        {
            bool emailYaExiste = await _context.Usuarios
                .AnyAsync(u =>
                    u.Email == model.Email &&
                    u.Id_Usuario != idUsuario &&
                    !u.Eliminado);

            if (emailYaExiste)
            {
                ModelState.AddModelError(nameof(model.Email), "Ya existe otro usuario registrado con ese email.");
                return (false, "Email duplicado");
            }

            var idsServiciosSeleccionados = model.IdsServiciosSeleccionados
                .Distinct()
                .ToList();

            bool serviciosValidos = await ValidarServiciosExistentes(idsServiciosSeleccionados);

            if (!serviciosValidos)
            {
                ModelState.AddModelError(nameof(model.IdsServiciosSeleccionados), "Uno o más servicios seleccionados no son válidos.");
                return (false, "Servicios inválidos");
            }

            var profesional = await _context.Profesionales
                .Include(p => p.Usuario)
                .Include(p => p.Profesional_Servicios)
                .FirstOrDefaultAsync(p => p.Id_Usuario == idUsuario && !p.Usuario.Eliminado);

            if (profesional == null)
            {
                ModelState.AddModelError(string.Empty, "No se encontró el profesional.");
                return (false, "Profesional no encontrado");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                profesional.Usuario.Nombre = model.Nombre;
                profesional.Usuario.Apellido = model.Apellido;
                profesional.Usuario.Email = model.Email;
                profesional.Usuario.Telefono = model.Telefono;

                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    profesional.Usuario.Password = model.Password;
                }

                profesional.UrlImagen = model.UrlImagen;
                profesional.Descripcion = model.Descripcion;

                _context.Profesional_Servicios.RemoveRange(profesional.Profesional_Servicios);

                foreach (var idServicio in idsServiciosSeleccionados)
                {
                    var profesionalServicio = new Profesional_Servicio
                    {
                        Id_Profesional = profesional.Id_Usuario,
                        Id_Servicio = idServicio
                    };

                    _context.Profesional_Servicios.Add(profesionalServicio);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "Error al actualizar profesional. Modo: {Modo}", modo);

                ModelState.AddModelError(string.Empty, "Ocurrió un error al actualizar el profesional.");

                return (false, "Error interno");
            }
        }
    }
}
