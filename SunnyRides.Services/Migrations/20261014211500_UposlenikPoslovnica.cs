using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SunnyRides.Services.Migrations
{
    /// <inheritdoc />
    public partial class UposlenikPoslovnica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PoslovnicaId",
                table: "Korisnik",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Korisnik_PoslovnicaId",
                table: "Korisnik",
                column: "PoslovnicaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Korisnik_Poslovnica_PoslovnicaId",
                table: "Korisnik",
                column: "PoslovnicaId",
                principalTable: "Poslovnica",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Korisnik_Poslovnica_PoslovnicaId",
                table: "Korisnik");

            migrationBuilder.DropIndex(
                name: "IX_Korisnik_PoslovnicaId",
                table: "Korisnik");

            migrationBuilder.DropColumn(
                name: "PoslovnicaId",
                table: "Korisnik");
        }
    }
}
