namespace SunnyRides.Services.Exceptions;

/// <summary>
/// Krsenje poslovnog pravila. Poruka je namijenjena korisniku i mora biti
/// konkretna - ne "Neispravan unos", nego "Datum vracanja mora biti poslije
/// datuma preuzimanja." ExceptionFilter je mapira na HTTP 400.
/// </summary>
public class BusinessException : Exception
{
    public BusinessException(string poruka) : base(poruka)
    {
    }

    public BusinessException(string poruka, Exception unutrasnja) : base(poruka, unutrasnja)
    {
    }
}
