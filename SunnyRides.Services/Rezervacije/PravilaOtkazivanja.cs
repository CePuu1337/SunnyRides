using SunnyRides.Model.DTOs;

namespace SunnyRides.Services.Rezervacije;

/// <summary>
/// Sta se vraca kad se rezervacija otkaze - cista funkcija, bez baze i bez stanja.
///
/// Odvojeno je iz istog razloga kao i obracun cijene: pravilo o povratu je ono sto
/// klijent citira kad se ne slaze sa iznosom, pa mora stajati na jednom mjestu koje
/// se moze procitati odjednom i provjeriti bez baze. Isto pravilo koristi i seed,
/// da podaci u bazi ne bi pricali drugu pricu od one koju racuna servis.
///
/// Osnova za obracun je **stvarno naplaceni iznos**, ne ponovni obracun iz cjenovnika.
/// Ako se cjenovnik u medjuvremenu promijeni, povrat to ne smije osjetiti - vraca se
/// dio onoga sto je naplaceno, a ne dio onoga sto bi danas kostalo.
/// </summary>
public static class PravilaOtkazivanja
{
    /// <summary>Otkazivanje ranije od ovoga vraca najam u cijelosti.</summary>
    public const int DanaZaPunPovrat = 7;

    /// <summary>Od ovoga do gornjeg praga vraca se polovina najma.</summary>
    public const int DanaZaPolovicanPovrat = 3;

    public const decimal ProcenatPolovicnog = 50m;

    /// <summary>
    /// Koliki se dio najma vraca.
    ///
    /// Depozit u ovome ne ucestvuje - on nije naknada nego polog i vraca se uvijek.
    /// Kad otkazuje agencija, klijent nije nista skrivio i dobija sve nazad, bez
    /// obzira koliko je vremena ostalo do preuzimanja.
    /// </summary>
    public static decimal ProcenatPovrataNajma(double danaDoPreuzimanja, bool otkazujeAgencija)
    {
        if (otkazujeAgencija)
        {
            return 100m;
        }

        if (danaDoPreuzimanja > DanaZaPunPovrat)
        {
            return 100m;
        }

        if (danaDoPreuzimanja >= DanaZaPolovicanPovrat)
        {
            return ProcenatPolovicnog;
        }

        return 0m;
    }

    public static ObracunOtkazivanjaDto Izracunaj(UlazOtkazivanja ulaz)
    {
        var danaDoPreuzimanja = (ulaz.DatumOd - ulaz.Sada).TotalDays;
        var procenat = ProcenatPovrataNajma(danaDoPreuzimanja, ulaz.OtkazujeAgencija);

        // Naplaceni iznos se dijeli na depozit i najam. Ogranicenje na naplaceno
        // postoji zbog djelimicne naplate: ako je uzeto manje nego sto depozit iznosi,
        // ne moze se vratiti vise nego sto je uzeto.
        var dioDepozita = Math.Min(Zaokruzi(ulaz.IznosDepozita), ulaz.Naplaceno);
        if (dioDepozita < 0)
        {
            dioDepozita = 0;
        }

        var dioNajma = Zaokruzi(ulaz.Naplaceno - dioDepozita);

        var povratNajma = Zaokruzi(dioNajma * procenat / 100m);
        var povratDepozita = dioDepozita;

        // Pripadajuci povrat, prije nego se odbije ono sto je vec vraceno. Iz ovoga
        // se racuna i ono sto agencija zadrzava, da zadrzani iznos ne raste svaki put
        // kad se obracun ponovo pozove.
        var pripada = Zaokruzi(povratNajma + povratDepozita);

        var zaIsplatu = Zaokruzi(pripada - ulaz.VecVraceno);
        if (zaIsplatu < 0)
        {
            zaIsplatu = 0;
        }

        return new ObracunOtkazivanjaDto
        {
            DatumOd = ulaz.DatumOd,
            DanaDoPreuzimanja = Math.Round(danaDoPreuzimanja, 2),
            OtkazujeAgencija = ulaz.OtkazujeAgencija,

            Naplaceno = ulaz.Naplaceno,
            VecVraceno = ulaz.VecVraceno,

            DioDepozita = dioDepozita,
            DioNajma = dioNajma,

            ProcenatPovrataNajma = procenat,
            PovratNajma = povratNajma,
            PovratDepozita = povratDepozita,

            UkupanPovrat = zaIsplatu,
            ZadrzanoAgenciji = Zaokruzi(ulaz.Naplaceno - pripada),

            Obrazlozenje = Obrazlozenje(ulaz, danaDoPreuzimanja, procenat, dioDepozita)
        };
    }

    /// <summary>
    /// Recenica koja se pokazuje klijentu prije nego potvrdi otkazivanje, i koja se
    /// zatim doslovno upisuje u historiju statusa. Kasnije se ne moze reci da nije
    /// znao po kojem pravilu je dobio koliko je dobio.
    /// </summary>
    private static string Obrazlozenje(
        UlazOtkazivanja ulaz, double dana, decimal procenat, decimal dioDepozita)
    {
        if (ulaz.Naplaceno <= 0)
        {
            return "Rezervacija nije placena, pa nema iznosa za povrat.";
        }

        var osnovno = ulaz.OtkazujeAgencija
            ? "Agencija je otkazala rezervaciju, pa se naplaceni iznos vraca u cijelosti."
            : procenat == 100m
                ? $"Otkazivanje vise od {DanaZaPunPovrat} dana prije preuzimanja - najam se vraca u cijelosti."
                : procenat == ProcenatPolovicnog
                    ? $"Otkazivanje izmedju {DanaZaPolovicanPovrat} i {DanaZaPunPovrat} dana prije preuzimanja - vraca se polovina najma."
                    : dana < 0
                        ? "Termin preuzimanja je vec poceo - najam se ne vraca."
                        : $"Otkazivanje manje od {DanaZaPolovicanPovrat} dana prije preuzimanja - najam se ne vraca.";

        return dioDepozita > 0
            ? osnovno + " Depozit se vraca u cijelosti."
            : osnovno;
    }

    /// <summary>
    /// Isto zaokruzivanje kao u obracunu cijene - na dvije decimale, na svakoj stavci
    /// posebno, da se prikazana razrada sabira u prikazani ukupan iznos.
    /// </summary>
    public static decimal Zaokruzi(decimal iznos) =>
        Math.Round(iznos, 2, MidpointRounding.AwayFromZero);
}

/// <summary>
/// Sve sto obracun povrata treba. Namjerno su to brojevi i datumi, a ne entiteti -
/// da se pravilo moze testirati bez baze.
/// </summary>
public sealed record UlazOtkazivanja
{
    /// <summary>Termin preuzimanja, od kojeg se mjeri koliko je otkazivanje rano.</summary>
    public required DateTime DatumOd { get; init; }

    /// <summary>Trenutak otkazivanja. Prosljedjuje ga pozivalac, da test ne ovisi o satu.</summary>
    public required DateTime Sada { get; init; }

    /// <summary>True kad otkazuje osoblje agencije, false kad otkazuje klijent.</summary>
    public required bool OtkazujeAgencija { get; init; }

    /// <summary>Zbir stvarno naplacenih iznosa iz uspjesnih placanja.</summary>
    public required decimal Naplaceno { get; init; }

    /// <summary>Depozit kako je zapisan na rezervaciji.</summary>
    public required decimal IznosDepozita { get; init; }

    /// <summary>Ono sto je po ovoj rezervaciji vec vraceno, da se povrat ne ponovi.</summary>
    public decimal VecVraceno { get; init; }
}
