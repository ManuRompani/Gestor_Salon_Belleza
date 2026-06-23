using Gestor_Salon_Belleza.Data;
using Gestor_Salon_Belleza.Models;
using Gestor_Salon_Belleza.Services.Email;
using Gestor_Salon_Belleza.Utils.Enumerables;
using Gestor_Salon_Belleza.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Gestor_Salon_Belleza.Controllers
{
    public class AccountController : Controller
    {

        private readonly AppDBContext _context;
        private readonly EmailService _emailService;

        public AccountController(AppDBContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }



        /*===== LOGIN =====*/

        [HttpGet]
        public IActionResult Login() {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return View();
            }
        }

        private async Task SignInLocalUserAsync(Usuario usuario, bool recordarme = false)
        {
            // Centralizamos las claims para reutilizar exactamente la misma sesión en login manual y Google.
            // Claims = datos mínimos del usuario que viajan dentro de la cookie autenticada.
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol!.Rol_Name)
            };

            // ClaimsIdentity define el tipo de autenticación y agrupa esas claims.
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            // ClaimsPrincipal es el objeto que ASP.NET guardará en la cookie y luego expondrá como User.
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = recordarme
                });
        }

        [HttpPost]
        public async Task<IActionResult> LoginAuthenticate(LoginViewModel model, string? returnUrl = null)
        {
            // Paso 1:
            // Validamos lo que llegó desde el formulario antes de consultar la base.
            if (!ModelState.IsValid)
            {
                return View("Login", model);
            }
            // Paso 2:
            // Buscamos el usuario por email e incluimos el Rol porque luego esa información
            // se copia a las claims de la cookie local.

            var usuarioBuscado = await _context.Usuarios
                .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == model.Email);

            // Paso 3:
            // Si la cuenta no existe o fue creada por Google sin contraseña local,
            // bloqueamos el login manual.
            if (usuarioBuscado == null || string.IsNullOrWhiteSpace(usuarioBuscado.Password))
            {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas");
                return View("Login", model);
            }

            // Paso 4:
            // La contraseña ingresada se compara contra el hash almacenado.
            var hasher = new PasswordHasher<Usuario>();
            var resultHash = hasher.VerifyHashedPassword(usuarioBuscado, usuarioBuscado.Password, model.Password);

            // Paso 5:
            // Si el hash falla, devolvemos el formulario sin indicar qué dato fue incorrecto.
            if (resultHash == PasswordVerificationResult.Failed) {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas");
                return View("Login", model);
            }

            // Paso 6:
            // Si las credenciales son válidas, firmamos la cookie local del sistema.

            await SignInLocalUserAsync(usuarioBuscado, model.Recordarme);

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);
            return RedirectToAction(nameof(RedirectByRol));
        }

        // EXTERNAL LOGIN
        [HttpPost]
        public IActionResult ExternalLogin(string provider, EnumRoles tipoUsuario, string? returnUrl = null)
        {
            // Recibo el proveedor y el tipo de usuario desde el formulario.

            if (string.IsNullOrWhiteSpace(provider))
            {
                TempData["Error"] = "Proveedor externo inválido.";
                return RedirectToAction(nameof(Login));
            }

            // Valido que solo se pueda iniciar este flujo como Cliente.
            if (tipoUsuario != EnumRoles.Cliente)
            {
                TempData["Error"] = "Tipo de usuario inválido.";
                return RedirectToAction(nameof(Login));
            }

            // Creo la URL interna a la que voy a volver después de Google.
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account");

            if (string.IsNullOrWhiteSpace(redirectUrl))
            {
                TempData["Error"] = "No se pudo generar la URL de retorno.";
                return RedirectToAction(nameof(Login));
            }

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            };

            // Guardo el tipo de usuario para recuperarlo al volver de Google.
            properties.Items["tipoUsuario"] = tipoUsuario.ToString();

            // Guardo la URL de retorno para redirigir después del login con Google.
            if (!string.IsNullOrWhiteSpace(returnUrl))
                properties.Items["returnUrl"] = returnUrl;

            // Inicio el login externo con Google.
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? remoteError = null)
        {
            // Verifico si Google devolvió un error.
            if (!string.IsNullOrWhiteSpace(remoteError))
            {
                TempData["Error"] = "No se pudo iniciar sesión con Google.";
                return RedirectToAction(nameof(Login));
            }

            // Leo la autenticación externa temporal.
            var externalAuth = await HttpContext.AuthenticateAsync("External");

            if (!externalAuth.Succeeded || externalAuth.Principal == null)
            {
                TempData["Error"] = "No se pudo leer la autenticación externa.";
                return RedirectToAction(nameof(Login));
            }

            // Recupero el tipo de usuario guardado antes de ir a Google.
            string? tipoUsuarioString = null;

            if (externalAuth.Properties?.Items.TryGetValue("tipoUsuario", out var value) == true)
            {
                tipoUsuarioString = value;
            }

            // Convierto el tipo de usuario a EnumRoles.
            if (!Enum.TryParse<EnumRoles>(tipoUsuarioString, ignoreCase: true, out var tipoUsuario) ||
                !Enum.IsDefined(typeof(EnumRoles), tipoUsuario))
            {
                await HttpContext.SignOutAsync("External");

                TempData["Error"] = "No se pudo determinar el tipo de usuario.";
                return RedirectToAction(nameof(Login));
            }

            // Valido que este flujo sea solo para Cliente.
            if (tipoUsuario != EnumRoles.Cliente)
            {
                await HttpContext.SignOutAsync("External");

                TempData["Error"] = "Tipo de usuario inválido.";
                return RedirectToAction(nameof(Login));
            }

            ClaimsPrincipal principal = externalAuth.Principal;

            // Obtengo el proveedor externo.
            string? provider = externalAuth.Properties?.Items.TryGetValue(".AuthScheme", out var providerValue) == true
                ? providerValue
                : "Google";

            // Obtengo los datos principales que devuelve Google.
            string? providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            string? email = principal.FindFirstValue(ClaimTypes.Email);

            // Valido los datos mínimos necesarios.
            if (string.IsNullOrWhiteSpace(provider) ||
                string.IsNullOrWhiteSpace(providerKey) ||
                string.IsNullOrWhiteSpace(email))
            {
                await HttpContext.SignOutAsync("External");

                TempData["Error"] = "Google no devolvió los datos mínimos del usuario.";
                return RedirectToAction(nameof(Login));
            }

            // Busco si esta cuenta de Google ya está vinculada.
            UsuarioLoginExterno? loginExterno = await _context.Usuario_LoginsExternos
                .Include(ule => ule.Usuario)
                .ThenInclude(u => u.Rol)
                .FirstOrDefaultAsync(ule =>
                    ule.Proveedor == provider &&
                    ule.ProviderKey == providerKey);

            Usuario usuario;

            if (loginExterno != null)
            {
                // Uso el usuario interno ya vinculado.
                usuario = loginExterno.Usuario;

                if (usuario.Id_Rol != (int)tipoUsuario)
                {
                    await HttpContext.SignOutAsync("External");

                    TempData["Error"] = "La cuenta está vinculada a otro tipo de usuario.";
                    return RedirectToAction(nameof(Login));
                }
            }
            else
            {
                // Busco si ya existe un usuario con ese email.
                var usuarioExistente = await _context.Usuarios
                    .Include(u => u.Rol)
                    .Include(u => u.LoginsExternos)
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (usuarioExistente != null)
                {
                    // Uso el usuario existente y lo voy a vincular con Google.
                    usuario = usuarioExistente;

                    if (usuario.Id_Rol != (int)tipoUsuario)
                    {
                        await HttpContext.SignOutAsync("External");

                        TempData["Error"] = "Ya existe una cuenta con ese email y otro tipo de usuario.";
                        return RedirectToAction(nameof(Login));
                    }
                }
                else
                {
                    // Creo un usuario nuevo con los datos de Google.
                    var nombre = principal.FindFirstValue(ClaimTypes.GivenName)
                        ?? principal.FindFirstValue(ClaimTypes.Name)
                        ?? "Usuario";

                    var apellido = principal.FindFirstValue(ClaimTypes.Surname)
                        ?? string.Empty;

                    usuario = new Usuario
                    {
                        Nombre = nombre,
                        Apellido = string.IsNullOrWhiteSpace(apellido) ? "Google" : apellido,
                        Email = email,
                        Telefono = null,
                        Password = null,
                        Id_Rol = (int)tipoUsuario
                    };

                    _context.Usuarios.Add(usuario);
                    await _context.SaveChangesAsync();

                    // Creo también el registro de Cliente.
                    if (tipoUsuario == EnumRoles.Cliente)
                    {
                        var nuevoCliente = new Cliente
                        {
                            Id_Usuario = usuario.Id_Usuario
                        };

                        _context.Clientes.Add(nuevoCliente);
                        await _context.SaveChangesAsync();
                    }

                    // Recargo el usuario con su rol.
                    usuario = await _context.Usuarios
                        .Include(u => u.Rol)
                        .FirstAsync(u => u.Id_Usuario == usuario.Id_Usuario);
                }

                // Vinculo el usuario interno con la cuenta de Google.
                var nuevoLoginExterno = new UsuarioLoginExterno
                {
                    Id_Usuario = usuario.Id_Usuario,
                    Proveedor = provider,
                    ProviderKey = providerKey
                };

                _context.Usuario_LoginsExternos.Add(nuevoLoginExterno);
                await _context.SaveChangesAsync();
            }

            // Limpio la cookie externa temporal.
            await HttpContext.SignOutAsync("External");

            // Inicio sesión con mi cookie local.
            await SignInLocalUserAsync(usuario);

            // Recupero la URL de retorno guardada antes de ir a Google.
            string? returnUrl = null;
            if (externalAuth.Properties?.Items.TryGetValue("returnUrl", out var ru) == true)
                returnUrl = ru;

            if (!string.IsNullOrWhiteSpace(returnUrl))
                return LocalRedirect(returnUrl);

            // Redirijo según el rol.
            return RedirectToAction(nameof(RedirectByRol));
        }


        /*===== LOGOUT =====*/

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        /*===== PROFILE =====*/

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            // Si el usuario autenticado no es cliente, lo devolvemos a su flujo normal, por ahora
            if (!User.IsInRole("Cliente"))
            {
                return RedirectToAction(nameof(RedirectByRol));
            }

            // El perfil se busca por email porque ese dato ya viaja en la cookie del usuario autenticado.
            var emailUsuario = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(emailUsuario))
            {
                return RedirectToAction(nameof(Login));
            }

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == emailUsuario);

            if (usuario == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Mapeamos la entidad Usuario a un ViewModel para no exponer directamente
            // la entidad de base en la vista y para enviar solo los campos necesarios.
            var model = new ProfileViewModel
            {
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Email,
                Telefono = usuario.Telefono
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (!User.IsInRole("Cliente"))
            {
                return RedirectToAction(nameof(RedirectByRol));
            }

            // Si el cliente empezó a escribir una nueva contraseña,
            // validamos ese subflujo antes de tocar la base.
            ValidarCambioPasswordPerfil(model);

            // El POST vuelve a validar el ViewModel porque nunca debemos confiar
            // solamente en lo que validó el navegador del usuario.
            if (!ModelState.IsValid)
            {
                return View("Profile", model);
            }

            var emailUsuario = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(emailUsuario))
            {
                return RedirectToAction(nameof(Login));
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == emailUsuario);

            if (usuario == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // El formulario trae los datos editables del perfil. El email se deja solo lectura
            // para no desalinear la cuenta local con la sesión actual ni con el login externo.
            usuario.Nombre = model.Nombre;
            usuario.Apellido = model.Apellido;
            usuario.Telefono = model.Telefono;

            // Si el usuario decidió cambiar la contraseña desde perfil,
            // guardamos el hash y nunca la contraseña en texto plano.
            if (!string.IsNullOrWhiteSpace(model.NuevaPassword))
            {
                usuario.Password = HashearPassword(usuario, model.NuevaPassword);
            }

            await _context.SaveChangesAsync();

            // Refirmamos la cookie para que las claims reflejen los datos actualizados del perfil.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await _context.Entry(usuario).Reference(u => u.Rol).LoadAsync();
            await SignInLocalUserAsync(usuario);

            TempData["Exito"] = "Perfil actualizado correctamente.";

            return RedirectToAction(nameof(Profile));
        }


        /*===== REGISTER =====*/

        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return View();
            }
        }

        public async Task<IActionResult> RegisterAuthenticate(RegisterViewModel model) {
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

            // Flujo de alta manual:
            // 1. Guardamos la identidad base en Usuarios.
            // 2. Creamos su extensión en Clientes.
            // 3. El login posterior usará la cookie local con claims de ese Usuario.
            Usuario nuevoUsuario = new Usuario()
            {
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Email = model.Email,
                Telefono = model.Telefono,
                // La contraseña se transforma en hash antes de persistirse.
                Password = string.Empty,
                Id_Rol = (int)EnumRoles.Cliente
            };

            nuevoUsuario.Password = HashearPassword(nuevoUsuario, model.Password);

            _context.Add(nuevoUsuario);
            _context.SaveChanges();

            Cliente nuevoCliente = new Cliente()
            {
                Id_Usuario = nuevoUsuario.Id_Usuario
            };

            // La tabla Clientes representa la especialización de negocio del usuario cliente.
            // Por eso, después de insertar Usuarios, reutilizamos el mismo Id como PK/FK.
            _context.Add(nuevoCliente);
            _context.SaveChanges();

            TempData["Exito"] = "Registro exitoso, podés iniciar sesión"; //alerta de exito


            return RedirectToAction("Login", "Account");
        }


        /*===== REDIRECT =====*/

        public IActionResult RedirectByRol()
        {
            // Esta acción decide a dónde mandar al usuario según la claim Role
            // que fue guardada al iniciar sesión.
            var rol_name = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrWhiteSpace(rol_name))
            {
                return RedirectToAction("Index", "Home");
            }

            switch (rol_name)
            {

                case "Administrador":

                    return RedirectToAction("DashboardAdmin", "Admin");
                case "Cliente":

                    return RedirectToAction("Index", "Home");

                case "Profesional":

                    return RedirectToAction("Index", "Home");
                default:
                    return View("Index", "Home");
            }

        }

        /*===== Recupero de contraseña =====*/
        [HttpGet]
        public IActionResult RecuperarPassword()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecuperarPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Ingresá tu correo electrónico para recuperar la contraseña.";
                return View();
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == email);

            if (usuario == null)
            {
                TempData["Error"] = "No existe una cuenta registrada con ese correo.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(usuario.Password))
            {
                TempData["Error"] = "No es posible recuperar tu contraseña, te has logueado con Google.";
                return View();
            }

            string nuevaPasswordTemporal = GenerarPasswordTemporal();

            // El recupero también persiste hash para mantener el mismo estándar
            // de seguridad que el registro y la edición de perfil.
            usuario.Password = HashearPassword(usuario, nuevaPasswordTemporal);

            await _context.SaveChangesAsync();

            string cuerpoHtml = $"""
                <h2>Recuperación de contraseña</h2>

                <p>Hola {usuario.Nombre},</p>

                <p>Se generó una nueva contraseña temporal para tu cuenta de Glow & Style.</p>

                <p><strong>Nueva contraseña temporal:</strong> {nuevaPasswordTemporal}</p>

                <p>Te recomendamos iniciar sesión y cambiarla lo antes posible.</p>
            """;

            await _emailService.EnviarCorreoAsync(
                destinatario: usuario.Email,
                asunto: "Recuperación de contraseña - Glow & Style",
                cuerpoHtml: cuerpoHtml
            );

            TempData["Exito"] = "Te enviamos una nueva contraseña temporal a tu correo.";

            return RedirectToAction(nameof(Login));
        }
        private static string GenerarPasswordTemporal()
        {
            const string mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string minusculas = "abcdefghijkmnopqrstuvwxyz";
            const string numeros = "23456789";
            const string especiales = "!@$?";

            string caracteres = mayusculas + minusculas + numeros + especiales;

            char[] password = new char[10];

            password[0] = mayusculas[RandomNumberGenerator.GetInt32(mayusculas.Length)];
            password[1] = minusculas[RandomNumberGenerator.GetInt32(minusculas.Length)];
            password[2] = numeros[RandomNumberGenerator.GetInt32(numeros.Length)];
            password[3] = especiales[RandomNumberGenerator.GetInt32(especiales.Length)];

            for (int i = 4; i < password.Length; i++)
            {
                password[i] = caracteres[RandomNumberGenerator.GetInt32(caracteres.Length)];
            }

            return new string(
                password
                    .OrderBy(_ => RandomNumberGenerator.GetInt32(1000))
                    .ToArray()
            );
        }

        private static string HashearPassword(Usuario usuario, string passwordPlano)
        {
            var hasher = new PasswordHasher<Usuario>();
            return hasher.HashPassword(usuario, passwordPlano);
        }

        private void ValidarCambioPasswordPerfil(ProfileViewModel model)
        {
            bool quiereCambiarPassword =
                !string.IsNullOrWhiteSpace(model.NuevaPassword) ||
                !string.IsNullOrWhiteSpace(model.ConfirmarNuevaPassword);

            // Si no tocó los campos de contraseña, dejamos pasar el update normal del perfil.
            if (!quiereCambiarPassword)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(model.NuevaPassword))
            {
                ModelState.AddModelError(nameof(model.NuevaPassword), "Ingresá la nueva contraseña.");
            }
            else if (model.NuevaPassword.Length < 8)
            {
                ModelState.AddModelError(nameof(model.NuevaPassword), "La contraseña debe tener al menos 8 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(model.ConfirmarNuevaPassword))
            {
                ModelState.AddModelError(nameof(model.ConfirmarNuevaPassword), "Confirmá la nueva contraseña.");
            }
            else if (model.NuevaPassword != model.ConfirmarNuevaPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmarNuevaPassword), "Las contraseñas no coinciden.");
            }
        }
    }
}
