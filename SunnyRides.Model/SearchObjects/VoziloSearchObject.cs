namespace SunnyRides.Model.SearchObjects;

public class VoziloSearchObject : BaseSearchObject
{
    /// <summary>Dio naziva modela, npr. "CB125".</summary>
    public string? ModelNaziv { get; set; }

    public string? RegistarskaOznaka { get; set; }

    public int? ModelVozilaId { get; set; }
    public int? MarkaId { get; set; }
    public int? TipVozilaId { get; set; }
    public int? TipGorivaId { get; set; }
    public int? KategorijaDozvoleId { get; set; }
    public int? PoslovnicaId { get; set; }

    /// <summary>Filtriranje po gradu ide kroz poslovnicu, bez dodatne kolone na vozilu.</summary>
    public int? GradId { get; set; }

    public decimal? CijenaOd { get; set; }
    public decimal? CijenaDo { get; set; }

    /// <summary>Prazno vraca i aktivna i deaktivirana vozila. Klijentska aplikacija salje true.</summary>
    public bool? Aktivno { get; set; }

    /// <summary>
    /// Termin za koji vozilo mora biti slobodno. Oba polja idu zajedno - jedan datum
    /// sam po sebi ne opisuje period, pa se ignorise.
    ///
    /// Filtriranje po dostupnosti radi isti servis koji odlucuje i pri kreiranju
    /// rezervacije, pa pretraga ne moze pokazati vozilo koje bi rezervacija odbila.
    /// </summary>
    public DateTime? SlobodnoOd { get; set; }
    public DateTime? SlobodnoDo { get; set; }
}
