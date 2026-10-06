using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SunnyRides.Services.Migrations
{
    /// <inheritdoc />
    public partial class OtkazaneRezervacijeVanJedinstvenosti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rezervacija_KorisnikId_VoziloId_DatumOd",
                table: "Rezervacija");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervacija_KorisnikId_VoziloId_DatumOd",
                table: "Rezervacija",
                columns: new[] { "KorisnikId", "VoziloId", "DatumOd" },
                unique: true,
                filter: "[Status] <> 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rezervacija_KorisnikId_VoziloId_DatumOd",
                table: "Rezervacija");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervacija_KorisnikId_VoziloId_DatumOd",
                table: "Rezervacija",
                columns: new[] { "KorisnikId", "VoziloId", "DatumOd" },
                unique: true);
        }
    }
}
