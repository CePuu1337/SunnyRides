namespace SunnyRides.Services.Dozvole;

/// <summary>Jedno pravilo iz tabele <c>PravilaKategorije</c>, bez EF zavisnosti.</summary>
public record PraviloUlaz(
    int KategorijaDozvoleId,
    int TipVozilaId,
    int? MaxKubikaza,
    decimal? MaxSnagaKw,
    int MinGodine);

/// <summary>
/// Koje kategorije vozila smije voziti neko ko posjeduje zadate kategorije dozvole.
///
/// Nigdje u ovoj klasi ne pise da "A pokriva A1". To se **izvodi iz podataka**:
/// pravilo kategorije A za motocikle nema gornju granicu kubikaze ni snage, a
/// pravilo kategorije A1 ima 125 cm3 i 11 kW. Ko ima A, ima pravilo koje pokriva
/// sve sto pokriva i A1 - pa je hijerarhija posljedica tabele, a ne grana u kodu.
///
/// Ako se propis promijeni i A1 dobije granicu od 150 cm3, ispravka je izmjena jednog
/// reda u sifrarniku. Da je hijerarhija napisana kao if, trebalo bi ponovo prevoditi
/// i objavljivati aplikaciju.
/// </summary>
public static class PravilaPokrivenosti
{
    /// <summary>
    /// Identifikatori kategorija cija vozila korisnik smije voziti.
    ///
    /// <paramref name="godine"/> ulazi u racun jer pravilo nosi i <c>MinGodine</c> -
    /// posjedovanje kategorije nije dovoljno ako klijent jos nije dovoljno star za nju.
    /// </summary>
    public static HashSet<int> Pokrivene(
        IReadOnlyList<PraviloUlaz> pravila,
        IReadOnlyCollection<int> posjedovaneKategorije,
        int godine)
    {
        var rezultat = new HashSet<int>();

        if (posjedovaneKategorije.Count == 0 || pravila.Count == 0)
        {
            return rezultat;
        }

        // Pravila kategorija koje korisnik posjeduje, i to samo ona za koja je
        // dovoljno star. Mladji vozac sa upisanom kategorijom A ne dobija njena prava
        // dok ne napuni godine koje pravilo trazi.
        var mojaPravila = pravila
            .Where(p => posjedovaneKategorije.Contains(p.KategorijaDozvoleId) && p.MinGodine <= godine)
            .ToList();

        if (mojaPravila.Count == 0)
        {
            return rezultat;
        }

        var poKategoriji = pravila.GroupBy(p => p.KategorijaDozvoleId);

        foreach (var kandidat in poKategoriji)
        {
            // Kategorija bez ijednog pravila ne daje pravo ni na sta - nema vozila
            // koje bi njome bilo pokriveno.
            var trazenaPravila = kandidat.ToList();

            var pokriva = mojaPravila
                .Select(p => p.KategorijaDozvoleId)
                .Distinct()
                .Any(mojaKategorija => SvaPravilaPokrivena(
                    trazenaPravila,
                    mojaPravila.Where(p => p.KategorijaDozvoleId == mojaKategorija).ToList()));

            if (pokriva)
            {
                rezultat.Add(kandidat.Key);
            }
        }

        return rezultat;
    }

    /// <summary>
    /// Posjedovana kategorija pokriva trazenu ako za svaki tip vozila koji trazena
    /// obuhvata ima vlastito pravilo koje nije strozije.
    /// </summary>
    private static bool SvaPravilaPokrivena(
        IReadOnlyList<PraviloUlaz> trazena, IReadOnlyList<PraviloUlaz> moja) =>
        trazena.All(t => moja.Any(m => m.TipVozilaId == t.TipVozilaId && Dominira(m, t)));

    /// <summary>
    /// Prazna granica znaci "bez ogranicenja" i pokriva svaku konkretnu granicu.
    /// Obrnuto ne vrijedi: pravilo sa granicom od 125 cm3 ne pokriva pravilo bez
    /// granice, jer bi inace A1 ispao ravnopravan sa A.
    /// </summary>
    private static bool Dominira(PraviloUlaz moje, PraviloUlaz trazeno)
    {
        var kubikaza = moje.MaxKubikaza is null
                       || (trazeno.MaxKubikaza is not null && moje.MaxKubikaza >= trazeno.MaxKubikaza);

        var snaga = moje.MaxSnagaKw is null
                    || (trazeno.MaxSnagaKw is not null && moje.MaxSnagaKw >= trazeno.MaxSnagaKw);

        return kubikaza && snaga;
    }
}
