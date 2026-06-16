using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gestor_Salon_Belleza.Migrations
{
    /// <inheritdoc />
    public partial class AddActivoToServicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Servicios",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Servicios");
        }
    }
}
