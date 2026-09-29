namespace SunnyRides.Services.Rezervacije;

/// <summary>Ono sto pravila zamjene gledaju na vozilu, bez baze.</summary>
public record VoziloZaZamjenu(int Id, bool Aktivno, int TipVozilaId, decimal DnevnaTarifa, int GradId);

/// <summary>
/// Kada vozilo smije zamijeniti rezervisano: isti tip, iste ili vise dnevne tarife, u
/// istom gradu. Klijent ne smije dobiti losije vozilo od onoga za koje je platio, niti
/// biti poslan u drugi grad.
///
/// Cista funkcija, pa je pokrivaju unit testovi. Iste uslove lista zamjenskih vozila
/// postavlja kao upit na bazi, a pri samoj zamjeni se provjeravaju ovdje - jer zahtjev
/// za zamjenu moze stici i sa vozilom koje lista nikad nije ponudila.
/// </summary>
public static class PravilaZamjeneVozila
{
    /// <summary>Zasto zamjena nije dozvoljena, ili null ako jeste.</summary>
    public static string? RazlogOdbijanja(VoziloZaZamjenu rezervisano, VoziloZaZamjenu zamjensko)
    {
        if (zamjensko.Id == rezervisano.Id)
        {
            return "Zamjensko vozilo mora biti drugo vozilo.";
        }

        if (!zamjensko.Aktivno)
        {
            return "Odabrano vozilo je povuceno iz ponude.";
        }

        if (zamjensko.TipVozilaId != rezervisano.TipVozilaId)
        {
            return "Zamjensko vozilo mora biti istog tipa kao rezervisano.";
        }

        if (zamjensko.DnevnaTarifa < rezervisano.DnevnaTarifa)
        {
            return "Zamjensko vozilo ne smije biti nize klase od rezervisanog - njegova dnevna " +
                   $"tarifa je {zamjensko.DnevnaTarifa:0.00} EUR, a rezervisanog {rezervisano.DnevnaTarifa:0.00} EUR.";
        }

        if (zamjensko.GradId != rezervisano.GradId)
        {
            return "Zamjensko vozilo mora biti u istom gradu kao rezervisano.";
        }

        return null;
    }
}
