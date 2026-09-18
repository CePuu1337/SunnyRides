namespace SunnyRides.Services.Poruke;

/// <summary>
/// Pristupni podaci za RabbitMQ, procitani iz okruzenja jednom, pri pokretanju.
/// </summary>
public class RabbitMqPostavke
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Korisnik { get; init; }
    public required string Lozinka { get; init; }

    public static RabbitMqPostavke IzOkruzenja() => new()
    {
        Host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
        Port = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var port) ? port : 5672,
        Korisnik = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
        Lozinka = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest"
    };
}
