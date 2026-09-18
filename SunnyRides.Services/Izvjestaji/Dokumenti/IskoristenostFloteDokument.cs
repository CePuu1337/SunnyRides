using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SunnyRides.Model.DTOs;

namespace SunnyRides.Services.Izvjestaji.Dokumenti;

/// <summary>
/// Iskoristenost flote: jedan red po vozilu, zbirni red i sumarni prikaz po tipu vozila.
///
/// Stranica je polozena jer izvjestaj ima osam kolona - uspravno bi se brojevi lomili u
/// dva reda i tabela bi postala necitljiva.
/// </summary>
public class IskoristenostFloteDokument : OsnovaDokumenta
{
    private readonly IskoristenostFloteDto _podaci;

    public IskoristenostFloteDokument(IskoristenostFloteDto podaci)
    {
        _podaci = podaci;
    }

    protected override string Naslov => "Iskorištenost flote";

    protected override DateTime Od => _podaci.Od;
    protected override DateTime Do => _podaci.Do;
    protected override string? Poslovnica => _podaci.Poslovnica;
    protected override DateTime GenerisanoUtc => _podaci.GenerisanoUtc;

    protected override bool Polozeno => true;

    protected override void Sadrzaj(IContainer container)
    {
        container.Column(kolona =>
        {
            kolona.Item().Element(TabelaVozila);

            kolona.Item().PaddingTop(16).Text("Sumarno po tipu vozila").FontSize(11).SemiBold();
            kolona.Item().PaddingTop(6).Element(TabelaPoTipu);

            if (_podaci.Stavke.Count == 0)
            {
                kolona.Item().PaddingTop(20)
                    .Text("U odabranom periodu i poslovnici nema vozila za prikaz.")
                    .Italic();
            }
        });
    }

    private void TabelaVozila(IContainer container)
    {
        container.Table(tabela =>
        {
            tabela.ColumnsDefinition(kolone =>
            {
                kolone.RelativeColumn(3);      // vozilo
                kolone.RelativeColumn(1.6f);   // registracija
                kolone.RelativeColumn(1.4f);   // tip
                kolone.RelativeColumn(2);      // poslovnica
                kolone.RelativeColumn(1);      // najmova
                kolone.RelativeColumn(1.2f);   // dana izdato
                kolone.RelativeColumn(1.4f);   // iskoristenost
                kolone.RelativeColumn(1.8f);   // prihod
                kolone.RelativeColumn(1.4f);   // ocjena
            });

            tabela.Header(zaglavlje =>
            {
                zaglavlje.Cell().Element(CelijaZaglavlja).Text("Vozilo");
                zaglavlje.Cell().Element(CelijaZaglavlja).Text("Registracija");
                zaglavlje.Cell().Element(CelijaZaglavlja).Text("Tip");
                zaglavlje.Cell().Element(CelijaZaglavlja).Text("Poslovnica");
                zaglavlje.Cell().Element(CelijaZaglavlja).AlignRight().Text("Najmova");
                zaglavlje.Cell().Element(CelijaZaglavlja).AlignRight().Text("Dana izdato");
                zaglavlje.Cell().Element(CelijaZaglavlja).AlignRight().Text("Iskorištenost");
                zaglavlje.Cell().Element(CelijaZaglavlja).AlignRight().Text("Prihod");
                zaglavlje.Cell().Element(CelijaZaglavlja).AlignRight().Text("Ocjena");
            });

            foreach (var stavka in _podaci.Stavke)
            {
                tabela.Cell().Element(Celija).Text(stavka.Vozilo);
                tabela.Cell().Element(Celija).Text(stavka.RegistarskaOznaka);
                tabela.Cell().Element(Celija).Text(stavka.TipVozila);
                tabela.Cell().Element(Celija).Text(stavka.Poslovnica);
                tabela.Cell().Element(Celija).AlignRight().Text(stavka.BrojNajmova.ToString());
                tabela.Cell().Element(Celija).AlignRight().Text($"{stavka.DanaIzdato:N1}");
                tabela.Cell().Element(Celija).AlignRight().Text(Postotak(stavka.Iskoristenost));
                tabela.Cell().Element(Celija).AlignRight().Text(Novac(stavka.Prihod));

                // Vozilo bez ijedne ocjene dobija crticu, ne nulu. Nula bi znacila losu
                // ocjenu, a rijec je o tome da ocjene nema.
                tabela.Cell().Element(Celija).AlignRight().Text(stavka.BrojOcjena > 0
                    ? $"{stavka.ProsjecnaOcjena:N2} ({stavka.BrojOcjena})"
                    : "-");
            }

            var zbir = _podaci.Zbir;

            tabela.Cell().ColumnSpan(4).Element(CelijaZbira).Text($"Ukupno ({zbir.BrojVozila} vozila)");
            tabela.Cell().Element(CelijaZbira).AlignRight().Text(zbir.BrojNajmova.ToString());
            tabela.Cell().Element(CelijaZbira).AlignRight().Text($"{zbir.DanaIzdato:N1}");
            tabela.Cell().Element(CelijaZbira).AlignRight().Text(Postotak(zbir.Iskoristenost));
            tabela.Cell().Element(CelijaZbira).AlignRight().Text(Novac(zbir.Prihod));
            tabela.Cell().Element(CelijaZbira).AlignRight().Text(zbir.ProsjecnaOcjena.HasValue
                ? $"{zbir.ProsjecnaOcjena:N2}"
                : "-");
        });
    }

    private void TabelaPoTipu(IContainer container)
    {
        container.Table(tabela =>
        {
            tabela.ColumnsDefinition(kolone =>
            {
                kolone.RelativeColumn(3);
                kolone.RelativeColumn(1.5f);
                kolone.RelativeColumn(1.5f);
                kolone.RelativeColumn(1.5f);
                kolone.RelativeColumn(1.5f);
                kolone.RelativeColumn(2);
            });

            tabela.Header(zaglavlje =>
            {
                zaglavlje.Cell().Element(CelijaZaglavlja).Text("Tip vozila");
                zaglavlje.Cell().Element(CelijaZaglavlja).AlignRight().Text("Vozila");
                zaglavlje.Cell().Element(CelijaZaglavlja).AlignRight().Text("Najmova");
                zaglavlje.Cell().Element(CelijaZaglavlja).AlignRight().Text("Dana izdato");
                zaglavlje.Cell().Element(CelijaZaglavlja).AlignRight().Text("Iskorištenost");
                zaglavlje.Cell().Element(CelijaZaglavlja).AlignRight().Text("Prihod");
            });

            foreach (var red in _podaci.PoTipuVozila)
            {
                tabela.Cell().Element(Celija).Text(red.TipVozila);
                tabela.Cell().Element(Celija).AlignRight().Text(red.BrojVozila.ToString());
                tabela.Cell().Element(Celija).AlignRight().Text(red.BrojNajmova.ToString());
                tabela.Cell().Element(Celija).AlignRight().Text($"{red.DanaIzdato:N1}");
                tabela.Cell().Element(Celija).AlignRight().Text(Postotak(red.Iskoristenost));
                tabela.Cell().Element(Celija).AlignRight().Text(Novac(red.Prihod));
            }
        });
    }
}
