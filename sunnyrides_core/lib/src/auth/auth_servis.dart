import '../api/api_greska.dart';
import '../api/api_klijent.dart';
import '../modeli/korisnik.dart';
import 'pohrana_tokena.dart';

/// Prijava, odjava i sve sto se tice lozinke.
class AuthServis {
  AuthServis({required this._klijent, required this._pohrana});

  final ApiKlijent _klijent;
  final PohranaTokena _pohrana;

  Future<Korisnik> prijava(String korisnickoIme, String lozinka) async {
    final odgovor = await _klijent.post('/api/auth/login', tijelo: {
      'korisnickoIme': korisnickoIme,
      'lozinka': lozinka,
    });

    final prijava = PrijavaOdgovor.izJsona(odgovor as Map<String, dynamic>);

    await _pohrana.sacuvaj(prijava.token, prijava.isticeUtc);

    return prijava.korisnik;
  }

  /// Otvara klijentski nalog i odmah prijavljuje.
  ///
  /// Registracija na serveru vraca samo podatke o novom korisniku, ne token. Token
  /// se dobija prijavom, isto kao svaki drugi - tako server i za njega vodi zapis
  /// koji odjava ponistava.
  Future<Korisnik> registracija({
    required String korisnickoIme,
    required String ime,
    required String prezime,
    required String email,
    required String telefon,
    required DateTime datumRodjenja,
    required String lozinka,
    required String potvrdaLozinke,
  }) async {
    await _klijent.post('/api/auth/register', tijelo: {
      'korisnickoIme': korisnickoIme,
      'ime': ime,
      'prezime': prezime,
      'email': email,
      'telefon': telefon,
      'datumRodjenja': datumRodjenja.toUtc().toIso8601String(),
      'lozinka': lozinka,
      'potvrdaLozinke': potvrdaLozinke,
    });

    try {
      return await prijava(korisnickoIme, lozinka);
    } on ApiGreska catch (greska) {
      // Nalog postoji - ponovna registracija bi samo javila da je ime zauzeto.
      throw ApiGreska(
        status: greska.status,
        naslov: 'Nalog je otvoren',
        poruka: 'Nalog je otvoren, ali automatska prijava nije uspjela. '
            'Vratite se nazad i prijavite se.',
      );
    }
  }

  /// Ko je vlasnik tokena. Koristi se pri pokretanju, da se sacuvani token provjeri
  /// prije nego se korisnik pusti u aplikaciju.
  Future<Korisnik> ja() async {
    final odgovor = await _klijent.get('/api/auth/ja');

    return Korisnik.izJsona(odgovor as Map<String, dynamic>);
  }

  /// Odjava ponistava token i na serveru, ne samo lokalno. Zato se poziv salje prije
  /// brisanja - obrnutim redom server ne bi znao koji token da ponisti.
  Future<void> odjava() async {
    try {
      await _klijent.post('/api/auth/logout');
    } finally {
      await _pohrana.obrisi();
    }
  }

  Future<void> promjenaLozinke({
    required String stara,
    required String nova,
    required String potvrda,
  }) async {
    await _klijent.post('/api/auth/promjena-lozinke', tijelo: {
      'staraLozinka': stara,
      'novaLozinka': nova,
      'potvrdaNoveLozinke': potvrda,
    });
  }

  Future<void> zaboravljenaLozinka(String email) async {
    await _klijent.post('/api/auth/zaboravljena-lozinka', tijelo: {'email': email});
  }

  Future<void> resetLozinke({
    required String email,
    required String kod,
    required String nova,
    required String potvrda,
  }) async {
    await _klijent.post('/api/auth/reset-lozinke', tijelo: {
      'email': email,
      'kod': kod,
      'novaLozinka': nova,
      'potvrdaNoveLozinke': potvrda,
    });
  }
}
