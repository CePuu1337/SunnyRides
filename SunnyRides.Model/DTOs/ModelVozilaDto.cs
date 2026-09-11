namespace SunnyRides.Model.DTOs;

public class ModelVozilaDto
{
    public int Id { get; set; }
    public int MarkaId { get; set; }
    public int TipVozilaId { get; set; }
    public int TipGorivaId { get; set; }
    public int KategorijaDozvoleId { get; set; }
    public string Naziv { get; set; } = null!;
    public int Kubikaza { get; set; }
    public decimal SnagaKw { get; set; }

    public string? MarkaNaziv { get; set; }
    public string? TipVozilaNaziv { get; set; }
    public string? TipGorivaNaziv { get; set; }

    /// <summary>Oznaka kategorije potrebne za upravljanje ovim modelom, npr. "A1".</summary>
    public string? KategorijaDozvoleOznaka { get; set; }
}
