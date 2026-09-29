using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;

namespace SunnyRides.Services.Rezervacije;

/// <summary>
/// Zamjena vozila na postojecoj rezervaciji, kad dogovoreno vozilo ode u kvar ili na
/// servis. Uz otkazivanje sa punim povratom, to je drugi izbor koji agencija ima za
/// rezervaciju pogodjenu blokadom.
/// </summary>
public interface IZamjenaVozilaService
{
    /// <summary>
    /// Vozila koja mogu preuzeti rezervaciju: slobodna u njenom terminu, istog tipa,
    /// iste ili vise dnevne tarife, u istom gradu, i dozvoljena klijentu po dozvoli.
    /// </summary>
    Task<List<VoziloDto>> ZamjenskaVozilaAsync(int rezervacijaId, CancellationToken ct = default);

    /// <summary>
    /// Prebacuje rezervaciju na drugo vozilo. Cijena ostaje ista, status se ne mijenja,
    /// a zamjena ide u historiju rezervacije i klijentu u obavjestenje.
    /// </summary>
    Task<RezervacijaDto> ZamijeniAsync(
        int rezervacijaId, ZamjenaVozilaRequest request, CancellationToken ct = default);
}
