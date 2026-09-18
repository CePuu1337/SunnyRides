using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SunnyRides.Model.DTOs;

namespace SunnyRides.Services.Izvjestaji.Dokumenti;

/// <summary>
/// Finansijski pregled: jedan red po mjesecu i poslovnici, sa zbirnim redom.
///
/// Sve brojke dolaze iz placanja, ne iz iznosa rezervacija - izvjestaj o prihodu mora
/// govoriti o novcu koji je stvarno presao.
/// </summary>
public class FinansijskiPregledDokument : OsnovaDokumenta
{
    private readonly FinansijskiPregledDto _podaci;

    public FinansijskiPregledDokument(FinansijskiPregledDto podaci)
    {
        _podaci = podaci;
    }

    protected override string Naslov => "Finansijski pregled";

    protected override DateTime Od => _podaci.Od;
    protected override DateTime Do => _podaci.Do;
    protected override string? Poslovnica => _podaci.Poslovnica;
    protected override DateTime GenerisanoUtc => _podaci.GenerisanoUtc;

    protected override void Sadrzaj(IContainer container)
    {
        container.Column(kolona =>
        {
            kolona.Item().Element(Tabela);

            if (_podaci.Stavke.Count == 0)
            {
                kolona.Item().PaddingTop(20)
                    .Text("U odabranom periodu nema naplaćenih iznosa.")
                    .Italic();
            }
        });
    }

    private void Tabela(IContainer container)
    {
        container.Table(tabela =>
        {
            tabela.ColumnsDefinition(kolone =>
            {
                kolone.RelativeColumn(2.2f);   // period
                kolone.RelativeColumn(2.6f);   // poslovnica
                kolone.RelativeColumn(1.3f);   // rezervacija
                kolone.RelativeColumn(2);      // naplaceno
                kolone.RelativeColumn(2);      // refundirano
                kolone.RelativeColumn(2);      // neto
                kolone.RelativeColumn(2);      // prosjecna vrijednost
            });

            tabela.Header(zaglavlje =>
            {
                zaglavlje.Cell().Element(CelijaZaglavlja).Text("Period");
                zaglavlje.Cell().Element(CelijaZaglavlja).Text("Poslovnica");
                zaglavlje.Cell().Element(CelijaZaglavlja).AlignRight().Text("Rezervacija");
                zaglavlje.Cell().Element(CelijaZaglavlja).AlignRight().Text("Naplaćeno");
                zaglavlje.Cell().Element(CelijaZaglavlja).AlignRight().Text("Refundirano");
                zaglavlje.Cell().Element(CelijaZaglavlja).AlignRight().Text("Neto prihod");
                zaglavlje.Cell().Element(CelijaZaglavlja).AlignRight().Text("Prosječan najam");
            });

            foreach (var stavka in _podaci.Stavke)
            {
                tabela.Cell().Element(Celija).Text(stavka.Period);
                tabela.Cell().Element(Celija).Text(stavka.Poslovnica);
                tabela.Cell().Element(Celija).AlignRight().Text(stavka.BrojRezervacija.ToString());
                tabela.Cell().Element(Celija).AlignRight().Text(Novac(stavka.Naplaceno));
                tabela.Cell().Element(Celija).AlignRight().Text(Novac(stavka.Refundirano));
                tabela.Cell().Element(Celija).AlignRight().Text(Novac(stavka.NetoPrihod));
                tabela.Cell().Element(Celija).AlignRight().Text(Novac(stavka.ProsjecnaVrijednostNajma));
            }

            var zbir = _podaci.Zbir;

            tabela.Cell().ColumnSpan(2).Element(CelijaZbira).Text("Ukupno");
            tabela.Cell().Element(CelijaZbira).AlignRight().Text(zbir.BrojRezervacija.ToString());
            tabela.Cell().Element(CelijaZbira).AlignRight().Text(Novac(zbir.Naplaceno));
            tabela.Cell().Element(CelijaZbira).AlignRight().Text(Novac(zbir.Refundirano));
            tabela.Cell().Element(CelijaZbira).AlignRight().Text(Novac(zbir.NetoPrihod));
            tabela.Cell().Element(CelijaZbira).AlignRight().Text(Novac(zbir.ProsjecnaVrijednostNajma));
        });
    }
}
