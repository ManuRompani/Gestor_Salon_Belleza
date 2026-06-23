namespace Gestor_Salon_Belleza.ViewModels
{
   
        public class UsuarioAdminVM
        {
            public int Id_Usuario { get; set; }
            public string Nombre { get; set; }
            public string Apellido { get; set; }
            public string Email { get; set; }
            public string Rol { get; set; }

        public bool TieneLoginExt { get; set; }

        public int Id_Rol { get; set; }
    }
}