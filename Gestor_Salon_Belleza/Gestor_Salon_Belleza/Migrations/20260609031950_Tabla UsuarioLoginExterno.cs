using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gestor_Salon_Belleza.Migrations
{
    /// <inheritdoc />
    public partial class TablaUsuarioLoginExterno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuario_LoginsExternos",
                columns: table => new
                {
                    Id_LoginExterno = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id_Usuario = table.Column<int>(type: "int", nullable: false),
                    Proveedor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario_LoginsExternos", x => x.Id_LoginExterno);
                    table.ForeignKey(
                        name: "FK_Usuario_LoginsExternos_Usuarios_Id_Usuario",
                        column: x => x.Id_Usuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id_Usuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_LoginsExternos_Id_Usuario",
                table: "Usuario_LoginsExternos",
                column: "Id_Usuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Usuario_LoginsExternos");
        }
    }
}
