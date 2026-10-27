namespace SunnyRides.Model.DTOs;

/// <summary>
/// Jedno vozilo iz flote. Lista i detalji vracaju isti oblik - galerija fotografija
/// ne ulazi ovdje nego se dohvata zasebno, sa /api/vozila/{id}/slike.
///
/// Razlog je pravilo iz uputstva: list endpoint ne smije vuci velike podatke. Kartica
/// u listi treba jednu malu sliku, a ne cijelu galeriju, pa je ovdje samo
/// <see cref="ThumbnailUrl"/>.
/// </summary>
public class VoziloDto
{
    public int Id { get; set; }
    public int ModelVozilaId { get; set; }
    public int PoslovnicaId { get; set; }
    public string RegistarskaOznaka { get; set; } = null!;
    public int GodinaProizvodnje { get; set; }
    public int Kilometraza { get; set; }
    public bool Aktivno { get; set; }
    public decimal SatnaTarifa { get; set; }
    public decimal DnevnaTarifa { get; set; }
    public decimal IznosDepozita { get; set; }

    public string? ModelNaziv { get; set; }
    public string? MarkaNaziv { get; set; }
    public string? TipVozilaNaziv { get; set; }
    public string? TipGorivaNaziv { get; set; }

    /// <summary>Vozilo na struju. Prikaz po ovome bira izmedju kubikaze i snage, i izmedju goriva i baterije.</summary>
    public bool JeElektricno { get; set; }
    public int Kubikaza { get; set; }
    public decimal SnagaKw { get; set; }

    /// <summary>Kategorija dozvole potrebna za ovo vozilo. Nasljedjuje se iz modela.</summary>
    public string? KategorijaDozvoleOznaka { get; set; }
    public int KategorijaDozvoleId { get; set; }

    public string? PoslovnicaNaziv { get; set; }
    public string? GradNaziv { get; set; }

    /// <summary>
    /// Zracna udaljenost poslovnice vozila od klijenta, u kilometrima. Popunjena samo u
    /// pretrazi i samo kad je poznato odakle se mjeri - od lokacije telefona ili od
    /// grada iz profila.
    /// </summary>
    public double? UdaljenostKm { get; set; }

    /// <summary>URL male slike, nikad sadrzaj slike. Prazno ako vozilo nema glavnu fotografiju.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Prosjecna ocjena iz neskrivenih recenzija, zaokruzena na jednu decimalu.
    /// Prazno kad vozilo jos nije ocijenjeno - nula bi se citala kao losa ocjena.
    /// </summary>
    public double? ProsjecnaOcjena { get; set; }

    /// <summary>
    /// Cijena za termin iz pretrage. Popunjena samo kad pretraga ima termin - bez
    /// njega nema perioda za koji bi se cijena racunala, pa vazi dnevna cijena.
    /// </summary>
    public CijenaPeriodaDto? CijenaZaPeriod { get; set; }

    /// <summary>
    /// Cijena po satu i po danu koju klijent placa na dan preuzimanja (ili danas, bez
    /// termina), sa sezonom iz cjenovnika. <see cref="DnevnaTarifa"/> je osnovna tarifa
    /// upisana na vozilu i nju vidi osoblje; klijentu se prikazuje ova. Popunjena u
    /// pretrazi i preporukama.
    /// </summary>
    public TarifaNaDanDto? Tarifa { get; set; }

    /// <summary>Broj recenzija iza prosjeka. Bez njega ocjena 5,0 iz jedne recenzije izgleda isto kao iz pedeset.</summary>
    public int BrojRecenzija { get; set; }
}
