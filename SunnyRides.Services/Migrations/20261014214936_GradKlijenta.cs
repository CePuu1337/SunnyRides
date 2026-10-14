using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SunnyRides.Services.Migrations
{
    /// <inheritdoc />
    public partial class GradKlijenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GradId",
                table: "Korisnik",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latituda",
                table: "Grad",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longituda",
                table: "Grad",
                type: "float",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Korisnik_GradId",
                table: "Korisnik",
                column: "GradId");

            migrationBuilder.AddForeignKey(
                name: "FK_Korisnik_Grad_GradId",
                table: "Korisnik",
                column: "GradId",
                principalTable: "Grad",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Korisnik_Grad_GradId",
                table: "Korisnik");

            migrationBuilder.DropIndex(
                name: "IX_Korisnik_GradId",
                table: "Korisnik");

            migrationBuilder.DropColumn(
                name: "GradId",
                table: "Korisnik");

            migrationBuilder.DropColumn(
                name: "Latituda",
                table: "Grad");

            migrationBuilder.DropColumn(
                name: "Longituda",
                table: "Grad");
        }
    }
}
