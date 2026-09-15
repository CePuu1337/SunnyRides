using SunnyRides.Model.DTOs;

namespace SunnyRides.Services.Cijene;

/// <summary>
/// Sam obracun cijene - cista funkcija, bez baze i bez stanja.
///
/// Servis ucitava podatke, ovo ih racuna. Podjela postoji zbog provjerljivosti:
/// kontrolni primjeri iz uputstva (24 h 30 min, 25 h, 48 h, 49 h) testiraju se
/// ovdje, bez baze i bez mokova, pa se ne moze desiti da test prodje zato sto je
/// mok podeseno da vrati ocekivani rezultat.
/// </summary>
public static class ObracunCijene
{
    public static CijenaRezervacijeDto Izracunaj(UlazObracuna ulaz)
    {
        var trajanje = TrajanjeNajma.Izracunaj(ulaz.DatumOd, ulaz.DatumDo);

        var osnovica = trajanje.PoSatu
            ? ulaz.SatnaTarifa * trajanje.Sati
            : ulaz.DnevnaTarifa * trajanje.Dani;

        // Sezonski mnozilac djeluje na osnovicu, prije popusta. Redoslijed nije
        // proizvoljan: popust je popust na cijenu koja se stvarno naplacuje, pa se
        // racuna od iznosa koji vec nosi sezonu.
        var saSezonom = Zaokruzi(osnovica * ulaz.Mnozilac);

        var procenatPopusta = ProcenatPopusta(trajanje.Dani, ulaz);
        var iznosPopusta = Zaokruzi(saSezonom * procenatPopusta / 100m);

        var oprema = ObracunajOpremu(ulaz.Oprema, trajanje.DaniZaDodatke);
        var iznosOpreme = Zaokruzi(oprema.Sum(x => x.Iznos));

        var iznosOsiguranja = ulaz.PaketOsiguranjaId.HasValue
            ? Zaokruzi(ulaz.OsiguranjeCijenaPoDanu * trajanje.DaniZaDodatke)
            : 0m;

        var iznosNajma = Zaokruzi(saSezonom - iznosPopusta);
        var depozit = Zaokruzi(ulaz.IznosDepozita);

        return new CijenaRezervacijeDto
        {
            DatumOd = ulaz.DatumOd,
            DatumDo = ulaz.DatumDo,

            NaplataPoSatu = trajanje.PoSatu,
            BrojSati = trajanje.Sati,
            BrojDana = trajanje.Dani,

            SatnaTarifa = ulaz.SatnaTarifa,
            DnevnaTarifa = ulaz.DnevnaTarifa,
            Mnozilac = ulaz.Mnozilac,
            NazivSezone = ulaz.NazivSezone,

            OsnovicaNajma = Zaokruzi(osnovica),
            ProcenatPopusta = procenatPopusta,
            IznosPopusta = iznosPopusta,
            IznosNajma = iznosNajma,

            Oprema = oprema,
            IznosOpreme = iznosOpreme,

            PaketOsiguranjaId = ulaz.PaketOsiguranjaId,
            PaketOsiguranjaNaziv = ulaz.PaketOsiguranjaNaziv,
            IznosOsiguranja = iznosOsiguranja,

            IznosDepozita = depozit,
            UkupanIznos = Zaokruzi(iznosNajma + iznosOpreme + iznosOsiguranja + depozit)
        };
    }

    /// <summary>
    /// Pragovi i procenti dolaze iz cjenovnika, nisu zakucani u kodu. Gleda se prvo
    /// visi prag, da najam od deset dana dobije popust za sedam a ne za tri dana.
    /// </summary>
    private static decimal ProcenatPopusta(int dani, UlazObracuna ulaz)
    {
        if (dani >= ulaz.PopustPrag2)
        {
            return ulaz.PopustProcenat2;
        }

        if (dani >= ulaz.PopustPrag1)
        {
            return ulaz.PopustProcenat1;
        }

        return 0m;
    }

    private static List<StavkaCijeneDto> ObracunajOpremu(
        IReadOnlyList<StavkaOpremeUlaz> stavke, int dani)
    {
        var rezultat = new List<StavkaCijeneDto>();

        foreach (var stavka in stavke)
        {
            // Oprema se naplacuje ili po danu ili fiksno. Sifrarnik garantuje da je
            // tacno jedno od ta dva postavljeno, pa ovdje nema treceg slucaja.
            var cijenaPoJedinici = stavka.CijenaPoDanu.HasValue
                ? Zaokruzi(stavka.CijenaPoDanu.Value * dani)
                : Zaokruzi(stavka.FiksnaCijena ?? 0m);

            rezultat.Add(new StavkaCijeneDto
            {
                VrstaOpremeId = stavka.VrstaOpremeId,
                Naziv = stavka.Naziv,
                Kolicina = stavka.Kolicina,
                CijenaPoJedinici = cijenaPoJedinici,
                Iznos = Zaokruzi(cijenaPoJedinici * stavka.Kolicina)
            });
        }

        return rezultat;
    }

    /// <summary>
    /// Zaokruzivanje na dvije decimale, na svakoj stavci posebno. Tako se zbir
    /// prikazanih stavki uvijek poklapa sa prikazanim ukupnim iznosom - inace bi
    /// klijent vidio razradu koja se ne sabira.
    /// </summary>
    private static decimal Zaokruzi(decimal iznos) =>
        Math.Round(iznos, 2, MidpointRounding.AwayFromZero);
}
