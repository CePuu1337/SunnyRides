using System.Globalization;

namespace SunnyRides.Services.Vrijeme;

/// <summary>
/// Prikaz vremena u tekstu koji cita covjek - obavjestenja, emailovi, poruke o gresci.
///
/// Sva vremena u bazi su UTC i tako ostaju; ovdje se pretvaraju tek pri ispisu. Klijentu
/// koji preuzima vozilo u 16:00 ne pomaze recenica "u 14:00 (UTC)". Zona se cita iz
/// varijable VREMENSKA_ZONA (podrazumijevano Europe/Sarajevo), jednom, pri prvom
/// koristenju. Ako je sistem ne poznaje, ispis ostaje u UTC-u i to jasno pise uz vrijeme,
/// da se pogresno vrijeme ne bi predstavilo kao lokalno.
/// </summary>
public static class LokalnoVrijeme
{
    private const string PodrazumijevanaZona = "Europe/Sarajevo";

    private static readonly TimeZoneInfo? Zona = UcitajZonu();

    /// <summary>UTC vrijeme iz baze u vremenu agencije.</summary>
    public static DateTime IzUtc(DateTime utc)
    {
        var kaoUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        return Zona is null ? kaoUtc : TimeZoneInfo.ConvertTimeFromUtc(kaoUtc, Zona);
    }

    /// <summary>Npr. "30.09.2026. 16:00".</summary>
    public static string DatumIVrijeme(DateTime utc) =>
        IzUtc(utc).ToString("dd.MM.yyyy. HH:mm", CultureInfo.InvariantCulture) + Oznaka;

    /// <summary>Npr. "30.09.2026. u 16:00".</summary>
    public static string DatumUVrijeme(DateTime utc) =>
        IzUtc(utc).ToString("dd.MM.yyyy. 'u' HH:mm", CultureInfo.InvariantCulture) + Oznaka;

    /// <summary>Npr. "16:00".</summary>
    public static string Vrijeme(DateTime utc) =>
        IzUtc(utc).ToString("HH:mm", CultureInfo.InvariantCulture) + Oznaka;

    /// <summary>Npr. "30.09.2026.".</summary>
    public static string Datum(DateTime utc) =>
        IzUtc(utc).ToString("dd.MM.yyyy.", CultureInfo.InvariantCulture);

    /// <summary>Prazno kad je zona poznata; inace se uz vrijeme pise da je u UTC-u.</summary>
    private static string Oznaka => Zona is null ? " (UTC)" : string.Empty;

    private static TimeZoneInfo? UcitajZonu()
    {
        var naziv = Environment.GetEnvironmentVariable("VREMENSKA_ZONA");

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                string.IsNullOrWhiteSpace(naziv) ? PodrazumijevanaZona : naziv.Trim());
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Ispis ostaje u UTC-u, uz oznaku. Vrijeme je i dalje tacno, samo nije lokalno.
            return null;
        }
    }
}
