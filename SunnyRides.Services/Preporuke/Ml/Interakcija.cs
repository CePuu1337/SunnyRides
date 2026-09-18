namespace SunnyRides.Services.Preporuke.Ml;

/// <summary>
/// Jedan red matrice korisnik x model vozila, onako kako ga ML.NET ocekuje.
///
/// Polja su <c>float</c> jer ih pipeline pretvara u kljuceve kroz <c>MapValueToKey</c>;
/// to je oblik koji trener matricne faktorizacije trazi. Nazivi polja se u pipelineu
/// koriste kao nazivi kolona, pa se ne smiju mijenjati bez izmjene pipelinea.
/// </summary>
public class Interakcija
{
    public float KorisnikId { get; set; }

    public float ModelVozilaId { get; set; }

    /// <summary>Ocjena 1-5. Ovo je kolona koju model uci da predvidi.</summary>
    public float Ocjena { get; set; }
}

/// <summary>Izlaz modela. <c>Score</c> je naziv kolone koju trener upisuje.</summary>
public class PredikcijaOcjene
{
    public float ModelVozilaId { get; set; }

    public float Score { get; set; }
}

/// <summary>
/// Interakcija sa oznakom odakle potice.
///
/// Odvajanje je bitno zbog evaluacije: RMSE se racuna iskljucivo nad stvarnim
/// ocjenama. Da se u test skup uvuku procijenjene vrijednosti, model bi se mjerio
/// prema broju koji smo mu sami zadali, pa bi rezultat izgledao bolje nego sto jeste.
/// </summary>
public record InterakcijaZapis(int KorisnikId, int ModelVozilaId, double Ocjena, bool JeStvarnaOcjena);

/// <summary>Jedna ocjena iz baze, svedena na ono sto modelu treba.</summary>
public record OcjenaZapis(int KorisnikId, int ModelVozilaId, int Ocjena);

/// <summary>Jedan zavrsen najam, svedeno na korisnika i model vozila.</summary>
public record NajamZapis(int KorisnikId, int ModelVozilaId);
