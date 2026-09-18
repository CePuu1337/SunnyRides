using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Poruke;
using SunnyRides.Model.Requests;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;
using SunnyRides.Services.Poruke;

namespace SunnyRides.Services.Auth;

public class AuthService : IAuthService
{
    private readonly SunnyRidesDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _trenutniKorisnik;
    private readonly IObjavljivacPoruka _objavljivac;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        SunnyRidesDbContext context,
        ITokenService tokenService,
        ICurrentUserService trenutniKorisnik,
        IObjavljivacPoruka objavljivac,
        ILogger<AuthService> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _trenutniKorisnik = trenutniKorisnik;
        _objavljivac = objavljivac;
        _logger = logger;
    }

    public async Task<PrijavaOdgovorDto> PrijaviAsync(LoginRequest request, CancellationToken ct = default)
    {
        var korisnik = await _context.Korisnici
            .Include(x => x.KorisnikRole)
                .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.KorisnickoIme == request.KorisnickoIme, ct);

        // Ista poruka i kad korisnik ne postoji i kad je lozinka pogresna. Razlicite
        // poruke bi otkrile koja korisnicka imena postoje u sistemu.
        if (korisnik is null || !BCrypt.Net.BCrypt.Verify(request.Lozinka, korisnik.LozinkaHash))
        {
            throw new BusinessException("Pogresno korisnicko ime ili lozinka.");
        }

        if (!korisnik.Aktivan)
        {
            throw new BusinessException("Nalog je deaktiviran. Obratite se agenciji.");
        }

        // Blokiran korisnik se namjerno moze prijaviti - blokada sprjecava kreiranje
        // nove rezervacije, ne pristup vlastitoj historiji.

        var uloge = korisnik.KorisnikRole.Select(x => x.Role.Naziv).ToList();

        var token = _tokenService.Generisi(
            korisnik.Id, korisnik.KorisnickoIme, korisnik.Ime, korisnik.Prezime, uloge);

        return new PrijavaOdgovorDto
        {
            Token = token.Token,
            IsticeUtc = token.IsticeUtc,
            Korisnik = UDto(korisnik, uloge)
        };
    }

    public async Task<KorisnikDto> RegistrujAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var korisnickoImeZauzeto = await _context.Korisnici
            .AnyAsync(x => x.KorisnickoIme == request.KorisnickoIme, ct);

        if (korisnickoImeZauzeto)
        {
            throw new BusinessException($"Korisnicko ime \"{request.KorisnickoIme}\" je vec zauzeto.");
        }

        var emailZauzet = await _context.Korisnici.AnyAsync(x => x.Email == request.Email, ct);
        if (emailZauzet)
        {
            throw new BusinessException("Nalog sa tom email adresom vec postoji.");
        }

        var godine = GodineNaDan(request.DatumRodjenja, DateTime.UtcNow);
        if (godine < 16)
        {
            throw new BusinessException("Registracija je moguca od 16. godine.");
        }

        // Uloga se dodjeljuje na serveru i uvijek je Klijent. Zahtjev za registraciju
        // nema nijedno polje kojim bi klijent mogao uticati na ovu odluku.
        var ulogaKlijent = await _context.Role.FirstOrDefaultAsync(x => x.Naziv == Uloge.Klijent, ct)
            ?? throw new BusinessException("Uloga Klijent ne postoji u sistemu.");

        var korisnik = new Korisnik
        {
            KorisnickoIme = request.KorisnickoIme,
            Ime = request.Ime,
            Prezime = request.Prezime,
            Email = request.Email,
            Telefon = request.Telefon,
            DatumRodjenja = request.DatumRodjenja,
            LozinkaHash = BCrypt.Net.BCrypt.HashPassword(request.Lozinka),
            Aktivan = true,
            Blokiran = false,
            DatumRegistracije = DateTime.UtcNow
        };

        korisnik.KorisnikRole.Add(new KorisnikRole
        {
            Role = ulogaKlijent,
            DatumDodjele = DateTime.UtcNow
        });

        _context.Korisnici.Add(korisnik);
        await _context.SaveChangesAsync(ct);

        return UDto(korisnik, new List<string> { Uloge.Klijent });
    }

    public async Task PromijeniLozinkuAsync(PromjenaLozinkeRequest request, CancellationToken ct = default)
    {
        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

        var korisnik = await _context.Korisnici.FirstOrDefaultAsync(x => x.Id == korisnikId, ct)
            ?? throw NotFoundException.Za("Korisnik", korisnikId);

        if (!BCrypt.Net.BCrypt.Verify(request.StaraLozinka, korisnik.LozinkaHash))
        {
            throw new BusinessException("Stara lozinka nije tacna.");
        }

        if (BCrypt.Net.BCrypt.Verify(request.NovaLozinka, korisnik.LozinkaHash))
        {
            throw new BusinessException("Nova lozinka mora biti razlicita od stare.");
        }

        korisnik.LozinkaHash = BCrypt.Net.BCrypt.HashPassword(request.NovaLozinka);
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Kod se salje i upisuje hashiran, isto kao lozinka. U bazi ne stoji nista cime
    /// bi se nalog mogao otvoriti - ni onome ko bazu vidi.
    ///
    /// Odgovor je uvijek isti, i kad email postoji i kad ne postoji. Da nije tako,
    /// ovaj endpoint bi bio besplatna provjera koje su adrese registrovane.
    /// </summary>
    public async Task ZatraziResetAsync(
        ZaboravljenaLozinkaRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim();

        var korisnik = await _context.Korisnici.FirstOrDefaultAsync(x => x.Email == email, ct);

        if (korisnik is null || !korisnik.Aktivan)
        {
            _logger.LogInformation("Zatrazen reset lozinke za adresu koja nema aktivan nalog.");
            return;
        }

        // Stariji kodovi istog korisnika prestaju vaziti. Vazeci kod je uvijek tacno
        // jedan - onaj iz posljednjeg emaila.
        var raniji = await _context.KodoviZaResetLozinke
            .Where(x => x.KorisnikId == korisnik.Id && !x.Iskoristen)
            .ToListAsync(ct);

        foreach (var stari in raniji)
        {
            stari.Iskoristen = true;
        }

        var kod = KodoviZaReset.Generisi();
        var sada = DateTime.UtcNow;
        var istice = sada.Add(KodoviZaReset.Trajanje);

        _context.KodoviZaResetLozinke.Add(new KodZaResetLozinke
        {
            KorisnikId = korisnik.Id,
            KodHash = BCrypt.Net.BCrypt.HashPassword(kod),
            DatumIsteka = istice,
            Iskoristen = false,
            DatumKreiranja = sada
        });

        await _context.SaveChangesAsync(ct);

        // Poruka ide tek kad je upis potvrdjen. Obrnutim redoslijedom bi klijent mogao
        // dobiti kod koji u bazi ne postoji.
        await _objavljivac.ObjaviAsync(
            Redovi.ResetLozinke, new ResetLozinkePoruka(korisnik.Id, kod, istice), ct);
    }

    /// <summary>
    /// Poruka o gresci je jedna jedina, bez obzira na to sta tacno nije u redu -
    /// nepostojeci nalog, pogresan kod i istekao kod izgledaju isto. Razlicite poruke
    /// bi rekle napadacu kada je pogodio email, a kada kod.
    /// </summary>
    public async Task PotvrdiResetAsync(ResetLozinkeRequest request, CancellationToken ct = default)
    {
        const string PorukaGreske = "Kod nije ispravan ili je istekao. Zatrazite novi.";

        var email = request.Email.Trim();
        var kod = KodoviZaReset.Normalizuj(request.Kod);

        if (!KodoviZaReset.JeMogucOblik(kod))
        {
            throw new BusinessException(PorukaGreske);
        }

        var korisnik = await _context.Korisnici.FirstOrDefaultAsync(x => x.Email == email, ct)
            ?? throw new BusinessException(PorukaGreske);

        if (!korisnik.Aktivan)
        {
            throw new BusinessException(PorukaGreske);
        }

        var sada = DateTime.UtcNow;

        // Hash se ne moze traziti upitom, pa se uzimaju kandidati ovog korisnika -
        // najvise jedan vazeci - i provjerava se kroz BCrypt.
        var kandidati = await _context.KodoviZaResetLozinke
            .Where(x => x.KorisnikId == korisnik.Id && !x.Iskoristen && x.DatumIsteka > sada)
            .OrderByDescending(x => x.Id)
            .ToListAsync(ct);

        var zapis = kandidati.FirstOrDefault(x => BCrypt.Net.BCrypt.Verify(kod, x.KodHash))
            ?? throw new BusinessException(PorukaGreske);

        // Kod vazi jednom. Bez ovoga bi isti email ostao kljuc naloga do isteka roka.
        zapis.Iskoristen = true;
        korisnik.LozinkaHash = BCrypt.Net.BCrypt.HashPassword(request.NovaLozinka);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Korisnik {KorisnikId} je postavio novu lozinku kodom sa emaila.", korisnik.Id);
    }

    public async Task OdjaviAsync(CancellationToken ct = default)
    {
        var jti = _trenutniKorisnik.Jti;
        if (string.IsNullOrWhiteSpace(jti))
        {
            throw new BusinessException("Zahtjev ne sadrzi vazeci token.");
        }

        // Odjava mora invalidirati token na serveru. Brisanje tokena na uredjaju nije
        // dovoljno - token bi i dalje bio vazeci do isteka roka.
        var vecOpozvan = await _context.OpozvaniTokeni.AnyAsync(x => x.Jti == jti, ct);
        if (vecOpozvan)
        {
            return;
        }

        _context.OpozvaniTokeni.Add(new OpozvaniToken
        {
            Jti = jti,
            DatumIsteka = _trenutniKorisnik.IsticeUtc ?? DateTime.UtcNow,
            DatumOpoziva = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);
    }

    public async Task<KorisnikDto> TrenutniKorisnikAsync(CancellationToken ct = default)
    {
        var korisnikId = _trenutniKorisnik.ObaveznoKorisnikId();

        var korisnik = await _context.Korisnici
            .Include(x => x.KorisnikRole)
                .ThenInclude(x => x.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == korisnikId, ct)
            ?? throw NotFoundException.Za("Korisnik", korisnikId);

        return UDto(korisnik, korisnik.KorisnikRole.Select(x => x.Role.Naziv).ToList());
    }

    private static KorisnikDto UDto(Korisnik korisnik, List<string> uloge) => new()
    {
        Id = korisnik.Id,
        KorisnickoIme = korisnik.KorisnickoIme,
        Ime = korisnik.Ime,
        Prezime = korisnik.Prezime,
        Email = korisnik.Email,
        Telefon = korisnik.Telefon,
        DatumRodjenja = korisnik.DatumRodjenja,
        DatumRegistracije = korisnik.DatumRegistracije,
        PutanjaSlike = korisnik.PutanjaSlike,
        ThumbnailUrl = Fajlovi.PutanjeSlika.Thumbnail(korisnik.PutanjaSlike),
        Aktivan = korisnik.Aktivan,
        Blokiran = korisnik.Blokiran,
        Uloge = uloge
    };

    private static int GodineNaDan(DateTime datumRodjenja, DateTime naDan)
    {
        var godine = naDan.Year - datumRodjenja.Year;
        if (datumRodjenja.Date > naDan.Date.AddYears(-godine))
        {
            godine--;
        }
        return godine;
    }
}
