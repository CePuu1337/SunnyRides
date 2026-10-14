namespace SunnyRides.Services.Lokacija;

/// <summary>
/// Tacka od koje se mjeri udaljenost do poslovnica.
/// </summary>
/// <param name="PoLokaciji">Tacka je trenutna lokacija telefona, a ne grad iz profila.</param>
/// <param name="GradNaziv">Naziv grada kad je tacka grad iz profila.</param>
public record Polaziste(double Latituda, double Longituda, bool PoLokaciji, string? GradNaziv);

/// <summary>
/// Odakle je klijent i koliko su mu daleko poslovnice.
///
/// Jedno mjesto za tu odluku, da pretraga i kartica najblize poslovnice ne bi mjerile
/// od razlicitih tacaka.
/// </summary>
public interface ILokacijaKlijenta
{
    /// <summary>
    /// Lokacija telefona ako ju je klijent poslao, inace sredina grada iz njegovog
    /// profila. Prazno kad nije poznato ni jedno - na primjer za osoblje, ili za grad
    /// kojem jos nisu unesene koordinate.
    /// </summary>
    Task<Polaziste?> PolazisteAsync(double? latituda, double? longituda, CancellationToken ct = default);

    /// <summary>Udaljenost svake poslovnice koja ima koordinate, u kilometrima, po identifikatoru poslovnice.</summary>
    Task<Dictionary<int, double>> UdaljenostiPoslovnicaAsync(Polaziste polaziste, CancellationToken ct = default);
}
