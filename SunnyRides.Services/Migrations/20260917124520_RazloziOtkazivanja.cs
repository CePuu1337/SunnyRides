using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SunnyRides.Services.Migrations
{
    /// <inheritdoc />
    public partial class RazloziOtkazivanja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RazlogOtkazivanja",
                table: "Rezervacija",
                newName: "NapomenaOtkazivanja");

            migrationBuilder.AddColumn<int>(
                name: "RazlogOtkazivanjaId",
                table: "Rezervacija",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RazlogOtkazivanja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Naziv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ZaKlijenta = table.Column<bool>(type: "bit", nullable: false),
                    ZaAgenciju = table.Column<bool>(type: "bit", nullable: false),
                    TraziNapomenu = table.Column<bool>(type: "bit", nullable: false),
                    Aktivan = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RazlogOtkazivanja", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rezervacija_RazlogOtkazivanjaId",
                table: "Rezervacija",
                column: "RazlogOtkazivanjaId");

            migrationBuilder.CreateIndex(
                name: "IX_RazlogOtkazivanja_Naziv",
                table: "RazlogOtkazivanja",
                column: "Naziv",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Rezervacija_RazlogOtkazivanja_RazlogOtkazivanjaId",
                table: "Rezervacija",
                column: "RazlogOtkazivanjaId",
                principalTable: "RazlogOtkazivanja",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rezervacija_RazlogOtkazivanja_RazlogOtkazivanjaId",
                table: "Rezervacija");

            migrationBuilder.DropTable(
                name: "RazlogOtkazivanja");

            migrationBuilder.DropIndex(
                name: "IX_Rezervacija_RazlogOtkazivanjaId",
                table: "Rezervacija");

            migrationBuilder.DropColumn(
                name: "RazlogOtkazivanjaId",
                table: "Rezervacija");

            migrationBuilder.RenameColumn(
                name: "NapomenaOtkazivanja",
                table: "Rezervacija",
                newName: "RazlogOtkazivanja");
        }
    }
}
