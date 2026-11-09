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

    /// <summary>
    /// Naziv zone kakav razumije SQL Server u <c>AT TIME ZONE</c> - on koristi Windows
    /// nazive ("Central European Standard Time"), a ne IANA ("Europe/Sarajevo").
    /// Kad zona nije poznata, "UTC", pa se grupisanje po mjesecu radi po UTC-u.
    /// </summary>
    public static string ZonaZaBazu { get; } = NazivZoneZaBazu();

    /// <summary>UTC vrijeme iz baze u vremenu agencije.</summary>
    public static DateTime IzUtc(DateTime utc)
    {
        var kaoUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        return Zona is null ? kaoUtc : TimeZoneInfo.ConvertTimeFromUtc(kaoUtc, Zona);
    }

    /// <summary>Lokalni datum i vrijeme agencije (bez oznake zone) pretvoreno u UTC.</summary>
    public static DateTime UUtc(DateTime lokalno)
    {
        var bezZone = DateTime.SpecifyKind(lokalno, DateTimeKind.Unspecified);

        return Zona is null
            ? DateTime.SpecifyKind(bezZone, DateTimeKind.Utc)
            : TimeZoneInfo.ConvertTimeToUtc(bezZone, Zona);
    }

    /// <summary>
    /// Kalendarski dan agencije kojem pripada vrijednost iz zahtjeva.
    ///
    /// Aplikacija salje lokalnu ponoc pretvorenu u UTC: 1. oktobar postaje
    /// "30.09. 22:00Z". Odsijecanje sata od toga dalo bi 30. septembar. Zato se UTC
    /// vrijednost prvo vraca u lokalno vrijeme, pa se uzima datum. Vrijednost bez
    /// oznake zone (npr. "2026-10-01" iz Swaggera) je vec lokalni datum.
    /// </summary>
    public static DateTime LokalniDatum(DateTime vrijednost) => vrijednost.Kind switch
    {
        DateTimeKind.Utc => IzUtc(vrijednost).Date,
        DateTimeKind.Local => IzUtc(vrijednost.ToUniversalTime()).Date,
        _ => vrijednost.Date
    };

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

    private static string NazivZoneZaBazu()
    {
        if (Zona is null)
        {
            return "UTC";
        }

        if (!Zona.HasIanaId)
        {
            return Zona.Id;
        }

        return TimeZoneInfo.TryConvertIanaIdToWindowsId(Zona.Id, out var windows) ? windows : "UTC";
    }

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
