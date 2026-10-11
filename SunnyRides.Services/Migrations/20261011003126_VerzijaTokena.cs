using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SunnyRides.Services.Migrations
{
    /// <inheritdoc />
    public partial class VerzijaTokena : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VerzijaTokena",
                table: "Korisnik",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerzijaTokena",
                table: "Korisnik");
        }
    }
}
