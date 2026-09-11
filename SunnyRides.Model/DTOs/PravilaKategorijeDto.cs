namespace SunnyRides.Model.DTOs;

public class PravilaKategorijeDto
{
    public int Id { get; set; }
    public int KategorijaDozvoleId { get; set; }
    public int TipVozilaId { get; set; }
    public int? MaxKubikaza { get; set; }
    public decimal? MaxSnagaKw { get; set; }
    public int MinGodine { get; set; }

    public string? KategorijaDozvoleOznaka { get; set; }
    public string? TipVozilaNaziv { get; set; }
}
