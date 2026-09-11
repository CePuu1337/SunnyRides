namespace SunnyRides.Services.Exceptions;

/// <summary>
/// Korisnik je prijavljen, ali nema pravo na ovaj resurs - na primjer, pokusava
/// preuzeti tudju fotografiju vozacke dozvole. ExceptionFilter je mapira na HTTP 403.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string poruka) : base(poruka)
    {
    }
}
