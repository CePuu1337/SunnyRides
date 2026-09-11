namespace SunnyRides.Services.Exceptions;

/// <summary>Trazeni zapis ne postoji. ExceptionFilter je mapira na HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string poruka) : base(poruka)
    {
    }

    public static NotFoundException Za(string entitet, int id) =>
        new($"{entitet} sa identifikatorom {id} ne postoji.");
}
