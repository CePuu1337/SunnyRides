using SunnyRides.Model.DTOs;
using SunnyRides.Model.Requests;
using SunnyRides.Model.SearchObjects;
using SunnyRides.Services.Base;

namespace SunnyRides.Services.Obavijesti;

/// <summary>
/// Objave agencije. Pise ih administrator, a citaju svi klijenti.
///
/// Klijentu se prikazuju samo aktivne obavijesti ciji je datum objave prosao. Obje
/// provjere su na serveru - obavijest koja jos nije objavljena ne smije se moci
/// procitati ni namjernim slanjem drugacijeg zahtjeva.
/// </summary>
public interface IObavijestService
    : ICRUDService<ObavijestDto, ObavijestSearchObject, ObavijestInsertRequest, ObavijestUpdateRequest>
{
    Task<ObavijestDto> PostaviSlikuAsync(
        int id, Stream sadrzaj, long duzinaBajta, CancellationToken ct = default);

    Task<ObavijestDto> UkloniSlikuAsync(int id, CancellationToken ct = default);
}
