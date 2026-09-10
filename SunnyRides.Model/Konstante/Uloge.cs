namespace SunnyRides.Model.Konstante;

/// <summary>
/// Nazivi uloga na jednom mjestu. Iste vrijednosti koriste se u seed podacima
/// i u [Authorize(Roles = ...)] atributima - ako se raziduju, autorizacija tiho pada.
/// </summary>
public static class Uloge
{
    public const string Administrator = "Administrator";
    public const string Uposlenik = "Uposlenik";
    public const string Klijent = "Klijent";

    public const string AdministratorIliUposlenik = Administrator + "," + Uposlenik;
}
