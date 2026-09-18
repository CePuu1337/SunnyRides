using System.Security.Cryptography;

namespace SunnyRides.Services.Auth;

/// <summary>
/// Kod koji klijent dobija na email kad zaboravi lozinku.
///
/// Klasa je namjerno bez zavisnosti, pa se moze provjeriti kao obicna funkcija.
/// Kod se generise kroz <see cref="RandomNumberGenerator"/>, nikad kroz
/// <c>System.Random</c> - Random je predvidiv i iz nekoliko poznatih vrijednosti
/// se moze izracunati sljedeca, a ovdje ta vrijednost otvara tudji nalog.
/// </summary>
public static class KodoviZaReset
{
    /// <summary>
    /// Osam znakova iz abecede od 32 znaka daje nesto vise od 10^12 kombinacija.
    /// Sest cifara, koliko se obicno vidi, ima ih milion - a milion pokusaja kroz
    /// API je izvodljivo. Zato kod ovdje nije brojcani PIN.
    /// </summary>
    public const int Duzina = 8;

    /// <summary>Abeceda bez znakova koji se mijesaju pri prepisivanju: I, O, 0 i 1.</summary>
    private const string Abeceda = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Koliko kod vazi. Dovoljno da se email procita, prekratko da se pogadja.</summary>
    public static readonly TimeSpan Trajanje = TimeSpan.FromMinutes(15);

    public static string Generisi()
    {
        var znakovi = new char[Duzina];

        for (var i = 0; i < Duzina; i++)
        {
            // GetInt32 uzima iz kriptografskog izvora i ne pravi pomak prema pocetku
            // abecede, sto bi se desilo obicnim "slucajan broj % duzina".
            znakovi[i] = Abeceda[RandomNumberGenerator.GetInt32(Abeceda.Length)];
        }

        return new string(znakovi);
    }

    /// <summary>
    /// Kod se prepisuje rukom, pa se mala slova i razmaci ne racunaju kao greska.
    /// Crtica se uklanja jer je aplikacija moze prikazati radi citljivosti.
    /// </summary>
    public static string Normalizuj(string? kod)
    {
        if (string.IsNullOrWhiteSpace(kod))
        {
            return string.Empty;
        }

        var ociscen = kod
            .Where(znak => !char.IsWhiteSpace(znak) && znak != '-')
            .Select(char.ToUpperInvariant)
            .ToArray();

        return new string(ociscen);
    }

    /// <summary>Kod koji ni po obliku ne moze biti nas se odbija bez ijednog upita prema bazi.</summary>
    public static bool JeMogucOblik(string kod) =>
        kod.Length == Duzina && kod.All(znak => Abeceda.Contains(znak));
}
