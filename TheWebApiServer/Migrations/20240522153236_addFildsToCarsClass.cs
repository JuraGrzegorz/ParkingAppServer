using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheWebApiServer.Migrations
{
    /// <inheritdoc />
    public partial class addFildsToCarsClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CarBrand",
                table: "cars",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CarModel",
                table: "cars",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CarBrand",
                table: "cars");

            migrationBuilder.DropColumn(
                name: "CarModel",
                table: "cars");
        }
    }
}
