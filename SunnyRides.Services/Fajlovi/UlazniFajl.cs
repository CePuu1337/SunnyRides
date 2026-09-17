namespace SunnyRides.Services.Fajlovi;

/// <summary>
/// Otpremljen fajl, predat servisu bez ikakve veze sa HTTP-om. Kontroler otvara
/// stream i zatvara ga poslije poziva; servis ga samo cita.
/// </summary>
public record UlazniFajl(Stream Sadrzaj, long Duzina);
