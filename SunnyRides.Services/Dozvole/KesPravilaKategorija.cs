using Microsoft.Extensions.Caching.Memory;

namespace SunnyRides.Services.Dozvole;

/// <summary>
/// Jedno mjesto za kljuc pod kojim se kesiraju pravila kategorija.
///
/// Pravila cita <see cref="DozvolaService"/>, a mijenja ih sifarnik
/// (<c>PravilaKategorijeService</c>). Da svako ima svoj string, dovoljna bi bila jedna
/// tipfelerska razlika i kes se nikad ne bi brisao - a to se ni na jednom testu ne
/// bi vidjelo dok neko ne promijeni pravilo i ceka 15 minuta.
/// </summary>
public static class KesPravilaKategorija
{
    public const string Kljuc = "pravila-kategorija";

    public static void Ponisti(IMemoryCache kes) => kes.Remove(Kljuc);
}
