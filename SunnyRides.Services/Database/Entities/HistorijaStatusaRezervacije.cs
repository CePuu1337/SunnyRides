using SunnyRides.Model.Enums;

namespace SunnyRides.Services.Database.Entities;

/// <summary>Audit trag svakog prelaza statusa: ko, kada, razlog, opis. Upisuje ga RezervacijaStateMachine.</summary>
public class HistorijaStatusaRezervacije
{
    public int Id { get; set; }
    public int RezervacijaId { get; set; }
    public StatusRezervacije? StatusIz { get; set; }
    public StatusRezervacije StatusU { get; set; }
    public string? Razlog { get; set; }
    public string Opis { get; set; } = null!;
    public int? IzvrsioKorisnikId { get; set; }
    public DateTime DatumVrijeme { get; set; }

    public Rezervacija Rezervacija { get; set; } = null!;
    public Korisnik? IzvrsioKorisnik { get; set; }
}
