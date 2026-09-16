using Microsoft.Extensions.Logging;
using SunnyRides.Model.Enums;
using SunnyRides.Services.Auth;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Rezervacije;

public class RezervacijaStateMachine : IRezervacijaStateMachine
{
    private readonly ICurrentUserService _trenutniKorisnik;
    private readonly ILogger<RezervacijaStateMachine> _logger;

    public RezervacijaStateMachine(
        ICurrentUserService trenutniKorisnik, ILogger<RezervacijaStateMachine> logger)
    {
        _trenutniKorisnik = trenutniKorisnik;
        _logger = logger;
    }

    public void Promijeni(
        Rezervacija rezervacija, StatusRezervacije noviStatus, string opis, string? razlog = null)
    {
        var stari = rezervacija.Status;

        if (!PrelaziRezervacije.JeDozvoljen(stari, noviStatus))
        {
            throw new BusinessException(PrelaziRezervacije.PorukaOdbijanja(stari, noviStatus));
        }

        rezervacija.Status = noviStatus;

        DodajZapis(rezervacija, stari, noviStatus, opis, razlog);

        _logger.LogInformation(
            "Rezervacija {Broj}: {Stari} -> {Novi} ({Opis})",
            rezervacija.Broj, stari, noviStatus, opis);
    }

    public void ZabiljeziKreiranje(Rezervacija rezervacija, string opis)
    {
        DodajZapis(rezervacija, statusIz: null, rezervacija.Status, opis, razlog: null);
    }

    /// <summary>
    /// Audit zapis nosi cetiri stvari koje uputstvo trazi: ko, kada, razlog i opis.
    ///
    /// Izvrsilac se cita iz tokena i smije biti prazan - periodicni posao u workeru
    /// otkazuje istekle rezervacije bez ijednog prijavljenog korisnika, i tada je
    /// tacno reci da to nije uradio niko nego sistem.
    /// </summary>
    private void DodajZapis(
        Rezervacija rezervacija,
        StatusRezervacije? statusIz,
        StatusRezervacije statusU,
        string opis,
        string? razlog)
    {
        rezervacija.HistorijaStatusa.Add(new HistorijaStatusaRezervacije
        {
            StatusIz = statusIz,
            StatusU = statusU,
            Opis = opis,
            Razlog = razlog,
            IzvrsioKorisnikId = _trenutniKorisnik.KorisnikId,
            DatumVrijeme = DateTime.UtcNow
        });
    }
}
