namespace SunnyRides.Services.Database.Entities;

/// <summary>Mehanizam idempotentnosti za webhook put. Postojanje zapisa znaci da je dogadjaj vec obradjen.</summary>
public class ObradjeniWebhookEvent
{
    public int Id { get; set; }
    public string ProviderEventId { get; set; } = null!;
    public string TipEventa { get; set; } = null!;
    public DateTime DatumObrade { get; set; }
}
