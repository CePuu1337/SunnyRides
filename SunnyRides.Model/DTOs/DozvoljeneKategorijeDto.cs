namespace SunnyRides.Model.DTOs;

/// <summary>
/// Sta klijent smije voziti, i zasto.
///
/// Obrazlozenje nije ukras: uputstvo trazi da iznad rezultata pretrage stoji
/// objasnjenje filtriranja, da korisniku ne ostane nejasno zasto pojedina vozila
/// nisu ponudjena.
/// </summary>
public class DozvoljeneKategorijeDto
{
    public int KorisnikId { get; set; }

    /// <summary>Kategorije upisane na dozvoli.</summary>
    public List<string> PosjedovaneKategorije { get; set; } = new();

    /// <summary>Kategorije vozila koja klijent smije voziti, ukljucujuci one pokrivene hijerarhijom.</summary>
    public List<string> DozvoljeneKategorije { get; set; } = new();
    public List<int> DozvoljeneKategorijeIds { get; set; } = new();

    public bool MozeRezervisati { get; set; }

    public string Obrazlozenje { get; set; } = null!;
}
