namespace SunnyRides.Services.Preporuke.Ml;

/// <summary>
/// Pretvara ono sto sistem ima (recenzije i zavrsene najmove) u matricu koju model uci.
///
/// Klasa nema bazu ni ML.NET - prima liste, vraca liste, pa se cijelo pravilo pripreme
/// podataka provjerava obicnim testovima. To je i najosjetljiviji dio: greska ovdje ne
/// obara build nego tiho kvari ono sto model nauci.
/// </summary>
public static class PripremaInterakcija
{
    /// <summary>
    /// Ispod ovoliko stvarnih ocjena se ne trenira. Matricna faktorizacija sa sacicom
    /// redova nauci sum, a predikcija koja se ni na sta ne oslanja gora je od postenog
    /// pada na rezervnu logiku.
    /// </summary>
    public const int NajmanjeOcjenaZaTreniranje = 15;

    /// <summary>Isto vrijedi i za broj korisnika - model uci iz poklapanja medju njima.</summary>
    public const int NajmanjeKorisnikaZaTreniranje = 3;

    /// <summary>
    /// Jedan red po paru korisnik-model.
    ///
    /// Korisnik koji je isti model ocijenio vise puta ulazi sa prosjekom svojih ocjena.
    /// Par koji je samo iznajmljen, a nikad ocijenjen, ulazi sa prosjekom flote: to je
    /// posten prevod recenice "uzeo je vozilo i nije se zalio" - ni odusevljenje ni
    /// nezadovoljstvo, nego prosjecno iskustvo. Takav red nosi oznaku da nije stvarna
    /// ocjena, pa nikad ne zavrsi u test skupu.
    /// </summary>
    public static List<InterakcijaZapis> Sastavi(
        IReadOnlyList<OcjenaZapis> ocjene,
        IReadOnlyList<NajamZapis> najmovi,
        double prosjekFlote)
    {
        var redovi = ocjene
            .GroupBy(x => (x.KorisnikId, x.ModelVozilaId))
            .Select(g => new InterakcijaZapis(
                g.Key.KorisnikId, g.Key.ModelVozilaId, g.Average(x => x.Ocjena), JeStvarnaOcjena: true))
            .ToList();

        var ocijenjeni = redovi.Select(x => (x.KorisnikId, x.ModelVozilaId)).ToHashSet();

        var procijenjeni = najmovi
            .Select(x => (x.KorisnikId, x.ModelVozilaId))
            .Distinct()
            .Where(x => !ocijenjeni.Contains(x))
            .Select(x => new InterakcijaZapis(
                x.KorisnikId, x.ModelVozilaId, prosjekFlote, JeStvarnaOcjena: false));

        redovi.AddRange(procijenjeni);

        return redovi;
    }

    /// <summary>
    /// Ima li dovoljno podataka da treniranje uopste ima smisla.
    /// </summary>
    public static bool DovoljnoZaTreniranje(IReadOnlyList<InterakcijaZapis> redovi)
    {
        var stvarne = redovi.Where(x => x.JeStvarnaOcjena).ToList();

        return stvarne.Count >= NajmanjeOcjenaZaTreniranje
               && stvarne.Select(x => x.KorisnikId).Distinct().Count() >= NajmanjeKorisnikaZaTreniranje;
    }

    /// <summary>
    /// Dijeli podatke na skup za ucenje i skup za provjeru.
    ///
    /// U provjeru idu **samo stvarne ocjene**, i to nasumican dio njih. Procijenjeni
    /// redovi ostaju u ucenju: oni pomazu modelu da popuni matricu, ali nisu istina
    /// prema kojoj se model smije mjeriti.
    ///
    /// Sjeme generatora je fiksno, pa dva uzastopna treniranja nad istim podacima daju
    /// isti rezultat - inace se ne bi moglo tvrditi da je promjena RMSE-a posljedica
    /// izmjene modela a ne slucaja.
    /// </summary>
    public static (List<InterakcijaZapis> ZaUcenje, List<InterakcijaZapis> ZaProvjeru) Podijeli(
        IReadOnlyList<InterakcijaZapis> redovi, double udioZaProvjeru = 0.2, int sjeme = 20260918)
    {
        var slucajan = new Random(sjeme);

        var stvarne = redovi.Where(x => x.JeStvarnaOcjena).OrderBy(_ => slucajan.Next()).ToList();
        var ostale = redovi.Where(x => !x.JeStvarnaOcjena).ToList();

        var zaProvjeru = (int)Math.Round(stvarne.Count * udioZaProvjeru);

        // Uvijek mora ostati dovoljno za ucenje, pa se test skup ogranicava.
        zaProvjeru = Math.Clamp(zaProvjeru, 0, Math.Max(0, stvarne.Count - NajmanjeOcjenaZaTreniranje));

        var provjera = stvarne.Take(zaProvjeru).ToList();
        var ucenje = stvarne.Skip(zaProvjeru).Concat(ostale).ToList();

        return (ucenje, provjera);
    }

    /// <summary>
    /// Dijeli podatke za unakrsnu provjeru.
    ///
    /// Stvarne ocjene se izmijesaju i podijele na jednake dijelove. Svaki dio jednom
    /// bude skup za provjeru, a sve ostalo - zajedno sa procijenjenim redovima - ide u
    /// ucenje. Tako svaka ocjena tacno jednom posluzi za mjerenje.
    ///
    /// Ovo zamjenjuje jedan mali skup za odabir parametara. Na pedesetak ocjena izdvojenih
    /// deset redova daju gresku koja je vise stvar slucaja nego mjera kvaliteta - izbor
    /// parametara po takvom broju je pogadjanje sa dodatnim korakom.
    /// </summary>
    public static List<(List<InterakcijaZapis> ZaUcenje, List<InterakcijaZapis> ZaProvjeru)> PodijeliUnakrsno(
        IReadOnlyList<InterakcijaZapis> redovi, int brojDijelova = 5, int sjeme = 20260920)
    {
        var rezultat = new List<(List<InterakcijaZapis>, List<InterakcijaZapis>)>();

        var slucajan = new Random(sjeme);
        var stvarne = redovi.Where(x => x.JeStvarnaOcjena).OrderBy(_ => slucajan.Next()).ToList();
        var ostale = redovi.Where(x => !x.JeStvarnaOcjena).ToList();

        // Dio mora imati bar jednu ocjenu, a u ucenju mora ostati dovoljno za treniranje.
        if (brojDijelova < 2 || stvarne.Count < brojDijelova)
        {
            return rezultat;
        }

        for (var dio = 0; dio < brojDijelova; dio++)
        {
            var zaProvjeru = stvarne.Where((_, indeks) => indeks % brojDijelova == dio).ToList();
            var zaUcenje = stvarne.Where((_, indeks) => indeks % brojDijelova != dio).Concat(ostale).ToList();

            if (zaProvjeru.Count == 0 || zaUcenje.Count(x => x.JeStvarnaOcjena) < NajmanjeOcjenaZaTreniranje)
            {
                return new List<(List<InterakcijaZapis>, List<InterakcijaZapis>)>();
            }

            rezultat.Add((zaUcenje, zaProvjeru));
        }

        return rezultat;
    }
}
