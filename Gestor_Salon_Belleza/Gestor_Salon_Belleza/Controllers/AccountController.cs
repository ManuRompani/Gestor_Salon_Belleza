using Gestor_Salon_Belleza.Data;
using Gestor_Salon_Belleza.Models;
using Gestor_Salon_Belleza.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

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




    // === LOGIN ===

        [HttpGet] //GET: Porque muestra formulario de login
        public IActionResult Login() {
            if (User.Identity != null && User.Identity.IsAuthenticated) //si identity no es null y el usuario esta autenticado
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return View(); //sino se va al login
            }
        }

        public async Task<IActionResult> Logout() {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
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
            
            var usuarioBuscado = await _context.Usuarios
                .Include( u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == model.Email);

            var hasher = new PasswordHasher<Usuario>();
            var resultHash = hasher.VerifyHashedPassword(null, usuarioBuscado.Password, model.Password);

            //si no existe vuelvo a mostrar login con datos ingresador en el form
            if (usuarioBuscado == null && resultHash == PasswordVerificationResult.Failed) {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas");
                return View("Login", model);
            }

            //=== CASO DE EXITO ===
            int rol_id = usuarioBuscado.Id_Rol; //para usar en switch y derivar correctamente
                                                
            //creo la lista de claims que guarda la identidad usuario activo en la sesion, para no hacer
            //consultas constantemente a bd
            var claims = new List<Claim>(){
                     new Claim(ClaimTypes.Name, usuarioBuscado.Nombre),
                     new Claim(ClaimTypes.Email, usuarioBuscado.Email),
                     new Claim(ClaimTypes.Role, usuarioBuscado.Rol!.Rol_Name)
                    };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme); //agrupo la identidad
            var principal = new ClaimsPrincipal(identity); //representacion del usuario en ASP.NET

            // Firma la cookie con la identidad del usuario, a partir de este momento
            // User.Identity.IsAuthenticated devuelve true y el usuario está logueado
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectByRol();


        }


        // === REGISTRO === 

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

            var hasher = new PasswordHasher<Usuario>();

            /* Validar el modelo - LISTO
             * Verificar que el email no esté ya registrado - LISTO
             * Hashear la contraseña - LISTO
             * Crear el objeto Usuario con los datos del formulario -LISTO
             * Guardarlo en la base de datos -LISTO
             * Redirigir al login -LISTO
             */

            if (!ModelState.IsValid) {
                return View("Register", model);
            }

            var emailBuscado = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (emailBuscado != null) {
                ModelState.AddModelError("Email", "El email ya está registrado");
                return View("Register", model);
            }

            var passwordHasheada = hasher.HashPassword(null, model.Password);

            Usuario nuevoUsuario = new Usuario()
            {
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Email = model.Email,
                Telefono = model.Telefono,
                Password = passwordHasheada,
                Id_Rol = 2
            };

            _context.Add(nuevoUsuario);
            _context.SaveChanges();

            TempData["Exito"] = "Registro exitoso, podés iniciar sesión"; //alerta de exito


            return RedirectToAction("Login", "Account");
        }

        // === REDIRECT ===

        public IActionResult RedirectByRol()
        {
            var rol_name = User.FindFirst(ClaimTypes.Role).Value;

            switch (rol_name)
            {

                case "Administrador":
                    
                    return RedirectToAction("Index", "Home");
                case "Cliente":
                    
                    return RedirectToAction("Index", "Home");
                case "Profesional":
                    
                    return RedirectToAction("Index", "Home");
                default:
                    return View("Index", "Home");
            }

        }


}  
}
