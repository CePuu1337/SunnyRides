using SunnyRides.Model.DTOs;

namespace SunnyRides.Services.Pregled;

/// <summary>
/// Agregati za pocetni ekran administrativne aplikacije.
///
/// Servis samo cita i broji - nista ne mijenja. Svaki broj se racuna na bazi, grupisanim
/// upitom; nigdje se ne ucitava lista zapisa da bi se u memoriji prebrojala.
/// </summary>
public interface IPregledService
{
    Task<PregledPoslovanjaDto> PregledAsync(CancellationToken ct = default);
}
