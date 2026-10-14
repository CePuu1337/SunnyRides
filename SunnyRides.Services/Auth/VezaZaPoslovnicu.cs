using SunnyRides.Model.Konstante;

namespace SunnyRides.Services.Auth;

/// <summary>
/// Ko je vezan za jednu poslovnicu.
///
/// Uposlenik jeste. Administrator nije, ni kad uz to ima i ulogu uposlenika - on vidi
/// cijelu agenciju. Klijent nije vezan ni za jednu poslovnicu.
/// </summary>
public static class VezaZaPoslovnicu
{
    public static bool JeVezan(IEnumerable<string> uloge)
    {
        var lista = uloge as ICollection<string> ?? uloge.ToList();

        return lista.Contains(Uloge.Uposlenik) && !lista.Contains(Uloge.Administrator);
    }
}
