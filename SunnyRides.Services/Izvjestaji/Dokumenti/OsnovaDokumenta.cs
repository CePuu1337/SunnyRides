using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SunnyRides.Services.Izvjestaji.Dokumenti;

/// <summary>
/// Zajednicki izgled oba izvjestaja: zaglavlje sa nazivom, periodom i poslovnicom, te
/// podnozje sa vremenom generisanja i brojem stranice.
///
/// Postoji da se izgled ne bi pisao dvaput. Kad se doda treci izvjestaj, on nasljedjuje
/// isti okvir i razlikuje se samo po tabeli.
/// </summary>
public abstract class OsnovaDokumenta : IDocument
{
    protected const string Agencija = "SunnyRides";

    /// <summary>Boja zaglavlja tabele. Jedna nijansa kroz cijeli dokument, bez ukrasa.</summary>
    protected static readonly Color BojaZaglavlja = Color.FromHex("#E8EDF2");

    protected static readonly Color BojaZbira = Color.FromHex("#D9E2EC");

    protected abstract string Naslov { get; }

    protected abstract DateTime Od { get; }
    protected abstract DateTime Do { get; }
    protected abstract string? Poslovnica { get; }
    protected abstract DateTime GenerisanoUtc { get; }

    /// <summary>Uspravno ili polozeno - izvjestaj sa mnogo kolona trazi polozenu stranicu.</summary>
    protected virtual bool Polozeno => false;

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"{Agencija} - {Naslov}",
        Author = Agencija
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(stranica =>
        {
            stranica.Size(Polozeno ? PageSizes.A4.Landscape() : PageSizes.A4);
            stranica.Margin(1.5f, Unit.Centimetre);
            stranica.DefaultTextStyle(x => x.FontSize(9));

            stranica.Header().Element(Zaglavlje);
            stranica.Content().PaddingVertical(10).Element(Sadrzaj);
            stranica.Footer().Element(Podnozje);
        });
    }

    protected abstract void Sadrzaj(IContainer container);

    private void Zaglavlje(IContainer container)
    {
        container.Column(kolona =>
        {
            kolona.Item().Row(red =>
            {
                red.RelativeItem().Column(lijevo =>
                {
                    lijevo.Item().Text(Agencija).FontSize(16).SemiBold();
                    lijevo.Item().Text(Naslov).FontSize(12);
                });

                red.ConstantItem(200).AlignRight().Column(desno =>
                {
                    desno.Item().Text($"Period: {Od:dd.MM.yyyy.} - {Do.AddDays(-1):dd.MM.yyyy.}");
                    desno.Item().Text($"Poslovnica: {Poslovnica ?? "sve poslovnice"}");
                });
            });

            kolona.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Medium);
        });
    }

    private void Podnozje(IContainer container)
    {
        container.Column(kolona =>
        {
            kolona.Item().PaddingBottom(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

            kolona.Item().Row(red =>
            {
                red.RelativeItem()
                    .Text($"Generisano {GenerisanoUtc:dd.MM.yyyy. HH:mm} (UTC)")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);

                red.RelativeItem().AlignRight().Text(tekst =>
                {
                    tekst.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1));
                    tekst.Span("Stranica ");
                    tekst.CurrentPageNumber();
                    tekst.Span(" od ");
                    tekst.TotalPages();
                });
            });
        });
    }

    // --- pomocne celije ----------------------------------------------------

    protected static IContainer CelijaZaglavlja(IContainer container) =>
        container.Background(BojaZaglavlja).Padding(4).DefaultTextStyle(x => x.SemiBold());

    protected static IContainer Celija(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);

    protected static IContainer CelijaZbira(IContainer container) =>
        container.Background(BojaZbira).Padding(4).DefaultTextStyle(x => x.SemiBold());

    protected static string Novac(decimal iznos) => $"{iznos:N2} EUR";

    protected static string Postotak(double vrijednost) => $"{vrijednost:N1} %";
}
