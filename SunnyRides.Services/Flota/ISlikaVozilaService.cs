using SunnyRides.Model.DTOs;

namespace SunnyRides.Services.Flota;

/// <summary>
/// Fotografije vozila. Nije obican CRUD - unos prima sadrzaj fajla, a ne JSON, pa
/// ovaj servis ne nasljedjuje genericku bazu.
/// </summary>
public interface ISlikaVozilaService
{
    Task<List<SlikaVozilaDto>> ZaVoziloAsync(int voziloId, CancellationToken ct = default);

    Task<SlikaVozilaDto> DodajAsync(
        int voziloId, Stream sadrzaj, long duzinaBajta, CancellationToken ct = default);

    Task<SlikaVozilaDto> PostaviGlavnuAsync(int voziloId, int slikaId, CancellationToken ct = default);

    Task ObrisiAsync(int voziloId, int slikaId, CancellationToken ct = default);
}
