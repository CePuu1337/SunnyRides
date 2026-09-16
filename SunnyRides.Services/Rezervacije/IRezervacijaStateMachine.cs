using SunnyRides.Model.Enums;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Rezervacije;

/// <summary>
/// Jedino mjesto gdje se mijenja status rezervacije.
///
/// Nijedan servis ne postavlja <c>rezervacija.Status</c> direktno, a kontroleri ovo
/// ne zovu uopste - zovu servis, servis zove ovo. Da svaki servis mijenja status sam,
/// prvo pravilo tipa "otkazivanje nije moguce ako je vozilo vec izdato" trebalo bi
/// napisati na tri mjesta i na jednom bi se zaboravilo.
///
/// Metode su sinhrone i namjerno **ne** zovu <c>SaveChangesAsync</c>. Promjena statusa
/// je uvijek dio vece operacije - naplate, otkazivanja, povrata vozila - pa transakcijom
/// upravlja onaj ko tu operaciju vodi.
/// </summary>
public interface IRezervacijaStateMachine
{
    /// <summary>
    /// Mijenja status i upisuje audit zapis. Baca <c>BusinessException</c> ako prelaz
    /// nije dozvoljen.
    /// </summary>
    void Promijeni(
        Rezervacija rezervacija, StatusRezervacije noviStatus, string opis, string? razlog = null);

    /// <summary>
    /// Prvi zapis u historiji, za rezervaciju koja tek nastaje. Nema prethodnog
    /// statusa, pa nema ni prelaza koji bi se provjeravao.
    /// </summary>
    void ZabiljeziKreiranje(Rezervacija rezervacija, string opis);
}
