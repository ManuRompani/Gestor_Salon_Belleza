using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gestor_Salon_Belleza.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id_Rol = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Rol_Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id_Rol);
                });

            migrationBuilder.CreateTable(
                name: "Servicios",
                columns: table => new
                {
                    Id_Servicio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Duracion_Minutos = table.Column<int>(type: "int", nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicios", x => x.Id_Servicio);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id_Usuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    Id_Rol = table.Column<int>(type: "int", nullable: false),
                    RolId_Rol = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id_Usuario);
                    table.ForeignKey(
                        name: "FK_Usuarios_Roles_RolId_Rol",
                        column: x => x.RolId_Rol,
                        principalTable: "Roles",
                        principalColumn: "Id_Rol",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id_Usuario = table.Column<int>(type: "int", nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id_Usuario);
                    table.ForeignKey(
                        name: "FK_Clientes_Usuarios_Id_Usuario",
                        column: x => x.Id_Usuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id_Usuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profesionales",
                columns: table => new
                {
                    Id_Usuario = table.Column<int>(type: "int", nullable: false),
                    UrlImagen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profesionales", x => x.Id_Usuario);
                    table.ForeignKey(
                        name: "FK_Profesionales_Usuarios_Id_Usuario",
                        column: x => x.Id_Usuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id_Usuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profesional_Servicios",
                columns: table => new
                {
                    Id_Profesional = table.Column<int>(type: "int", nullable: false),
                    Id_Servicio = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profesional_Servicios", x => new { x.Id_Profesional, x.Id_Servicio });
                    table.ForeignKey(
                        name: "FK_Profesional_Servicios_Profesionales_Id_Profesional",
                        column: x => x.Id_Profesional,
                        principalTable: "Profesionales",
                        principalColumn: "Id_Usuario",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Profesional_Servicios_Servicios_Id_Servicio",
                        column: x => x.Id_Servicio,
                        principalTable: "Servicios",
                        principalColumn: "Id_Servicio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Turnos",
                columns: table => new
                {
                    Id_Turno = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Cliente = table.Column<int>(type: "int", nullable: false),
                    Id_Profesional = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCancelacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServicioId_Servicio = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turnos", x => x.Id_Turno);
                    table.ForeignKey(
                        name: "FK_Turnos_Clientes_Id_Cliente",
                        column: x => x.Id_Cliente,
                        principalTable: "Clientes",
                        principalColumn: "Id_Usuario");
                    table.ForeignKey(
                        name: "FK_Turnos_Profesionales_Id_Profesional",
                        column: x => x.Id_Profesional,
                        principalTable: "Profesionales",
                        principalColumn: "Id_Usuario");
                    table.ForeignKey(
                        name: "FK_Turnos_Servicios_ServicioId_Servicio",
                        column: x => x.ServicioId_Servicio,
                        principalTable: "Servicios",
                        principalColumn: "Id_Servicio");
                });

            migrationBuilder.CreateTable(
                name: "Turno_Servicios",
                columns: table => new
                {
                    Id_Turno = table.Column<int>(type: "int", nullable: false),
                    Id_Servicio = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turno_Servicios", x => new { x.Id_Turno, x.Id_Servicio });
                    table.ForeignKey(
                        name: "FK_Turno_Servicios_Servicios_Id_Servicio",
                        column: x => x.Id_Servicio,
                        principalTable: "Servicios",
                        principalColumn: "Id_Servicio",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Turno_Servicios_Turnos_Id_Turno",
                        column: x => x.Id_Turno,
                        principalTable: "Turnos",
                        principalColumn: "Id_Turno",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Profesional_Servicios_Id_Servicio",
                table: "Profesional_Servicios",
                column: "Id_Servicio");

            migrationBuilder.CreateIndex(
                name: "IX_Turno_Servicios_Id_Servicio",
                table: "Turno_Servicios",
                column: "Id_Servicio");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_Id_Cliente",
                table: "Turnos",
                column: "Id_Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_Id_Profesional",
                table: "Turnos",
                column: "Id_Profesional");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_ServicioId_Servicio",
                table: "Turnos",
                column: "ServicioId_Servicio");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_RolId_Rol",
                table: "Usuarios",
                column: "RolId_Rol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Profesional_Servicios");

            migrationBuilder.DropTable(
                name: "Turno_Servicios");

            migrationBuilder.DropTable(
                name: "Turnos");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "Profesionales");

            migrationBuilder.DropTable(
                name: "Servicios");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
