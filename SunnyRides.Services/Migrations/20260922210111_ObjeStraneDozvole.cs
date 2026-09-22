using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SunnyRides.Services.Migrations
{
    /// <inheritdoc />
    public partial class ObjeStraneDozvole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF je postojecu kolonu preimenovao u zadnju stranu, jer imena poredi
            // abecedno i ne zna sta je sta. Fotografija koja vec postoji je prednja
            // strana - to je ono sto se dosad trazilo od klijenta.
            migrationBuilder.RenameColumn(
                name: "PutanjaSlike",
                table: "VozackaDozvola",
                newName: "PutanjaSlikePrednja");

            migrationBuilder.AddColumn<string>(
                name: "PutanjaSlikeZadnja",
                table: "VozackaDozvola",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // Postojece dozvole imaju samo jednu fotografiju, a od sada se traze dvije.
            // Obje strane se vezu za isti fajl, da vec odobrene dozvole ne postanu
            // nepotpune preko noci. Servis pri zamjeni jedne strane provjerava pokazuje
            // li na isti fajl i druga, pa se slika ne moze obrisati ispod nje.
            migrationBuilder.Sql(
                "UPDATE VozackaDozvola SET PutanjaSlikeZadnja = PutanjaSlikePrednja "
                + "WHERE PutanjaSlikePrednja IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PutanjaSlikeZadnja",
                table: "VozackaDozvola");

            migrationBuilder.RenameColumn(
                name: "PutanjaSlikePrednja",
                table: "VozackaDozvola",
                newName: "PutanjaSlike");
        }
    }
}
