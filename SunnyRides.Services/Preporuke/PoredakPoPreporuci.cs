namespace SunnyRides.Services.Preporuke;

/// <summary>Vozilo iz rezultata pretrage sa skorom koji mu je dao sistem preporuke.</summary>
public record RangiranoVozilo(int VoziloId, int ModelVozilaId, double Skor);

/// <summary>
/// Redoslijed rezultata pretrage kad je odabran poredak "Preporuceno za vas".
///
/// Flota ima po nekoliko primjeraka istog modela, u razlicitim poslovnicama. Da se
/// poreda samo po skoru, primjerci istog modela bi stajali jedan do drugog - model
/// preporuke ocjenjuje model vozila, pa svi njegovi primjerci dobiju isti skor - i
/// korisnik bi na vrhu vidio tri puta isti skuter. Zato lista ide u krugovima: prvo
/// najbolji primjerak svakog modela, poredani po skoru, pa drugi primjerak svakog
/// modela, i tako dalje. Nijedno vozilo se ne izostavlja, samo se ponavljanja spustaju.
///
/// Racun je cist i bez baze, pa ga pokrivaju unit testovi.
/// </summary>
public static class PoredakPoPreporuci
{
    public static List<int> Poredaj(IEnumerable<RangiranoVozilo> vozila)
    {
        return vozila
            .GroupBy(x => x.ModelVozilaId)
            .SelectMany(model => model
                .OrderByDescending(x => x.Skor)
                .ThenBy(x => x.VoziloId)
                .Select((vozilo, krug) => (Vozilo: vozilo, Krug: krug)))
            .OrderBy(x => x.Krug)
            .ThenByDescending(x => x.Vozilo.Skor)

            // Kod jednakog skora odlucuje identifikator, da dva uzastopna upita vrate
            // isti redoslijed - bez toga bi se stranice pri listanju mijesale.
            .ThenBy(x => x.Vozilo.VoziloId)
            .Select(x => x.Vozilo.VoziloId)
            .ToList();
    }
}
