using SunnyRides.Model.Enums;

namespace SunnyRides.Model.DTOs;

/// <summary>Jedan pokusaj naplate, sa povratima koji su iz njega isplaceni.</summary>
public class PlacanjeDto
{
    public int Id { get; set; }

    public int RezervacijaId { get; set; }
    public string? RezervacijaBroj { get; set; }
    public StatusRezervacije StatusRezervacije { get; set; }
    public bool IsPaid { get; set; }
    public string? KlijentImePrezime { get; set; }

    /// <summary>Iznos koji je server zatrazio.</summary>
    public decimal Iznos { get; set; }

    /// <summary>
    /// Iznos koji je Stripe stvarno primio. Prazan dok placanje nije uspjelo.
    /// Svaki povrat se racuna iz ovog broja, nikad iz cjenovnika.
    /// </summary>
    public decimal? NaplaceniIznos { get; set; }

    public string Valuta { get; set; } = null!;
    public StatusPlacanja Status { get; set; }
    public string Provider { get; set; } = null!;
    public string? ProviderPaymentIntentId { get; set; }
    public DateTime DatumKreiranja { get; set; }
    public DateTime? DatumAzuriranja { get; set; }

    public decimal UkupnoVraceno { get; set; }

    public List<PovratDto> Povrati { get; set; } = new();
}
