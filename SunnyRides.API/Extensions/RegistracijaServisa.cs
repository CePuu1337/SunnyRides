using SunnyRides.Services.Sifrarnici;

namespace SunnyRides.API.Extensions;

/// <summary>
/// Registracija servisa na jednom mjestu, da Program.cs ostane citljiv kad ih
/// bude dvadesetak.
/// </summary>
public static class RegistracijaServisa
{
    public static IServiceCollection DodajServise(this IServiceCollection services)
    {
        // Svi servisi koji koriste DbContext registruju se kao Scoped, nikad Transient.
        // DbContext nije siguran za istovremeno koristenje iz vise niti, a Transient
        // bi ga u istom zahtjevu umnozio.
        services.AddScoped<IDrzavaService, DrzavaService>();

        return services;
    }
}
