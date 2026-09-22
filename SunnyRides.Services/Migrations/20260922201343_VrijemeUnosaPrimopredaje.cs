using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SunnyRides.Services.Migrations
{
    /// <inheritdoc />
    public partial class VrijemeUnosaPrimopredaje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DatumUnosa",
                table: "Primopredaja",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Postojeci zapisi su uneseni u trenutku kad se primopredaja i desila, pa
            // im vrijeme unosa jednako vremenu dogadjaja. Bez ovoga bi im ostala
            // podrazumijevana vrijednost i svi bi izgledali kao naknadni unos.
            migrationBuilder.Sql("UPDATE Primopredaja SET DatumUnosa = DatumVrijeme;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DatumUnosa",
                table: "Primopredaja");
        }
    }
}
