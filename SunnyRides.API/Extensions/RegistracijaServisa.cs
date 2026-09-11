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
        DodajSifrarnike(services);

        return services;
    }

    /// <summary>
    /// Sifrarnici, redoslijedom kojim zavise jedan od drugog: drzava nosi grad,
    /// grad nosi poslovnicu, a marka, tip vozila, tip goriva i kategorija dozvole
    /// zajedno cine model vozila.
    /// </summary>
    private static void DodajSifrarnike(IServiceCollection services)
    {
        services.AddScoped<IDrzavaService, DrzavaService>();
        services.AddScoped<IGradService, GradService>();
        services.AddScoped<IPoslovnicaService, PoslovnicaService>();
        services.AddScoped<ITipVozilaService, TipVozilaService>();
        services.AddScoped<IMarkaService, MarkaService>();
        services.AddScoped<ITipGorivaService, TipGorivaService>();
        services.AddScoped<IKategorijaDozvoleService, KategorijaDozvoleService>();
        services.AddScoped<IModelVozilaService, ModelVozilaService>();
        services.AddScoped<IPravilaKategorijeService, PravilaKategorijeService>();
        services.AddScoped<IVrstaOpremeService, VrstaOpremeService>();
        services.AddScoped<IPaketOsiguranjaService, PaketOsiguranjaService>();
    }
}
