using System.Diagnostics;
using System.Threading.Tasks;
using Gestor_Salon_Belleza.Data;
using Gestor_Salon_Belleza.Models;
using Gestor_Salon_Belleza.Utils.Enumerables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gestor_Salon_Belleza.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDBContext _context;

        public HomeController(ILogger<HomeController> logger, AppDBContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            if (User.IsInRole(EnumRoles.Administrador.ToString()))
            {
                return RedirectToAction("DashboardAdmin", "Admin");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

       

        public async Task<IActionResult> Services()
        {
            var servicios = await _context.Servicios
                .OrderBy(s => s.Nombre)
                .ToListAsync();

            return View(servicios);
        }

        public async Task<IActionResult> Specialists()
        {
            var profesionales = await _context.Profesionales
                .Include(p => p.Usuario)
                .Include(p => p.Profesional_Servicios)
                    .ThenInclude(ps => ps.Servicio)
                .Where(p => !p.Usuario.Eliminado)
                .OrderBy(p => p.Usuario.Apellido)
                .ToListAsync();

            return View(profesionales);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
