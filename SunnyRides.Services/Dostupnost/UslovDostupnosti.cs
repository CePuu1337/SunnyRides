using System.Linq.Expressions;
using SunnyRides.Model.Enums;
using SunnyRides.Services.Database.Entities;

namespace SunnyRides.Services.Dostupnost;

/// <summary>
/// Granice perioda koje ulaze u provjeru dostupnosti.
///
/// Buffer postoji zato sto vozilo izmedju dva najma treba oprati, dopuniti gorivo i
/// pregledati. Bez njega bi sistem dozvolio da jedan klijent vrati skuter u 10:00, a
/// drugi ga preuzme u 10:00 istog trenutka.
///
/// Sve sto racuna sa bufferom prolazi kroz ove dvije metode. Da se oduzimanje i
/// dodavanje pisu na svakom mjestu gdje se gradi upit, prvi propusteni buffer bio bi
/// dvostruko ugovoren termin - a to je tacno problem zbog kojeg ovaj sistem postoji.
/// </summary>
public static class UslovDostupnosti
{
    public static readonly TimeSpan Buffer = TimeSpan.FromHours(2);

    /// <summary>Donja granica preklapanja: postojeci najam smeta ako se zavrsava poslije ovog trenutka.</summary>
    public static DateTime GranicaOd(DateTime trazeniOd) => trazeniOd - Buffer;

    /// <summary>Gornja granica preklapanja: postojeci najam smeta ako pocinje prije ovog trenutka.</summary>
    public static DateTime GranicaDo(DateTime trazeniDo) => trazeniDo + Buffer;

    /// <summary>
    /// Rezervacija koja drzi svoj termin - i vozilo i opremu uz njega.
    ///
    /// Otkazana ne drzi nista, a zavrsena je proslost. Neplacena drzi samo dok joj
    /// traje drzanje; kad istekne, termin je slobodan i prije nego je worker formalno
    /// otkaze. Isti uslov koriste provjera vozila i provjera opreme, da jedno ne bi
    /// vidjelo rezervaciju koju drugo ne vidi.
    /// </summary>
    public static Expression<Func<Rezervacija, bool>> ZauzimaTermin(DateTime sada) =>
        r => r.Status == StatusRezervacije.Confirmed
             || (r.Status == StatusRezervacije.Pending && r.DrziDo > sada);
}
