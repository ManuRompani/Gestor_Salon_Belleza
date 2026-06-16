using Gestor_Salon_Belleza.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestor_Salon_Belleza.Controllers
{
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

        public IActionResult EditServices() { 
            return View();
    }

        public IActionResult RegisterServices() {
            return View();
        }

        public IActionResult DashboardServices() {
            return View();
        }

    }
}
