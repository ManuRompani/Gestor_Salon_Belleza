using Gestor_Salon_Belleza.Data;
using Gestor_Salon_Belleza.Utils.Enumerables;
using Gestor_Salon_Belleza.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gestor_Salon_Belleza.Controllers
{
    [Authorize(Roles="Administrador")]
    public class AdminController : Controller
    {
        private readonly AppDBContext _context;

        public AdminController(AppDBContext context)
        {
            _context = context;
        }


        public IActionResult DashboardAdmin()
        {
            return View();
        }

            public async Task<IActionResult> IndexUsuarios()
            {
                var usuarios = await _context.Usuarios
                    .AsNoTracking()
                    .Include(u => u.Rol)
                    .Include(u => u.LoginsExternos)
                    .Where(u => u.Id_Rol != 1)
                    .ToListAsync();

                var model = usuarios.Select(u => new UsuarioAdminVM
                {
                    Id_Usuario = u.Id_Usuario,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Email = u.Email,
                    Rol = u.Rol.Rol_Name,
                    Id_Rol = u.Id_Rol,
                    TieneLoginExt = u.LoginsExternos.Any()
                }).ToList();

                return View(model);
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> ConverToProfesional(int id)
            {
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null) return NotFound();
                usuario.Id_Rol = 3; // Profesional
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Rol actualizado a Profesional.";
                return RedirectToAction("IndexUsuarios");
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> ConverToCliente(int id)
            {
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null) return NotFound();
                usuario.Id_Rol = 2; // Cliente
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Rol actualizado a Cliente.";
                return RedirectToAction("IndexUsuarios");
            }
        }


    }
