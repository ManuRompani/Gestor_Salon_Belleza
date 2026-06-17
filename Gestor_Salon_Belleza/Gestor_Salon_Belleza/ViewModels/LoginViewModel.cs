using System.ComponentModel.DataAnnotations;

namespace Gestor_Salon_Belleza.ViewModels
{
    public class LoginViewModel
    {

            [Required(ErrorMessage = "Campo Requerido")]
            [EmailAddress(ErrorMessage = "Email inválido")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Campo Requerido")]
            public string Password { get; set; }

            public bool Recordarme { get; set; } = false;
            
            public string? ReturnUrl { get; set; }
    }
    }

