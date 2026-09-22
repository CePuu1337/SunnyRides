namespace SunnyRides.Model.Enums;

/// <summary>
/// Strana vozacke dozvole na fotografiji.
///
/// Dozvola se fotografise s obje strane: na prednjoj su ime, broj i rok, na zadnjoj
/// kategorije i datumi po kategoriji. Jedna strana nije dovoljna da se dozvola
/// provjeri, pa se i cuvaju kao dvije odvojene fotografije.
/// </summary>
public enum StranaDozvole
{
    Prednja = 1,
    Zadnja = 2
}
