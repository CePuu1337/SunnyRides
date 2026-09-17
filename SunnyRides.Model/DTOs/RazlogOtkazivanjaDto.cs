namespace SunnyRides.Model.DTOs;

public class RazlogOtkazivanjaDto
{
    public int Id { get; set; }
    public string Naziv { get; set; } = null!;
    public bool ZaKlijenta { get; set; }
    public bool ZaAgenciju { get; set; }

    /// <summary>Kad je ukljuceno, aplikacija uz ovaj razlog trazi i kratko objasnjenje.</summary>
    public bool TraziNapomenu { get; set; }

    public bool Aktivan { get; set; }
}
