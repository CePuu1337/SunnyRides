namespace SunnyRides.Subscriber.Email;

public interface IPosiljalacEmaila
{
    Task PosaljiAsync(string primalac, string naslov, string tekst, CancellationToken ct = default);
}
