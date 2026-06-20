using Gestor_Salon_Belleza.Data;
using Gestor_Salon_Belleza.Models;
using Gestor_Salon_Belleza.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gestor_Salon_Belleza.Controllers
{
    public class ServicesController : Controller
    {

        /* -alta -->> LISTO
         * -modificacion
         * -baja logica
         * -listado
         */
        private readonly AppDBContext _context;

        public ServicesController(AppDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var services = await _context.Servicios
                .Where(s => s.Activo) // Solo mostrar servicios activos
                .Select(s => new ViewModels.ServicesViewModel
                {
                    Id = s.Id_Servicio,
                    Nombre = s.Nombre,
                    Descripcion = s.Descripcion,
                    Duracion = s.Duracion_Minutos,
                    Precio = s.Precio
                }).ToListAsync();
                
            return View(services);
        }


        /*=== ALTA ===*/
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(ServicesViewModel model)
        {

            if (!ModelState.IsValid){ return View(model); }

            
            bool existe = await _context.Servicios.AnyAsync(s => s.Nombre.ToLower() == model.Nombre.ToLower() && s.Activo);
            
            if (existe)
            {
                ModelState.AddModelError("Nombre", "Ya existe un servicio activo con ese nombre.");
                return View(model);
            }

            var nuevoServicio = new Servicio
            {
                Nombre = model.Nombre,
                Descripcion = model.Descripcion,
                Duracion_Minutos = model.Duracion,
                Precio = model.Precio,
                Activo = true
            };

            _context.Servicios.Add(nuevoServicio);
            await _context.SaveChangesAsync();


            TempData["Exito"] = "Servicio registrado exitosamente.";
            return RedirectToAction("Create"); 
        }

        /*=== MODIFICACION ===*/
        public async Task<IActionResult> Edit(int id)
        {
            var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Id_Servicio == id && s.Activo);
            if (servicio == null) return NotFound();

            var model = new ServicesViewModel
            {
                Id = servicio.Id_Servicio,
                Nombre = servicio.Nombre,
                Descripcion = servicio.Descripcion,
                Duracion = servicio.Duracion_Minutos,
                Precio = servicio.Precio
            };

            return View(model); // Llama a Edit.cshtml pasándole los datos actuales
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(int id, ServicesViewModel model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);

            // Validar que el nuevo nombre no choque con otro servicio activo
            bool duplicado = await _context.Servicios.AnyAsync(s => s.Nombre.ToLower() == model.Nombre.ToLower() && s.Id_Servicio != id && s.Activo);
            if (duplicado)
            {
                ModelState.AddModelError("Nombre", "Ya existe otro servicio activo con ese nombre.");
                return View(model);
            }

            // Buscar el servicio a modificar
            var servicio = await _context.Servicios.FindAsync(id);
            if (servicio == null || !servicio.Activo) return NotFound();

            servicio.Nombre = model.Nombre;
            servicio.Descripcion = model.Descripcion;
            servicio.Duracion_Minutos = model.Duracion;
            servicio.Precio = model.Precio;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        /*=== BAJA LOGICA ===*/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(int id)
        {
            var servicio = await _context.Servicios.FindAsync(id);
            if (servicio == null) return NotFound();

            servicio.Activo = false; // Desactivación en lugar de borrado físico
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }



    }

}
