namespace SunnyRides.Model.Enums;

/// <summary>
/// Cime je preporuka izracunata.
///
/// Vraca se uz svaku stavku, da se nikad ne pomijesa ono sto je model naucio sa onim
/// sto je izracunato pravilom. Bez ovog polja se iz samog odgovora ne bi moglo
/// razlikovati jedno od drugog.
/// </summary>
public enum MetodaPreporuke
{
    /// <summary>Predikcija istreniranog modela matricne faktorizacije.</summary>
    MatricnaFaktorizacija = 1,

    /// <summary>
    /// Rezervni put za korisnika o kojem model nema podataka: ponderisano poklapanje
    /// profila i popularnost vozila.
    /// </summary>
    RezervnaHeuristika = 2
}
