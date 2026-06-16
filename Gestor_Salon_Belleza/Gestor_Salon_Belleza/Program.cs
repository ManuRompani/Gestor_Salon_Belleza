using Gestor_Salon_Belleza.Data;
using Gestor_Salon_Belleza.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


//Confg conexion a BD
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'. Revisá appsettings.json.");
}

builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseSqlServer(connectionString));


//configuracion de autenticacion con google
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    // Para rutas [Authorize], manda al login manual.
    // Para Google, se usa Challenge explícito desde el AccountController.
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})

//autenticacion por cookie, agregamos el servicio, le decimos qu ees por cookies 
//seteamos un loginpath para proteger rutas authorized de usuarios no logueados
//seteamos accesdeniedpath para que no accedan a rutas donde no tienen permisos
//seteamos expiracion de cookie por inactividad luego de 30 min
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
})

// Cookie temporal para guardar los datos que devuelve Google
// antes de crear/iniciar sesión con el usuario local de tu BD
.AddCookie("External")

.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    var googleClientId = builder.Configuration["GoogleKeys:ClientId"];
    var googleClientSecret = builder.Configuration["GoogleKeys:ClientSecret"];

    if (string.IsNullOrWhiteSpace(googleClientId))
    {
        throw new InvalidOperationException("No se encontró GoogleKeys:ClientId. Revisá appsettings.json.");
    }

    if (string.IsNullOrWhiteSpace(googleClientSecret))
    {
        throw new InvalidOperationException("No se encontró GoogleKeys:ClientSecret. Revisá appsettings.json.");
    }

    options.ClientId = googleClientId;
    options.ClientSecret = googleClientSecret;

    // Google deja los datos en una cookie temporal.
    // Después tu AccountController los lee, crea/busca el Usuario y firma la cookie principal.
    options.SignInScheme = "External";
});


// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();


/*
 * Obtener una instancia del DbContext
 * Verificar si ya existen roles
 * Si no existen, cargar los tres roles: Admin, Profesional, Cliente
 * Guardar los cambios   
 */


// Creo un espacio temporal para conseguir servicios y obtener una instacia de DB 
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDBContext>(); // INSTANCIA DE DB

    //creacion de roles

    if (!context.Roles.Any())
    {
        context.Roles.Add(new Rol { Rol_Name = "Administrador" });
        context.Roles.Add(new Rol { Rol_Name = "Cliente" });
        context.Roles.Add(new Rol { Rol_Name = "Profesional" });
        context.SaveChanges();
    }
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//==== MIDDLEWARE ====
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();