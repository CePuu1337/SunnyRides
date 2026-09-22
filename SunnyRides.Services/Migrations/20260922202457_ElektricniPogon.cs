using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SunnyRides.Services.Migrations
{
    /// <inheritdoc />
    public partial class ElektricniPogon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "JeElektricni",
                table: "TipGoriva",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Postojeci sifrarnik ima zapis za elektricni pogon jos od seeda, pa mu se
            // zastavica postavlja odmah. Bez ovoga bi baza koja vec radi morala biti
            // ispravljena rucno da bi skuteri na struju prestali traziti nivo goriva.
            migrationBuilder.Sql(
                "UPDATE TipGoriva SET JeElektricni = 1 WHERE Naziv IN ('Elektricni', 'Električni');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JeElektricni",
                table: "TipGoriva");
        }
    }
}
