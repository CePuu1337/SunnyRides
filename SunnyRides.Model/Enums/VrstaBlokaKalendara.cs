namespace SunnyRides.Model.Enums;

/// <summary>Sta u kalendaru flote zauzima termin.</summary>
public enum VrstaBlokaKalendara
{
    Rezervacija = 1,

    /// <summary>Servis, kvar ili drugi razlog zbog kojeg vozilo nije u ponudi.</summary>
    Blokada = 2
}
