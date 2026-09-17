namespace SunnyRides.Services.Placanja;

/// <summary>
/// Stripe nije izvrsio zahtjev.
///
/// Razlikuju se dva slucaja jer se na njih reaguje razlicito:
/// <c>Konacna</c> znaci da je Stripe odgovorio i odbio zahtjev - operacija se
/// sigurno nije desila. Inace (mreza, istek vremena, greska na njihovoj strani)
/// ne znamo je li se desila, pa se zapis ostavlja otvoren i ponavlja se istim
/// idempotency kljucem, koji garantuje da se ne izvrsi dvaput.
/// </summary>
public class PlatniProvajderException : Exception
{
    public PlatniProvajderException(string poruka, bool konacna, Exception unutrasnja)
        : base(poruka, unutrasnja)
    {
        Konacna = konacna;
    }

    public bool Konacna { get; }
}
