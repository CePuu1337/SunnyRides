namespace SunnyRides.Model.SearchObjects;

public class VoziloSearchObject : BaseSearchObject
{
    /// <summary>
    /// Tekst pretrage po marki i modelu, npr. "Honda", "PCX" ili "Honda PCX". Svaka
    /// rijec mora postojati u nazivu marke ili modela.
    /// </summary>
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

    /// <summary>
    /// Filter osoblja u administraciji flote; prazno vraca i aktivna i deaktivirana.
    /// Klijentu server uvijek prikazuje samo aktivna, bez obzira na ovu vrijednost.
    /// </summary>
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

    /// <summary>
    /// Trenutna lokacija telefona, kad je klijent dozvoli. Udaljenost do poslovnica se
    /// tada racuna od nje, a bez nje od grada iz profila. Lokacija se nigdje ne cuva,
    /// sluzi samo ovom upitu.
    /// </summary>
    public double? Latituda { get; set; }
    public double? Longituda { get; set; }
}
