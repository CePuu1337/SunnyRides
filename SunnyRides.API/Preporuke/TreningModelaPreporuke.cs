using SunnyRides.Services.Preporuke.Ml;

namespace SunnyRides.API.Preporuke;

/// <summary>
/// Trenira model preporuke u pozadini: odmah po pokretanju API-ja, pa svakih sest sati.
///
/// Ranije se model trenirao pri prvom zahtjevu za preporukama, i taj zahtjev je cekao
/// kraj treniranja. Treniranje sa odabirom parametara i ugnijezdenom unakrsnom provjerom
/// je nekoliko stotina kratkih ucenja, pa je pocetni ekran mobilne aplikacije poslije
/// svakog restarta visio dok se ono ne zavrsi. Sada zahtjev nikad ne ceka: dok model
/// nije spreman, preporuke idu rezervnim putem, koji je u odgovoru i oznacen kao takav.
///
/// Ovo nije posao koji pripada workeru. Model zivi u memoriji API-ja i predikcija se
/// racuna u istom procesu koji odgovara na zahtjev - istreniran u drugom procesu, ovdje
/// ga ne bi bilo.
/// </summary>
public class TreningModelaPreporuke : BackgroundService
{
    /// <summary>
    /// Koliko dugo istreniran model vazi prije nego se ponovo trenira. Nove ocjene i
    /// najmovi do tada ne uticu na predikciju - to je cijena toga sto se model ne trenira
    /// pri svakom zahtjevu. Administrator moze pokrenuti treniranje odmah, kroz
    /// POST /api/preporuke/model/treniraj.
    /// </summary>
    private static readonly TimeSpan RazmakTreniranja = TimeSpan.FromHours(6);

    /// <summary>Ako treniranje pukne, novi pokusaj ide ranije nego redovno osvjezavanje.</summary>
    private static readonly TimeSpan RazmakPoslijeGreske = TimeSpan.FromMinutes(5);

    private readonly IModelPreporuke _model;
    private readonly ILogger<TreningModelaPreporuke> _logger;

    public TreningModelaPreporuke(IModelPreporuke model, ILogger<TreningModelaPreporuke> logger)
    {
        _model = model;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var razmak = RazmakTreniranja;

            try
            {
                await _model.TrenirajAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Pukao trening ne smije oboriti API. Preporuke i dalje rade rezervnim
                // putem, a pokusaj se ponavlja za nekoliko minuta.
                _logger.LogError(ex,
                    "Treniranje modela preporuke nije uspjelo. Novi pokusaj za {Minuta} min.",
                    RazmakPoslijeGreske.TotalMinutes);

                razmak = RazmakPoslijeGreske;
            }

            try
            {
                await Task.Delay(razmak, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
