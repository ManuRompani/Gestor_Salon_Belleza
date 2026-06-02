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
            else
            {
                return View(); //sino se va al login
            }
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
        public async Task<IActionResult> LoginAuthenticate(LoginViewModel model)
        {

            if (!ModelState.IsValid)
            {
                return View("Login", model);
            }
            //Recibe un modelo LoginViewModel con los datos del form del login
            //Busco en la bd un usuario que coincida con los datos del form
            // se lo asigno a usuarioBuscado
            var usuarioBuscado = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == model.Email && u.Password == model.Password);

            //Con un if : si existe redirijo al home del tipo de usuario
            //sino muestro mensaje de error y redirijo nuevamente a login
            if (usuarioBuscado == null) {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas");
                return View("Login", model);
            }

            int rol_id = usuarioBuscado.Id_Rol; //para usar en switch y derivar correctamente

            switch (rol_id) {

                case 1:
                    //admin
                    return RedirectToAction("Index", "Home");
                case 2:
                    //cliente
                    return RedirectToAction("Index", "Home");
                case 3:
                    //profesional
                    return RedirectToAction("Index", "Home");
                default:
                    return View("Login", model);
            }



        }


        /*---  REGISTRO --- */

        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated) 
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return View(); 
            }
        }

        public async Task<IActionResult> RegisterAuthenticate(RegisterViewModel model) {
            return BadRequest();
        }
    }
}
