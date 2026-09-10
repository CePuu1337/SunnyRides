namespace SunnyRides.Services.Database.Entities;

/// <summary>Sta koja kategorija dozvole obuhvata. Pravila su podatak u tabeli, ne if grane u kodu.</summary>
public class PravilaKategorije
{
    public int Id { get; set; }
    public int KategorijaDozvoleId { get; set; }
    public int TipVozilaId { get; set; }
    public int? MaxKubikaza { get; set; }
    public decimal? MaxSnagaKw { get; set; }
    public int MinGodine { get; set; }

    public KategorijaDozvole KategorijaDozvole { get; set; } = null!;
    public TipVozila TipVozila { get; set; } = null!;
}
