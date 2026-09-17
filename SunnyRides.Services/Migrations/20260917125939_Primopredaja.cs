using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SunnyRides.Services.Migrations
{
    /// <inheritdoc />
    public partial class Primopredaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PutanjaThumbnail",
                table: "FotografijaPrimopredaje");

            migrationBuilder.AddColumn<bool>(
                name: "KontrolnaListaProdjena",
                table: "Primopredaja",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KontrolnaListaProdjena",
                table: "Primopredaja");

            migrationBuilder.AddColumn<string>(
                name: "PutanjaThumbnail",
                table: "FotografijaPrimopredaje",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
