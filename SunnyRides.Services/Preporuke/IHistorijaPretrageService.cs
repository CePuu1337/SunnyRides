using SunnyRides.Model.SearchObjects;

namespace SunnyRides.Services.Preporuke;

/// <summary>
/// Biljezenje pretraga. Ovo je jedini ulazni podatak koji sistem preporuke ima o
/// korisniku prije nego on ista rezervise, pa upis nije sporedan posao nego
/// preduslov da preporuke uopste rade.
/// </summary>
public interface IHistorijaPretrageService
{
    Task ZabiljeziAsync(VoziloSearchObject search, CancellationToken ct = default);
}
