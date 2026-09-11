using Microsoft.EntityFrameworkCore;
using SunnyRides.Model.DTOs;
using SunnyRides.Model.Konstante;
using SunnyRides.Model.Requests;
using SunnyRides.Services.Database;
using SunnyRides.Services.Database.Entities;
using SunnyRides.Services.Exceptions;

namespace SunnyRides.Services.Auth;

public class AuthService : IAuthService
{
    private readonly SunnyRidesDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _trenutniKorisnik;

    public AuthService(
        SunnyRidesDbContext context,
        ITokenService tokenService,
        ICurrentUserService trenutniKorisnik)
    {
        _context = context;
        _tokenService = tokenService;
        _trenutniKorisnik = trenutniKorisnik;
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
        PutanjaSlike = korisnik.PutanjaSlike,
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
