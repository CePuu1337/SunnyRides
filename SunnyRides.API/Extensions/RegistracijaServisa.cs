using SunnyRides.API.Auth;
using SunnyRides.Services.Auth;
using SunnyRides.Services.Sifrarnici;

namespace SunnyRides.API.Extensions;

/// <summary>
/// Registracija servisa na jednom mjestu, da Program.cs ostane citljiv kad ih
/// bude dvadesetak.
/// </summary>
public static class RegistracijaServisa
{
    public static IServiceCollection DodajServise(this IServiceCollection services, JwtPostavke jwtPostavke)
    {
        // Iste postavke koje su iskoristene za konfiguraciju validacije tokena
        // dijele se i servisu koji token izdaje. Kljuc se cita iz okruzenja tacno
        // jednom, pri pokretanju.
        services.AddSingleton(jwtPostavke);

        // TokenService nema stanja i ne dira bazu, pa je singleton.
        services.AddSingleton<ITokenService, TokenService>();

        // CurrentUserService cita iskljucivo iz ClaimsPrincipal-a trenutnog zahtjeva.
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IAuthService, AuthService>();

        // Svi servisi koji koriste DbContext registruju se kao Scoped, nikad Transient.
        // DbContext nije siguran za istovremeno koristenje iz vise niti, a Transient
        // bi ga u istom zahtjevu umnozio.
        services.AddScoped<IDrzavaService, DrzavaService>();

        return services;
    }
}
