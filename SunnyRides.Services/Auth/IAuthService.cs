using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;

namespace SunnyRides.Services.Auth;

public interface IAuthService
{
    Task<PrijavaOdgovorDto> PrijaviAsync(LoginRequest request, CancellationToken ct = default);

    Task<KorisnikDto> RegistrujAsync(RegisterRequest request, CancellationToken ct = default);

    Task PromijeniLozinkuAsync(PromjenaLozinkeRequest request, CancellationToken ct = default);

    Task OdjaviAsync(CancellationToken ct = default);

    Task<KorisnikDto> TrenutniKorisnikAsync(CancellationToken ct = default);
}
