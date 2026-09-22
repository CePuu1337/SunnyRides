namespace SunnyRides.Services.Database.Entities;

public class TipGoriva
{
    public int Id { get; set; }
    public string Naziv { get; set; } = null!;

    /// <summary>
    /// Vozilo sa ovim pogonom se puni strujom, ne gorivom.
    ///
    /// Zastavica u sifrarniku, a ne poredjenje naziva u kodu: naziv je podatak koji
    /// administrator moze promijeniti ili dodati novi ("Struja", "EV"), pa bi provjera
    /// po nazivu tiho prestala vaziti. Ovako svojstvo stoji uz zapis koji ga i opisuje.
    /// </summary>
    public bool JeElektricni { get; set; }

    public ICollection<ModelVozila> Modeli { get; set; } = new List<ModelVozila>();
}
