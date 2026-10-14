using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using SunnyRides.Services.Poruke;

namespace SunnyRides.Subscriber.Email;

/// <summary>
/// Slanje emaila kroz MailKit.
///
/// Konekcija se otvara po poruci i uredno zatvara. Za obim ovog sistema - nekoliko
/// emaila u minuti - to je jeftinije nego drzati otvorenu SMTP sesiju koju server
/// ionako zatvori nakon nekog vremena neaktivnosti.
/// </summary>
public class MailKitPosiljalac : IPosiljalacEmaila
{
    private readonly EmailPostavke _postavke;
    private readonly ILogger<MailKitPosiljalac> _logger;

    public MailKitPosiljalac(EmailPostavke postavke, ILogger<MailKitPosiljalac> logger)
    {
        _postavke = postavke;
        _logger = logger;
    }

    public async Task PosaljiAsync(string primalac, string naslov, string tekst, CancellationToken ct = default)
    {
        var host = _postavke.Host;
        var korisnik = _postavke.Korisnik;
        var lozinka = _postavke.Lozinka;
        var posiljalac = _postavke.Posiljalac ?? korisnik;

        if (host is null || korisnik is null || lozinka is null || posiljalac is null)
        {
            _logger.LogWarning(
                "SMTP nije konfigurisan, email nije poslan. Primalac: {Primalac}, naslov: {Naslov}",
                primalac, naslov);
            return;
        }

        // Demo korisnici imaju adrese na rezervisanim domenama, na koje mail nikad ne bi
        // stigao. Kad je SMTP_PREUSMJERI_NA postavljeno, njihova poruka ide na tu adresu,
        // a u naslovu i tekstu pise kome je bila namijenjena. Stvarna adresa - nalog
        // koji je neko otvorio svojim emailom - dobija mail normalno.
        var stvarniPrimalac = primalac;
        var preusmjerenje = _postavke.PreusmjeriNa;

        if (!string.IsNullOrWhiteSpace(preusmjerenje) && DemoAdrese.JeDemo(primalac))
        {
            stvarniPrimalac = preusmjerenje;
            naslov = $"[za {primalac}] {naslov}";
            tekst = $"Poruka je preusmjerena jer je sistem u testnom radu.\n" +
                    $"Stvarni primalac: {primalac}\n\n" +
                    new string('-', 40) + "\n\n" + tekst;
        }

        var poruka = new MimeMessage();
        poruka.From.Add(MailboxAddress.Parse(posiljalac));
        poruka.To.Add(MailboxAddress.Parse(stvarniPrimalac));
        poruka.Subject = naslov;
        poruka.Body = new TextPart(TextFormat.Plain) { Text = tekst };

        using var klijent = new SmtpClient();

        // StartTls je ono sto Gmail i vecina servera ocekuje na portu 587.
        await klijent.ConnectAsync(host, _postavke.Port, SecureSocketOptions.StartTls, ct);
        await klijent.AuthenticateAsync(korisnik, lozinka, ct);
        await klijent.SendAsync(poruka, ct);
        await klijent.DisconnectAsync(quit: true, ct);

        _logger.LogInformation("Email poslan na {Primalac}: {Naslov}", stvarniPrimalac, naslov);
    }
}
