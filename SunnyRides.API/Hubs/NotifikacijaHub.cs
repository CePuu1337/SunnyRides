using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SunnyRides.API.Hubs;

/// <summary>
/// Veza kroz koju obavjestenja stizu u aplikaciju cim nastanu.
///
/// Hub nema nijednu metodu koju klijent moze pozvati. To je namjerno: sve sto
/// aplikacija smije uraditi sa obavjestenjima ide kroz kontroler, gdje postoji
/// provjera vlasnistva. Kad bi hub imao metodu tipa "prijavi me na korisnika X",
/// bilo bi dovoljno poslati tudji broj i slusati tudja obavjestenja - a tako nesto
/// se ne bi vidjelo ni u jednom logu HTTP zahtjeva.
///
/// Umjesto toga, veza se pri uspostavi sama svrstava u grupu svog korisnika, a
/// korisnik se cita iz tokena.
/// </summary>
[Authorize]
public class NotifikacijaHub : Hub
{
    private readonly ILogger<NotifikacijaHub> _logger;

    public NotifikacijaHub(ILogger<NotifikacijaHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adresa huba. Stoji ovdje da je mapiranje rute i provjera tokena iz query
    /// stringa citaju sa istog mjesta - ako se raziđu, veza se tiho ne uspostavlja.
    /// </summary>
    public const string Putanja = "/hubs/notifikacije";

    /// <summary>Naziv grupe za jednog korisnika. Isti oblik koristi i strana koja salje.</summary>
    public static string GrupaZa(int korisnikId) => $"korisnik-{korisnikId}";

    public override async Task OnConnectedAsync()
    {
        var korisnikId = KorisnikIzTokena();

        if (korisnikId is null)
        {
            // Do ovoga se dolazi samo ako je token prosao provjeru a nema sub claim,
            // sto bi znacilo da ga je izdao neko drugi. Veza se prekida.
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GrupaZa(korisnikId.Value));

        _logger.LogInformation("Korisnik {KorisnikId} je otvorio vezu {Veza}.",
            korisnikId.Value, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Grupa se ne mora rucno napustati - SignalR uklanja vezu iz svih grupa kad se
    /// prekine. Ovdje se samo biljezi razlog, jer je pri trazenju kvara korisno znati
    /// je li veza uredno zatvorena ili je pukla.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? greska)
    {
        if (greska is null)
        {
            _logger.LogInformation("Veza {Veza} je zatvorena.", Context.ConnectionId);
        }
        else
        {
            _logger.LogWarning(greska, "Veza {Veza} je prekinuta.", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(greska);
    }

    private int? KorisnikIzTokena() =>
        int.TryParse(Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id)
            ? id
            : null;
}
