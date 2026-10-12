namespace SunnyRides.Services.Rezervacije;

/// <summary>Ono sto pravila zamjene gledaju na vozilu, bez baze.</summary>
public record VoziloZaZamjenu(int Id, bool Aktivno, int GradId);

/// <summary>
/// Kada vozilo smije zamijeniti rezervisano: drugo, aktivno vozilo u istom gradu. Tip i
/// cijena se namjerno ne ogranicavaju - kad vozilo otkaze, klijentu je korisnije da bira
/// medju svime sto je slobodno nego da ostane bez najma zato sto istog tipa nema. Cijena
/// rezervacije se zamjenom ne mijenja.
///
/// Da klijent smije voziti zamjensko vozilo provjerava <c>DozvolaService</c>, isto kao pri
/// rezervaciji, a da je slobodno <c>AvailabilityService</c>.
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

        if (zamjensko.GradId != rezervisano.GradId)
        {
            return "Zamjensko vozilo mora biti u istom gradu kao rezervisano.";
        }

        return null;
    }
}
