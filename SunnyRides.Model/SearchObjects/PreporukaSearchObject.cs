namespace SunnyRides.Model.SearchObjects;

/// <summary>
/// Okvir u kojem se traze preporuke.
///
/// Nema polja za korisnika ni za kategorije dozvole - i jedno i drugo server izvodi
/// iz tokena. Da klijent salje kategorije, poslao bi one koje mu odgovaraju.
/// </summary>
public class PreporukaSearchObject : BaseSearchObject
{
    /// <summary>
    /// Termin za koji vozilo mora biti slobodno. Oba polja idu zajedno - jedan datum
    /// ne opisuje period. Kad ih nema, preporuke se ne filtriraju po dostupnosti.
    /// </summary>
    public DateTime? SlobodnoOd { get; set; }
    public DateTime? SlobodnoDo { get; set; }

    public int? PoslovnicaId { get; set; }
    public int? GradId { get; set; }
    public int? TipVozilaId { get; set; }
}
