using SunnyRides.API.Auth;
using SunnyRides.Services.Auth;
using SunnyRides.Services.Cijene;
using SunnyRides.Services.Dostupnost;
using SunnyRides.Services.Dozvole;
using SunnyRides.Services.Fajlovi;
using SunnyRides.Services.Flota;
using SunnyRides.Services.Sifrarnici;

namespace SunnyRides.API.Extensions;

/// <summary>
/// Registracija servisa na jednom mjestu, da Program.cs ostane citljiv kad ih
/// bude dvadesetak.
/// </summary>
public static class RegistracijaServisa
{
    public static IServiceCollection DodajServise(
        this IServiceCollection services, JwtPostavke jwtPostavke, PohranaOpcije pohranaOpcije)
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

        // Pohrana slika ne drzi stanje izmedju zahtjeva i ne koristi DbContext,
        // pa moze biti singleton zajedno sa svojim postavkama.
        services.AddSingleton(pohranaOpcije);
        services.AddSingleton<IPohranaSlika, PohranaSlika>();

        // Svi servisi koji koriste DbContext registruju se kao Scoped, nikad Transient.
        // DbContext nije siguran za istovremeno koristenje iz vise niti, a Transient
        // bi ga u istom zahtjevu umnozio.
        DodajSifrarnike(services);
        DodajFlotu(services);

        // Obracun cijene je jedini put do iznosa - i pregled prije rezervacije i samo
        // kreiranje rezervacije zovu isti servis.
        services.AddScoped<IPricingService, PricingService>();

        // Provjera dostupnosti je jedina implementacija uslova preklapanja. Registruje
        // se prije flote, jer je VoziloService koristi u pretrazi.
        services.AddScoped<IAvailabilityService, AvailabilityService>();

        // Dozvole su jedina implementacija hijerarhije kategorija - koristi ih i
        // pretraga i provjera preduslova pri rezervaciji.
        services.AddScoped<IDozvolaService, DozvolaService>();

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

    private static void DodajFlotu(IServiceCollection services)
    {
        services.AddScoped<IVoziloService, VoziloService>();
        services.AddScoped<ISlikaVozilaService, SlikaVozilaService>();
        services.AddScoped<IBlokadaVozilaService, BlokadaVozilaService>();
        services.AddScoped<ICjenovnikService, CjenovnikService>();
    }
}
