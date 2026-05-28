/* A MODO DE ORIENTACION SOBRE LO QUE ESTOY HACIENDO:
 * Por medio de este archivo se configura la conexion a BD
 * se definen tablas y claves compuestas para tablas intermedias.
 * 
 * Yamila, 27/5/26
 */


using Gestor_Salon_Belleza.Models;
using Microsoft.EntityFrameworkCore;

namespace Gestor_Salon_Belleza.Data
{
    public class AppDBContext : DbContext
    {

        //Constructor que recibe opciones de configuracion para BD y se la pasa a DBContext mediante base(options)
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
        {
        }

        // Configuracion de tablas en BD
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Profesional> Profesionales { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Servicio> Servicios { get; set; }
        public DbSet<Turno> Turnos { get; set; }
        public DbSet<Turno_Servicio> Turno_Servicios { get; set; }
        public DbSet<Profesional_Servicio> Profesional_Servicios { get; set; }



        // Configuracion de claves compuestas para tablas intermedias
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //modelBuilder construye el modelo 
            //.Entity<T>() determina sobre que tabla se va a configurar
            //.HasKey() define la clave primaria, en este caso una clave compuesta
            modelBuilder.Entity<Profesional_Servicio>()
                .HasKey(ps => new { ps.Id_Profesional, ps.Id_Servicio });

            modelBuilder.Entity<Turno_Servicio>()
                .HasKey(ts => new { ts.Id_Turno, ts.Id_Servicio });


            modelBuilder.Entity<Turno>() // rel entre turno y cliente
                        .HasOne(t => t.Cliente)  // un turno tiene un cliente
                        .WithMany(c => c.Turnos) // un cliente puede tener muchos turnos
                        .HasForeignKey(t => t.Id_Cliente) // la fk es Id_Cliente en turno
                        .OnDelete(DeleteBehavior.NoAction); // evito eliminacion en cascada

            modelBuilder.Entity<Turno>()
                        .HasOne(t => t.Profesional)
                        .WithMany(c => c.Turnos) 
                        .HasForeignKey(t => t.Id_Profesional) 
                        .OnDelete(DeleteBehavior.NoAction);
        }

        
    }
}
