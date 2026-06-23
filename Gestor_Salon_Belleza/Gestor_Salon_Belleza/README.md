# Gestor Salon Belleza

Aplicación web ASP.NET Core MVC para la gestión de un salón de belleza.

Permite:
- mostrar servicios y especialistas
- registrar clientes
- iniciar sesión manualmente o con Google
- reservar, consultar y cancelar turnos
- administrar profesionales y usuarios
- recuperar contraseña por correo
- gestionar el perfil de cliente y profesional

## Tecnologías

- .NET 8
- ASP.NET Core MVC
- Entity Framework Core
- SQL Server / LocalDB
- Autenticación por cookies
- Login externo con Google
- MailKit para envío de correos

## Roles de usuario

- `Administrador`
  - accede al panel general
  - administra usuarios y profesionales
- `Cliente`
  - puede registrarse
  - reservar turnos
  - ver y editar su perfil
- `Profesional`
  - administra su perfil
  - consulta, cancela y reprograma turnos

## Estructura general

- `Controllers/`
  - lógica HTTP y flujos principales
- `Models/`
  - entidades de dominio y tablas
- `ViewModels/`
  - modelos específicos para formularios y vistas
- `Views/`
  - interfaz MVC Razor
- `Data/AppDBContext.cs`
  - configuración de EF Core y relaciones
- `Services/Email/`
  - envío de correos SMTP
- `wwwroot/`
  - CSS, JS e imágenes

## Requisitos

Antes de levantar el proyecto, necesitás:

- [.NET SDK 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- SQL Server LocalDB o una instancia de SQL Server
- Visual Studio 2022 o VS Code
- una cuenta/configuración SMTP si querés probar recuperación de contraseña
- una app de Google OAuth si querés probar login con Google

## Cómo descargar el proyecto

Podés obtenerlo de dos formas:

### Opción 1. Clonar con Git

```powershell
git clone <URL_DEL_REPOSITORIO>
cd Gestor_Salon_Belleza
```

### Opción 2. Descargar ZIP

1. Descargar el repositorio como `.zip`
2. Descomprimirlo
3. Abrir la carpeta del proyecto en Visual Studio

## Cómo configurar el proyecto

### 1. Restaurar dependencias

```powershell
dotnet restore
```

### 2. Configurar `appsettings.json`

Usá como base el archivo:

- `plantilla_appsettings.json`

Debés completar:

- `ConnectionStrings:DefaultConnection`
- `GoogleKeys:ClientId`
- `GoogleKeys:ClientSecret`
- `SmtpSettings`

Ejemplo mínimo:

```json
{
  "GoogleKeys": {
    "ClientId": "TU_CLIENT_ID",
    "ClientSecret": "TU_CLIENT_SECRET"
  },
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "UseStartTls": true,
    "UserName": "tu-correo@gmail.com",
    "Password": "tu-clave-o-app-password",
    "FromEmail": "tu-correo@gmail.com",
    "FromName": "Glow & Style"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=GestorSalonBelleza;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

Importante:
- no conviene subir secretos reales al repositorio
- lo ideal es usar `appsettings.Development.json` o User Secrets para credenciales locales

### 3. Configurar Google Login

El proyecto usa autenticación con Google y espera este callback:

- `https://localhost:7274/signin-google`

En Google Cloud Console debés:

1. crear una app OAuth 2.0
2. habilitar Google como proveedor
3. agregar la URL de callback anterior en los redirect URIs autorizados

## Base de datos

El proyecto usa Entity Framework Core.

Pasos habituales:

```powershell
add-migration NombreDeLaMigracion
update-database
```

O con CLI:

```powershell
dotnet ef migrations add NombreDeLaMigracion
dotnet ef database update
```

## Seed inicial del sistema

Cuando la aplicación arranca:

- si la tabla `Roles` está vacía, crea:
  - `Administrador`
  - `Cliente`
  - `Profesional`
- si la tabla `Usuarios` está vacía, crea un administrador inicial

Credenciales iniciales del admin semilla:

- Email: `admin@glowstyle.com`
- Password: `Admin123!`

La contraseña se guarda hasheada en base de datos.

## Cómo ejecutar la app

Desde Visual Studio:

1. abrir la solución/proyecto
2. verificar que el proyecto de inicio sea `Gestor_Salon_Belleza`
3. ejecutar con `https`

Desde terminal:

```powershell
dotnet run
```

Luego abrir:

- `https://localhost:7274`

## Flujos principales

### Registro e inicio de sesión

- el cliente puede registrarse manualmente
- también puede iniciar sesión o registrarse con Google
- si una cuenta fue creada con Google y su `Password` es `null`, no puede iniciar sesión manual con contraseña

### Perfil

- el cliente puede editar nombre, apellido, teléfono y contraseña
- el profesional puede editar su perfil y cambiar contraseña

### Recuperación de contraseña

- el usuario recibe una contraseña temporal por correo
- si la cuenta es solo Google, no se permite este flujo

### Turnos

- el cliente elige servicio, profesional, fecha y horario
- el profesional puede ver, cancelar y reprogramar sus turnos

