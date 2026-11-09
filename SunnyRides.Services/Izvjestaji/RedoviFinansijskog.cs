using System.Globalization;
using SunnyRides.Model.DTOs;

namespace SunnyRides.Services.Izvjestaji;

/// <summary>Naplate jedne poslovnice u jednom mjesecu, kako ih vrati grupisani upit.</summary>
public record NaplataMjeseca(
    int Godina,
    int Mjesec,
    int PoslovnicaId,
    string Poslovnica,
    int BrojRezervacija,
    decimal Naplaceno,
    decimal VrijednostNajmova);

/// <summary>Povrati jedne poslovnice u jednom mjesecu.</summary>
public record PovratMjeseca(int Godina, int Mjesec, int PoslovnicaId, string Poslovnica, decimal Iznos);

/// <summary>
/// Sastavljanje redova finansijskog pregleda iz naplata i povrata.
///
/// Red postoji za svaki mjesec i poslovnicu u kojoj je bilo **ili** naplate **ili**
/// povrata. Najam placen u septembru i otkazan u oktobru daje u oktobru red sa
/// naplatom 0 i stvarnim povratom, pa neto tog mjeseca izlazi negativan - sto je
/// tacno, novac je tog mjeseca otisao. Da redovi nastaju samo iz naplata, takav povrat
/// bi nestao i iz reda i iz zbira.
/// </summary>
public static class RedoviFinansijskog
{
    public static (List<StavkaFinansijskogDto> Stavke, ZbirFinansijskogDto Zbir) Sastavi(
        IReadOnlyCollection<NaplataMjeseca> naplate, IReadOnlyCollection<PovratMjeseca> povrati)
    {
        var naplatePoKljucu = naplate.ToDictionary(x => (x.Godina, x.Mjesec, x.PoslovnicaId));
        var povratiPoKljucu = povrati.ToDictionary(x => (x.Godina, x.Mjesec, x.PoslovnicaId));

        var kljucevi = naplatePoKljucu.Keys.Union(povratiPoKljucu.Keys);

        var redovi = kljucevi
            .Select(kljuc =>
            {
                naplatePoKljucu.TryGetValue(kljuc, out var naplata);
                povratiPoKljucu.TryGetValue(kljuc, out var povrat);

                var naplaceno = naplata?.Naplaceno ?? 0m;
                var refundirano = povrat?.Iznos ?? 0m;
                var brojRezervacija = naplata?.BrojRezervacija ?? 0;
                var vrijednostNajmova = naplata?.VrijednostNajmova ?? 0m;

                var stavka = new StavkaFinansijskogDto
                {
                    Godina = kljuc.Godina,
                    Mjesec = kljuc.Mjesec,
                    Period = NazivMjeseca(kljuc.Godina, kljuc.Mjesec),
                    PoslovnicaId = kljuc.PoslovnicaId,
                    Poslovnica = naplata?.Poslovnica ?? povrat!.Poslovnica,
                    BrojRezervacija = brojRezervacija,
                    Naplaceno = naplaceno,
                    Refundirano = refundirano,
                    NetoPrihod = naplaceno - refundirano,
                    ProsjecnaVrijednostNajma = Prosjek(vrijednostNajmova, brojRezervacija)
                };

                return (Stavka: stavka, VrijednostNajmova: vrijednostNajmova);
            })
            .OrderBy(x => x.Stavka.Godina)
            .ThenBy(x => x.Stavka.Mjesec)
            .ThenBy(x => x.Stavka.Poslovnica)
            .ToList();

        var stavke = redovi.Select(x => x.Stavka).ToList();

        var ukupnoRezervacija = stavke.Sum(x => x.BrojRezervacija);
        var ukupnoNaplaceno = stavke.Sum(x => x.Naplaceno);
        var ukupnoRefundirano = stavke.Sum(x => x.Refundirano);

        var zbir = new ZbirFinansijskogDto
        {
            BrojRezervacija = ukupnoRezervacija,
            Naplaceno = ukupnoNaplaceno,
            Refundirano = ukupnoRefundirano,
            NetoPrihod = ukupnoNaplaceno - ukupnoRefundirano,
            ProsjecnaVrijednostNajma = Prosjek(redovi.Sum(x => x.VrijednostNajmova), ukupnoRezervacija)
        };

        return (stavke, zbir);
    }

    /// <summary>
    /// Prosjecna vrijednost najma, bez depozita. Depozit je novac klijenta koji se
    /// vraca, pa bi u prosjeku napuhao vrijednost svakog najma za iznos koji agencija
    /// ne zaradjuje.
    /// </summary>
    private static decimal Prosjek(decimal vrijednostNajmova, int brojRezervacija) =>
        brojRezervacija > 0 ? Math.Round(vrijednostNajmova / brojRezervacija, 2) : 0m;

    public static string NazivMjeseca(int godina, int mjesec)
    {
        string[] mjeseci =
        {
            "januar", "februar", "mart", "april", "maj", "juni",
            "juli", "august", "septembar", "oktobar", "novembar", "decembar"
        };

        var naziv = mjeseci[Math.Clamp(mjesec, 1, 12) - 1];

        return string.Create(CultureInfo.InvariantCulture, $"{naziv} {godina}");
    }
}
