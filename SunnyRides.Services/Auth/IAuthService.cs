using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;

namespace SunnyRides.Services.Auth;

public interface IAuthService
{
    Task<PrijavaOdgovorDto> PrijaviAsync(LoginRequest request, CancellationToken ct = default);

    Task<KorisnikDto> RegistrujAsync(RegisterRequest request, CancellationToken ct = default);

    Task PromijeniLozinkuAsync(PromjenaLozinkeRequest request, CancellationToken ct = default);

    /// <summary>
    /// Trazi kod za reset lozinke. Odgovor je isti bez obzira na to postoji li nalog
    /// sa tom email adresom - inace bi ovaj put bio spisak registrovanih korisnika.
    /// </summary>
    Task ZatraziResetAsync(ZaboravljenaLozinkaRequest request, CancellationToken ct = default);

    /// <summary>Postavlja novu lozinku uz kod iz emaila. Kod vazi jednom i kratko.</summary>
    Task PotvrdiResetAsync(ResetLozinkeRequest request, CancellationToken ct = default);

    Task OdjaviAsync(CancellationToken ct = default);

    Task<KorisnikDto> TrenutniKorisnikAsync(CancellationToken ct = default);
}
