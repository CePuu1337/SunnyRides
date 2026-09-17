using SunnyRides.Model.Enums;

namespace SunnyRides.Model.DTOs;

/// <summary>Jedan povrat novca uz placanje. Jedno placanje moze imati vise povrata.</summary>
public class PovratDto
{
    public int Id { get; set; }
    public decimal Iznos { get; set; }
    public string Razlog { get; set; } = null!;

    /// <summary>
    /// Created znaci da je povrat odobren u sistemu, ali Stripe jos nije potvrdio
    /// da ga je primio. Failed znaci da ga je Stripe odbio i da ga osoblje moze
    /// pokusati ponovo.
    /// </summary>
    public StatusPlacanja Status { get; set; }

    public string? ProviderRefundId { get; set; }
    public string? KreiraoKorisnikIme { get; set; }
    public DateTime DatumKreiranja { get; set; }
}
