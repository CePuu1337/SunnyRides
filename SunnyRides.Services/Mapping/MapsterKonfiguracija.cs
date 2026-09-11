using Mapster;

namespace SunnyRides.Services.Mapping;

/// <summary>
/// Mapster radi po konvenciji - svojstva istog naziva se poklapaju sama. Ovdje se
/// registruju samo izuzeci od te konvencije. Poziva se jednom, pri pokretanju.
/// </summary>
public static class MapsterKonfiguracija
{
    public static void Registruj()
    {
        // Kod izmjene se preskacu vrijednosti koje nisu poslane, da djelimicna
        // izmjena ne obrise polja koja korisnik nije ni dirao.
        TypeAdapterConfig.GlobalSettings.Default.IgnoreNullValues(true);
    }
}
