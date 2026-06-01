using Gestor_Salon_Belleza.Data;
using Microsoft.AspNetCore.Mvc;
using Gestor_Salon_Belleza.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gestor_Salon_Belleza.Controllers
{
    public class AccountController : Controller
    {

        /* Este controlador se encarga
         * de manejar acciones relacionadas con la cuenta
         * de los usuarios, login, registro, logout
         */


        private readonly AppDBContext _context; // Declaracion del contexto de BD

        // Constructor que recibe el contexto de BD por inyeccion de dependencias
        public AccountController(AppDBContext context)
        {
            _context = context; // Asignacion del contexto a la variable local
        }

        [HttpGet] //GET: Porque muestra formulario de login
        public IActionResult Login() {
            if (User.Identity.IsAuthenticated) //condiciona: si el usuario esta logueado va a home
            {
                return RedirectToAction("Index", "Home");
            }
            return View(); //sino se va al login
        }


        /* async ---> ¿Por qué se usa? En lugar de congelar un hilo del servidor esperando 
         * a que la base de datos responda,el hilo se libera para atender a otros usuarios. 
         * Cuando la base de datos termina, el método retoma su ejecución. 
         * Esto hace que la aplicación soporte muchísimos más usuarios en simultáneo.*/

        /* Task<IActionResult> ---> task va de la mano de async,
         * promesa de que el metodo devolvera algo en el futuro al terminar
         * la tarea asincrona. IActionResult puede devolver lo que deseessssss
         * */
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        { 
            return BadRequest();
        }

    }
}
