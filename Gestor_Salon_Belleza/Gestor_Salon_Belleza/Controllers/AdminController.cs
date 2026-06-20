using Gestor_Salon_Belleza.Data;
using Gestor_Salon_Belleza.Utils.Enumerables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

     

           

    }
}
