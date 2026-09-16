using SunnyRides.Model.Enums;

namespace SunnyRides.Model.DTOs;

/// <summary>
/// Jedan prelaz statusa rezervacije. Cita se na ekranu sa detaljima, kao trag toga
/// sta se sa rezervacijom dogadjalo i ko je sta uradio.
/// </summary>
public class HistorijaStatusaDto
{
    public int Id { get; set; }

    /// <summary>Prazno na prvom zapisu - rezervacija tada jos nije imala prethodni status.</summary>
    public StatusRezervacije? StatusIz { get; set; }

    public StatusRezervacije StatusU { get; set; }

    public string Opis { get; set; } = null!;

    /// <summary>Popunjen kod otkazivanja i odbijanja; kod ostalih prelaza nema sta objasniti.</summary>
    public string? Razlog { get; set; }

    public DateTime DatumVrijeme { get; set; }

    /// <summary>Prazno kad je prelaz izvrsio sistem, a ne prijavljen korisnik.</summary>
    public string? IzvrsioKorisnikIme { get; set; }
}
