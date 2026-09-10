using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SunnyRides.Services.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Drzava",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Naziv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Skracenica = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drzava", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KategorijaDozvole",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Oznaka = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Opis = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KategorijaDozvole", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Korisnik",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnickoIme = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ime = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Prezime = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telefon = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DatumRodjenja = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LozinkaHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PutanjaSlike = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Aktivan = table.Column<bool>(type: "bit", nullable: false),
                    Blokiran = table.Column<bool>(type: "bit", nullable: false),
                    DatumRegistracije = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Korisnik", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Marka",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Naziv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marka", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Obavijest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Naslov = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tekst = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PutanjaSlike = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DatumObjave = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Aktivna = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Obavijest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObradjeniWebhookEvent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderEventId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TipEventa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DatumObrade = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObradjeniWebhookEvent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpozvaniToken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Jti = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DatumIsteka = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DatumOpoziva = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpozvaniToken", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaketOsiguranja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Naziv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CijenaPoDanu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IznosUcesca = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaketOsiguranja", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Naziv = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Opis = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TipGoriva",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Naziv = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipGoriva", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TipVozila",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Naziv = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipVozila", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VrstaOpreme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Naziv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CijenaPoDanu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    FiksnaCijena = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VrstaOpreme", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Grad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DrzavaId = table.Column<int>(type: "int", nullable: false),
                    Naziv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostanskiBroj = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grad_Drzava_DrzavaId",
                        column: x => x.DrzavaId,
                        principalTable: "Drzava",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KodZaResetLozinke",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    KodHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DatumIsteka = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Iskoristen = table.Column<bool>(type: "bit", nullable: false),
                    DatumKreiranja = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KodZaResetLozinke", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KodZaResetLozinke_Korisnik_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VozackaDozvola",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    BrojDozvole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DatumIzdavanja = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DatumIsteka = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PutanjaSlike = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RazlogOdbijanja = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VerifikovaoKorisnikId = table.Column<int>(type: "int", nullable: true),
                    DatumVerifikacije = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DatumKreiranja = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VozackaDozvola", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VozackaDozvola_Korisnik_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VozackaDozvola_Korisnik_VerifikovaoKorisnikId",
                        column: x => x.VerifikovaoKorisnikId,
                        principalTable: "Korisnik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KorisnikRole",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    DatumDodjele = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KorisnikRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KorisnikRole_Korisnik_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KorisnikRole_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModelVozila",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarkaId = table.Column<int>(type: "int", nullable: false),
                    TipVozilaId = table.Column<int>(type: "int", nullable: false),
                    TipGorivaId = table.Column<int>(type: "int", nullable: false),
                    KategorijaDozvoleId = table.Column<int>(type: "int", nullable: false),
                    Naziv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kubikaza = table.Column<int>(type: "int", nullable: false),
                    SnagaKw = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelVozila", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelVozila_KategorijaDozvole_KategorijaDozvoleId",
                        column: x => x.KategorijaDozvoleId,
                        principalTable: "KategorijaDozvole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModelVozila_Marka_MarkaId",
                        column: x => x.MarkaId,
                        principalTable: "Marka",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModelVozila_TipGoriva_TipGorivaId",
                        column: x => x.TipGorivaId,
                        principalTable: "TipGoriva",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModelVozila_TipVozila_TipVozilaId",
                        column: x => x.TipVozilaId,
                        principalTable: "TipVozila",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PravilaKategorije",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KategorijaDozvoleId = table.Column<int>(type: "int", nullable: false),
                    TipVozilaId = table.Column<int>(type: "int", nullable: false),
                    MaxKubikaza = table.Column<int>(type: "int", nullable: true),
                    MaxSnagaKw = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    MinGodine = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PravilaKategorije", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PravilaKategorije_KategorijaDozvole_KategorijaDozvoleId",
                        column: x => x.KategorijaDozvoleId,
                        principalTable: "KategorijaDozvole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PravilaKategorije_TipVozila_TipVozilaId",
                        column: x => x.TipVozilaId,
                        principalTable: "TipVozila",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistorijaPretrage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    TipVozilaId = table.Column<int>(type: "int", nullable: true),
                    GradId = table.Column<int>(type: "int", nullable: true),
                    MarkaId = table.Column<int>(type: "int", nullable: true),
                    CijenaOd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CijenaDo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DatumVrijeme = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorijaPretrage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorijaPretrage_Grad_GradId",
                        column: x => x.GradId,
                        principalTable: "Grad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorijaPretrage_Korisnik_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistorijaPretrage_Marka_MarkaId",
                        column: x => x.MarkaId,
                        principalTable: "Marka",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorijaPretrage_TipVozila_TipVozilaId",
                        column: x => x.TipVozilaId,
                        principalTable: "TipVozila",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Poslovnica",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GradId = table.Column<int>(type: "int", nullable: false),
                    Naziv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Adresa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Latituda = table.Column<double>(type: "float", nullable: true),
                    Longituda = table.Column<double>(type: "float", nullable: true),
                    RadnoVrijeme = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Poslovnica", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Poslovnica_Grad_GradId",
                        column: x => x.GradId,
                        principalTable: "Grad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DozvolaKategorija",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VozackaDozvolaId = table.Column<int>(type: "int", nullable: false),
                    KategorijaDozvoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DozvolaKategorija", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DozvolaKategorija_KategorijaDozvole_KategorijaDozvoleId",
                        column: x => x.KategorijaDozvoleId,
                        principalTable: "KategorijaDozvole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DozvolaKategorija_VozackaDozvola_VozackaDozvolaId",
                        column: x => x.VozackaDozvolaId,
                        principalTable: "VozackaDozvola",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cjenovnik",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelVozilaId = table.Column<int>(type: "int", nullable: false),
                    Naziv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DatumOd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DatumDo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Mnozilac = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    SatnaTarifa = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DnevnaTarifa = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PopustPrag1 = table.Column<int>(type: "int", nullable: false),
                    PopustProcenat1 = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PopustPrag2 = table.Column<int>(type: "int", nullable: false),
                    PopustProcenat2 = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cjenovnik", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cjenovnik_ModelVozila_ModelVozilaId",
                        column: x => x.ModelVozilaId,
                        principalTable: "ModelVozila",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StanjeOpreme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VrstaOpremeId = table.Column<int>(type: "int", nullable: false),
                    PoslovnicaId = table.Column<int>(type: "int", nullable: false),
                    Kolicina = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StanjeOpreme", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StanjeOpreme_Poslovnica_PoslovnicaId",
                        column: x => x.PoslovnicaId,
                        principalTable: "Poslovnica",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StanjeOpreme_VrstaOpreme_VrstaOpremeId",
                        column: x => x.VrstaOpremeId,
                        principalTable: "VrstaOpreme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vozilo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelVozilaId = table.Column<int>(type: "int", nullable: false),
                    PoslovnicaId = table.Column<int>(type: "int", nullable: false),
                    RegistarskaOznaka = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GodinaProizvodnje = table.Column<int>(type: "int", nullable: false),
                    Kilometraza = table.Column<int>(type: "int", nullable: false),
                    Aktivno = table.Column<bool>(type: "bit", nullable: false),
                    DnevnaTarifa = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SatnaTarifa = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IznosDepozita = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DatumKreiranja = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vozilo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vozilo_ModelVozila_ModelVozilaId",
                        column: x => x.ModelVozilaId,
                        principalTable: "ModelVozila",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vozilo_Poslovnica_PoslovnicaId",
                        column: x => x.PoslovnicaId,
                        principalTable: "Poslovnica",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BlokadaVozila",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoziloId = table.Column<int>(type: "int", nullable: false),
                    DatumOd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DatumDo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Razlog = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    KreiraoKorisnikId = table.Column<int>(type: "int", nullable: false),
                    DatumKreiranja = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlokadaVozila", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlokadaVozila_Korisnik_KreiraoKorisnikId",
                        column: x => x.KreiraoKorisnikId,
                        principalTable: "Korisnik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BlokadaVozila_Vozilo_VoziloId",
                        column: x => x.VoziloId,
                        principalTable: "Vozilo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rezervacija",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Broj = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    VoziloId = table.Column<int>(type: "int", nullable: false),
                    PoslovnicaId = table.Column<int>(type: "int", nullable: false),
                    PaketOsiguranjaId = table.Column<int>(type: "int", nullable: true),
                    DatumOd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DatumDo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UkupanIznos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IznosDepozita = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IznosPopusta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    DrziDo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DatumKreiranja = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RazlogOtkazivanja = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OtkazaoKorisnikId = table.Column<int>(type: "int", nullable: true),
                    DatumOtkazivanja = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rezervacija", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rezervacija_Korisnik_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rezervacija_Korisnik_OtkazaoKorisnikId",
                        column: x => x.OtkazaoKorisnikId,
                        principalTable: "Korisnik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rezervacija_PaketOsiguranja_PaketOsiguranjaId",
                        column: x => x.PaketOsiguranjaId,
                        principalTable: "PaketOsiguranja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rezervacija_Poslovnica_PoslovnicaId",
                        column: x => x.PoslovnicaId,
                        principalTable: "Poslovnica",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rezervacija_Vozilo_VoziloId",
                        column: x => x.VoziloId,
                        principalTable: "Vozilo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SlikaVozila",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoziloId = table.Column<int>(type: "int", nullable: false),
                    Putanja = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PutanjaThumbnail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Redoslijed = table.Column<int>(type: "int", nullable: false),
                    JeGlavna = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlikaVozila", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlikaVozila_Vozilo_VoziloId",
                        column: x => x.VoziloId,
                        principalTable: "Vozilo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistorijaStatusaRezervacije",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RezervacijaId = table.Column<int>(type: "int", nullable: false),
                    StatusIz = table.Column<int>(type: "int", nullable: true),
                    StatusU = table.Column<int>(type: "int", nullable: false),
                    Razlog = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Opis = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IzvrsioKorisnikId = table.Column<int>(type: "int", nullable: true),
                    DatumVrijeme = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorijaStatusaRezervacije", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorijaStatusaRezervacije_Korisnik_IzvrsioKorisnikId",
                        column: x => x.IzvrsioKorisnikId,
                        principalTable: "Korisnik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorijaStatusaRezervacije_Rezervacija_RezervacijaId",
                        column: x => x.RezervacijaId,
                        principalTable: "Rezervacija",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifikacija",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    RezervacijaId = table.Column<int>(type: "int", nullable: true),
                    Naslov = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tekst = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Tip = table.Column<int>(type: "int", nullable: false),
                    Procitana = table.Column<bool>(type: "bit", nullable: false),
                    DatumKreiranja = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifikacija", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifikacija_Korisnik_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifikacija_Rezervacija_RezervacijaId",
                        column: x => x.RezervacijaId,
                        principalTable: "Rezervacija",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Placanje",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RezervacijaId = table.Column<int>(type: "int", nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Valuta = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderPaymentIntentId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NaplaceniIznos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DatumKreiranja = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DatumAzuriranja = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Placanje", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Placanje_Rezervacija_RezervacijaId",
                        column: x => x.RezervacijaId,
                        principalTable: "Rezervacija",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Primopredaja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RezervacijaId = table.Column<int>(type: "int", nullable: false),
                    Tip = table.Column<int>(type: "int", nullable: false),
                    DatumVrijeme = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Kilometraza = table.Column<int>(type: "int", nullable: false),
                    NivoGoriva = table.Column<int>(type: "int", nullable: false),
                    Napomena = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IzvrsioKorisnikId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Primopredaja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Primopredaja_Korisnik_IzvrsioKorisnikId",
                        column: x => x.IzvrsioKorisnikId,
                        principalTable: "Korisnik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Primopredaja_Rezervacija_RezervacijaId",
                        column: x => x.RezervacijaId,
                        principalTable: "Rezervacija",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recenzija",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    VoziloId = table.Column<int>(type: "int", nullable: false),
                    RezervacijaId = table.Column<int>(type: "int", nullable: false),
                    Ocjena = table.Column<int>(type: "int", nullable: false),
                    Komentar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DatumKreiranja = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Skrivena = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recenzija", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recenzija_Korisnik_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recenzija_Rezervacija_RezervacijaId",
                        column: x => x.RezervacijaId,
                        principalTable: "Rezervacija",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recenzija_Vozilo_VoziloId",
                        column: x => x.VoziloId,
                        principalTable: "Vozilo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StavkaOpreme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RezervacijaId = table.Column<int>(type: "int", nullable: false),
                    VrstaOpremeId = table.Column<int>(type: "int", nullable: false),
                    Kolicina = table.Column<int>(type: "int", nullable: false),
                    CijenaPoJedinici = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StavkaOpreme", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StavkaOpreme_Rezervacija_RezervacijaId",
                        column: x => x.RezervacijaId,
                        principalTable: "Rezervacija",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StavkaOpreme_VrstaOpreme_VrstaOpremeId",
                        column: x => x.VrstaOpremeId,
                        principalTable: "VrstaOpreme",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Refund",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlacanjeId = table.Column<int>(type: "int", nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Razlog = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProviderRefundId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    KreiraoKorisnikId = table.Column<int>(type: "int", nullable: true),
                    DatumKreiranja = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refund", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Refund_Korisnik_KreiraoKorisnikId",
                        column: x => x.KreiraoKorisnikId,
                        principalTable: "Korisnik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Refund_Placanje_PlacanjeId",
                        column: x => x.PlacanjeId,
                        principalTable: "Placanje",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvidencijaStete",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrimopredajaId = table.Column<int>(type: "int", nullable: false),
                    Opis = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DatumEvidentiranja = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EvidentiraoKorisnikId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidencijaStete", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvidencijaStete_Korisnik_EvidentiraoKorisnikId",
                        column: x => x.EvidentiraoKorisnikId,
                        principalTable: "Korisnik",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvidencijaStete_Primopredaja_PrimopredajaId",
                        column: x => x.PrimopredajaId,
                        principalTable: "Primopredaja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FotografijaPrimopredaje",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrimopredajaId = table.Column<int>(type: "int", nullable: false),
                    Putanja = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PutanjaThumbnail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FotografijaPrimopredaje", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FotografijaPrimopredaje_Primopredaja_PrimopredajaId",
                        column: x => x.PrimopredajaId,
                        principalTable: "Primopredaja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlokadaVozila_KreiraoKorisnikId",
                table: "BlokadaVozila",
                column: "KreiraoKorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_BlokadaVozila_VoziloId_DatumOd_DatumDo",
                table: "BlokadaVozila",
                columns: new[] { "VoziloId", "DatumOd", "DatumDo" });

            migrationBuilder.CreateIndex(
                name: "IX_Cjenovnik_ModelVozilaId_DatumOd_DatumDo",
                table: "Cjenovnik",
                columns: new[] { "ModelVozilaId", "DatumOd", "DatumDo" });

            migrationBuilder.CreateIndex(
                name: "IX_DozvolaKategorija_KategorijaDozvoleId",
                table: "DozvolaKategorija",
                column: "KategorijaDozvoleId");

            migrationBuilder.CreateIndex(
                name: "IX_DozvolaKategorija_VozackaDozvolaId_KategorijaDozvoleId",
                table: "DozvolaKategorija",
                columns: new[] { "VozackaDozvolaId", "KategorijaDozvoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drzava_Naziv",
                table: "Drzava",
                column: "Naziv",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvidencijaStete_EvidentiraoKorisnikId",
                table: "EvidencijaStete",
                column: "EvidentiraoKorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidencijaStete_PrimopredajaId",
                table: "EvidencijaStete",
                column: "PrimopredajaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FotografijaPrimopredaje_PrimopredajaId",
                table: "FotografijaPrimopredaje",
                column: "PrimopredajaId");

            migrationBuilder.CreateIndex(
                name: "IX_Grad_DrzavaId_Naziv",
                table: "Grad",
                columns: new[] { "DrzavaId", "Naziv" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorijaPretrage_GradId",
                table: "HistorijaPretrage",
                column: "GradId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorijaPretrage_KorisnikId_DatumVrijeme",
                table: "HistorijaPretrage",
                columns: new[] { "KorisnikId", "DatumVrijeme" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorijaPretrage_MarkaId",
                table: "HistorijaPretrage",
                column: "MarkaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorijaPretrage_TipVozilaId",
                table: "HistorijaPretrage",
                column: "TipVozilaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorijaStatusaRezervacije_IzvrsioKorisnikId",
                table: "HistorijaStatusaRezervacije",
                column: "IzvrsioKorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorijaStatusaRezervacije_RezervacijaId_DatumVrijeme",
                table: "HistorijaStatusaRezervacije",
                columns: new[] { "RezervacijaId", "DatumVrijeme" });

            migrationBuilder.CreateIndex(
                name: "IX_KategorijaDozvole_Oznaka",
                table: "KategorijaDozvole",
                column: "Oznaka",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KodZaResetLozinke_KorisnikId_Iskoristen",
                table: "KodZaResetLozinke",
                columns: new[] { "KorisnikId", "Iskoristen" });

            migrationBuilder.CreateIndex(
                name: "IX_Korisnik_Email",
                table: "Korisnik",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Korisnik_KorisnickoIme",
                table: "Korisnik",
                column: "KorisnickoIme",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KorisnikRole_KorisnikId_RoleId",
                table: "KorisnikRole",
                columns: new[] { "KorisnikId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KorisnikRole_RoleId",
                table: "KorisnikRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Marka_Naziv",
                table: "Marka",
                column: "Naziv",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelVozila_KategorijaDozvoleId",
                table: "ModelVozila",
                column: "KategorijaDozvoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelVozila_MarkaId_Naziv",
                table: "ModelVozila",
                columns: new[] { "MarkaId", "Naziv" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelVozila_TipGorivaId",
                table: "ModelVozila",
                column: "TipGorivaId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelVozila_TipVozilaId",
                table: "ModelVozila",
                column: "TipVozilaId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifikacija_KorisnikId_Procitana_DatumKreiranja",
                table: "Notifikacija",
                columns: new[] { "KorisnikId", "Procitana", "DatumKreiranja" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifikacija_RezervacijaId",
                table: "Notifikacija",
                column: "RezervacijaId");

            migrationBuilder.CreateIndex(
                name: "IX_Obavijest_Aktivna_DatumObjave",
                table: "Obavijest",
                columns: new[] { "Aktivna", "DatumObjave" });

            migrationBuilder.CreateIndex(
                name: "IX_ObradjeniWebhookEvent_ProviderEventId",
                table: "ObradjeniWebhookEvent",
                column: "ProviderEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpozvaniToken_DatumIsteka",
                table: "OpozvaniToken",
                column: "DatumIsteka");

            migrationBuilder.CreateIndex(
                name: "IX_OpozvaniToken_Jti",
                table: "OpozvaniToken",
                column: "Jti",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaketOsiguranja_Naziv",
                table: "PaketOsiguranja",
                column: "Naziv",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Placanje_ProviderPaymentIntentId",
                table: "Placanje",
                column: "ProviderPaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_Placanje_RezervacijaId",
                table: "Placanje",
                column: "RezervacijaId",
                unique: true,
                filter: "[Status] = 3");

            migrationBuilder.CreateIndex(
                name: "IX_Poslovnica_GradId",
                table: "Poslovnica",
                column: "GradId");

            migrationBuilder.CreateIndex(
                name: "IX_Poslovnica_Naziv",
                table: "Poslovnica",
                column: "Naziv",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PravilaKategorije_KategorijaDozvoleId_TipVozilaId",
                table: "PravilaKategorije",
                columns: new[] { "KategorijaDozvoleId", "TipVozilaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PravilaKategorije_TipVozilaId",
                table: "PravilaKategorije",
                column: "TipVozilaId");

            migrationBuilder.CreateIndex(
                name: "IX_Primopredaja_IzvrsioKorisnikId",
                table: "Primopredaja",
                column: "IzvrsioKorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_Primopredaja_RezervacijaId_Tip",
                table: "Primopredaja",
                columns: new[] { "RezervacijaId", "Tip" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recenzija_KorisnikId_RezervacijaId",
                table: "Recenzija",
                columns: new[] { "KorisnikId", "RezervacijaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recenzija_RezervacijaId",
                table: "Recenzija",
                column: "RezervacijaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recenzija_VoziloId_Skrivena",
                table: "Recenzija",
                columns: new[] { "VoziloId", "Skrivena" });

            migrationBuilder.CreateIndex(
                name: "IX_Refund_KreiraoKorisnikId",
                table: "Refund",
                column: "KreiraoKorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_Refund_PlacanjeId",
                table: "Refund",
                column: "PlacanjeId");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervacija_Broj",
                table: "Rezervacija",
                column: "Broj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rezervacija_KorisnikId_VoziloId_DatumOd",
                table: "Rezervacija",
                columns: new[] { "KorisnikId", "VoziloId", "DatumOd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rezervacija_OtkazaoKorisnikId",
                table: "Rezervacija",
                column: "OtkazaoKorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervacija_PaketOsiguranjaId",
                table: "Rezervacija",
                column: "PaketOsiguranjaId");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervacija_PoslovnicaId",
                table: "Rezervacija",
                column: "PoslovnicaId");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervacija_VoziloId_Status_DatumOd_DatumDo",
                table: "Rezervacija",
                columns: new[] { "VoziloId", "Status", "DatumOd", "DatumDo" });

            migrationBuilder.CreateIndex(
                name: "IX_Role_Naziv",
                table: "Role",
                column: "Naziv",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlikaVozila_VoziloId_Redoslijed",
                table: "SlikaVozila",
                columns: new[] { "VoziloId", "Redoslijed" });

            migrationBuilder.CreateIndex(
                name: "IX_StanjeOpreme_PoslovnicaId",
                table: "StanjeOpreme",
                column: "PoslovnicaId");

            migrationBuilder.CreateIndex(
                name: "IX_StanjeOpreme_VrstaOpremeId_PoslovnicaId",
                table: "StanjeOpreme",
                columns: new[] { "VrstaOpremeId", "PoslovnicaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StavkaOpreme_RezervacijaId_VrstaOpremeId",
                table: "StavkaOpreme",
                columns: new[] { "RezervacijaId", "VrstaOpremeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StavkaOpreme_VrstaOpremeId",
                table: "StavkaOpreme",
                column: "VrstaOpremeId");

            migrationBuilder.CreateIndex(
                name: "IX_TipGoriva_Naziv",
                table: "TipGoriva",
                column: "Naziv",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TipVozila_Naziv",
                table: "TipVozila",
                column: "Naziv",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VozackaDozvola_BrojDozvole",
                table: "VozackaDozvola",
                column: "BrojDozvole",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VozackaDozvola_KorisnikId",
                table: "VozackaDozvola",
                column: "KorisnikId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VozackaDozvola_VerifikovaoKorisnikId",
                table: "VozackaDozvola",
                column: "VerifikovaoKorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_Vozilo_ModelVozilaId",
                table: "Vozilo",
                column: "ModelVozilaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vozilo_PoslovnicaId",
                table: "Vozilo",
                column: "PoslovnicaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vozilo_RegistarskaOznaka",
                table: "Vozilo",
                column: "RegistarskaOznaka",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VrstaOpreme_Naziv",
                table: "VrstaOpreme",
                column: "Naziv",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlokadaVozila");

            migrationBuilder.DropTable(
                name: "Cjenovnik");

            migrationBuilder.DropTable(
                name: "DozvolaKategorija");

            migrationBuilder.DropTable(
                name: "EvidencijaStete");

            migrationBuilder.DropTable(
                name: "FotografijaPrimopredaje");

            migrationBuilder.DropTable(
                name: "HistorijaPretrage");

            migrationBuilder.DropTable(
                name: "HistorijaStatusaRezervacije");

            migrationBuilder.DropTable(
                name: "KodZaResetLozinke");

            migrationBuilder.DropTable(
                name: "KorisnikRole");

            migrationBuilder.DropTable(
                name: "Notifikacija");

            migrationBuilder.DropTable(
                name: "Obavijest");

            migrationBuilder.DropTable(
                name: "ObradjeniWebhookEvent");

            migrationBuilder.DropTable(
                name: "OpozvaniToken");

            migrationBuilder.DropTable(
                name: "PravilaKategorije");

            migrationBuilder.DropTable(
                name: "Recenzija");

            migrationBuilder.DropTable(
                name: "Refund");

            migrationBuilder.DropTable(
                name: "SlikaVozila");

            migrationBuilder.DropTable(
                name: "StanjeOpreme");

            migrationBuilder.DropTable(
                name: "StavkaOpreme");

            migrationBuilder.DropTable(
                name: "VozackaDozvola");

            migrationBuilder.DropTable(
                name: "Primopredaja");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "Placanje");

            migrationBuilder.DropTable(
                name: "VrstaOpreme");

            migrationBuilder.DropTable(
                name: "Rezervacija");

            migrationBuilder.DropTable(
                name: "Korisnik");

            migrationBuilder.DropTable(
                name: "PaketOsiguranja");

            migrationBuilder.DropTable(
                name: "Vozilo");

            migrationBuilder.DropTable(
                name: "ModelVozila");

            migrationBuilder.DropTable(
                name: "Poslovnica");

            migrationBuilder.DropTable(
                name: "KategorijaDozvole");

            migrationBuilder.DropTable(
                name: "Marka");

            migrationBuilder.DropTable(
                name: "TipGoriva");

            migrationBuilder.DropTable(
                name: "TipVozila");

            migrationBuilder.DropTable(
                name: "Grad");

            migrationBuilder.DropTable(
                name: "Drzava");
        }
    }
}
