namespace SunnyRides.Model.DTOs;

/// <summary>
/// Broj nepročitanih obavjestenja, za oznaku na ikoni zvona.
///
/// Vraca se kao objekat a ne kao goli broj zato sto se isti oblik salje i kroz
/// SignalR - aplikacija tada cita isto polje bez obzira je li podatak stigao
/// odgovorom na zahtjev ili gurnut sa servera.
/// </summary>
public class BrojNeprocitanihDto
{
    public int Broj { get; set; }
}
