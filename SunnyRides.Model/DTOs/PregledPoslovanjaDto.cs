using SunnyRides.Model.Enums;

namespace SunnyRides.Model.DTOs;

/// <summary>
/// Sve sto pocetni ekran desktop aplikacije prikazuje, u jednom odgovoru.
///
/// Vrijednosti su gotovi agregati. Racunaju se na bazi i stizu izracunate - Flutter ih
/// samo prikazuje. Da se racunaju u aplikaciji, morala bi povuci sve rezervacije i sva
/// placanja da bi ispisala cetiri broja.
/// </summary>
public class PregledPoslovanjaDto
{
    /// <summary>Trenutak za koji pregled vrijedi. Sve "danas" i "sada" vrijednosti se odnose na njega.</summary>
    public DateTime NaDanUtc { get; set; }

    public MetrikePoslovanjaDto Metrike { get; set; } = new();

    /// <summary>Preuzimanja i vracanja zakazana za danas, poredana po vremenu.</summary>
    public List<StavkaRasporedaDto> RasporedDanas { get; set; } = new();

    public List<PresjekPoTipuDto> PoTipuVozila { get; set; } = new();

    public List<PresjekPoPoslovniciDto> PoPoslovnici { get; set; } = new();
}

public class MetrikePoslovanjaDto
{
    public int UkupnoAktivnihVozila { get; set; }
    public int VozilaUNajmu { get; set; }

    /// <summary>
    /// Udio flote koji je u ovom trenutku kod klijenata, u procentima.
    ///
    /// Namjerno **trenutna**, ne mjesecna: kartica na pocetnom ekranu odgovara na
    /// pitanje "koliko nam vozila sada radi". Iskoristenost kroz period je nesto drugo
    /// i racuna se u izvjestaju o floti, gdje se gleda broj dana izdato.
    /// </summary>
    public double TrenutnaIskoristenost { get; set; }

    /// <summary>Potvrdjene rezervacije koje jos nisu zavrsene.</summary>
    public int AktivneRezervacije { get; set; }

    public decimal NaplacenoTekucegMjeseca { get; set; }
    public decimal RefundiranoTekucegMjeseca { get; set; }

    /// <summary>Naplaceno umanjeno za povrate. To je broj koji agenciju stvarno zanima.</summary>
    public decimal NetoPrihodTekucegMjeseca { get; set; }

    /// <summary>
    /// Zbir predmeta koji cekaju osoblje. Razlozen je na dvije stavke ispod, da
    /// uposlenik odmah zna gdje da klikne umjesto da trazi.
    /// </summary>
    public int CekaObradu { get; set; }

    public int NeverifikovaneDozvole { get; set; }
    public int NeplaceneRezervacije { get; set; }

    /// <summary>
    /// Poredjenje sa istim dijelom proslog mjeseca. Kartice na pocetnom ekranu uz
    /// vrijednost prikazuju i strelicu sa postotkom, a taj postotak se racuna ovdje.
    /// </summary>
    public PoredbaPeriodaDto Poredba { get; set; } = new();
}

/// <summary>
/// Tekuci mjesec do danas naspram istog broja dana proslog mjeseca.
///
/// Poredi se isti raspon, ne cijeli prosli mjesec. Da se poredi cijeli, devetnaestog
/// u mjesecu bi svaka kartica pokazivala pad - ne zato sto poslovanje ide losije nego
/// zato sto mjesec jos nije zavrsen.
/// </summary>
public class PoredbaPeriodaDto
{
    public DateTime TekuciOd { get; set; }
    public DateTime TekuciDo { get; set; }
    public DateTime PrethodniOd { get; set; }
    public DateTime PrethodniDo { get; set; }

    public PoredbaMetrikaDto NetoPrihod { get; set; } = new();
    public PoredbaMetrikaDto NoveRezervacije { get; set; } = new();
    public PoredbaMetrikaDto ZavrseneRezervacije { get; set; } = new();
}

/// <summary>Jedna brojka sada, ista brojka tada i razlika medju njima.</summary>
public class PoredbaMetrikaDto
{
    public decimal Tekuce { get; set; }
    public decimal Prethodno { get; set; }

    /// <summary>
    /// Promjena u postotku. Prazno kad je prethodna vrijednost nula - rast sa nule
    /// nije "beskonacno posto" nego podatak koji se ne moze izraziti postotkom, pa
    /// aplikacija u tom slucaju ne crta strelicu.
    /// </summary>
    public double? PromjenaPosto { get; set; }
}

/// <summary>Jedno preuzimanje ili vracanje zakazano za danas.</summary>
public class StavkaRasporedaDto
{
    public int RezervacijaId { get; set; }
    public string Broj { get; set; } = null!;

    /// <summary>Isti enum koji nosi i zapis o primopredaji - izdavanje ili povrat.</summary>
    public TipPrimopredaje Tip { get; set; }

    public DateTime Vrijeme { get; set; }

    public string Vozilo { get; set; } = null!;
    public string RegistarskaOznaka { get; set; } = null!;
    public string Klijent { get; set; } = null!;
    public string Poslovnica { get; set; } = null!;

    public StatusRezervacije Status { get; set; }

    /// <summary>Je li primopredaja tog tipa vec evidentirana. Uposlenik po tome zna sta mu preostaje.</summary>
    public bool Evidentirano { get; set; }
}

public class PresjekPoTipuDto
{
    public int TipVozilaId { get; set; }
    public string Naziv { get; set; } = null!;
    public int BrojVozila { get; set; }
    public int UNajmu { get; set; }
}

public class PresjekPoPoslovniciDto
{
    public int PoslovnicaId { get; set; }
    public string Naziv { get; set; } = null!;
    public int BrojVozila { get; set; }
    public int UNajmu { get; set; }
}
