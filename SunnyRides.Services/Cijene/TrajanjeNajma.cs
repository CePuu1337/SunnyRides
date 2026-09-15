using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Cijene;

/// <summary>
/// Koliko se sati ili dana naplacuje za zadati period.
///
/// Namjerno je odvojeno od ostatka obracuna i nema nijednu zavisnost - ni bazu, ni
/// cjenovnik, ni vozilo. Zbog toga se moze testirati kao obicna funkcija, a upravo
/// ovo je dio obracuna koji ima najvise rubnih slucajeva.
/// </summary>
public record TrajanjeNajma(bool PoSatu, int Sati, int Dani)
{
    /// <summary>Granica ispod koje se naplacuje po satu.</summary>
    public const int SatnaGranicaSati = 6;

    /// <summary>
    /// Tolerancija pri prekoracenju punog dana. Vracanje vozila deset minuta poslije
    /// roka ne smije klijenta kostati cijeli dodatni dan; sat i vise vec smije.
    /// </summary>
    public const int GraceMinuta = 59;

    /// <summary>Broj dana koji se naplacuje za opremu i osiguranje. Najam po satu racuna se kao jedan dan.</summary>
    public int DaniZaDodatke => PoSatu ? 1 : Dani;

    public static TrajanjeNajma Izracunaj(DateTime datumOd, DateTime datumDo)
    {
        var trajanje = datumDo - datumOd;

        if (trajanje <= TimeSpan.Zero)
        {
            throw new BusinessException("Datum vracanja mora biti poslije datuma preuzimanja.");
        }

        // Do sest sati se naplacuje po satu. Zapoceti sat se racuna cijeli, kao na
        // parkingu - inace bi najam od 61 minute kostao kao najam od jednog sata.
        if (trajanje <= TimeSpan.FromHours(SatnaGranicaSati))
        {
            var sati = (int)Math.Ceiling(trajanje.TotalHours);
            return new TrajanjeNajma(PoSatu: true, Sati: Math.Max(sati, 1), Dani: 0);
        }

        // Izmedju sest i dvadeset cetiri sata naplacuje se jedan dan. Ovdje satna
        // tarifa vec prelazi dnevnu, pa bi klijent inace platio vise za krace.
        if (trajanje <= TimeSpan.FromHours(24))
        {
            return new TrajanjeNajma(PoSatu: false, Sati: 0, Dani: 1);
        }

        var puniDani = (int)Math.Floor(trajanje.TotalHours / 24);
        var ostatak = trajanje - TimeSpan.FromHours(puniDani * 24);

        var dani = ostatak > TimeSpan.FromMinutes(GraceMinuta) ? puniDani + 1 : puniDani;

        return new TrajanjeNajma(PoSatu: false, Sati: 0, Dani: dani);
    }
}
