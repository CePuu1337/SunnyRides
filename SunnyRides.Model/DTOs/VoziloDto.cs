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

    /// <summary>URL male slike, nikad sadrzaj slike. Prazno ako vozilo nema glavnu fotografiju.</summary>
    public string? ThumbnailUrl { get; set; }
}
