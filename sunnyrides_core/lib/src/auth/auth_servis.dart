import '../api/api_klijent.dart';
import '../modeli/korisnik.dart';
import 'pohrana_tokena.dart';

/// Prijava, odjava i sve sto se tice lozinke.
class AuthServis {
  AuthServis({required ApiKlijent klijent, required PohranaTokena pohrana})
      : _klijent = klijent,
        _pohrana = pohrana;

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
    final odgovor = await _klijent.post('/api/auth/register', tijelo: {
      'korisnickoIme': korisnickoIme,
      'ime': ime,
      'prezime': prezime,
      'email': email,
      'telefon': telefon,
      'datumRodjenja': datumRodjenja.toUtc().toIso8601String(),
      'lozinka': lozinka,
      'potvrdaLozinke': potvrdaLozinke,
    });

    final prijava = PrijavaOdgovor.izJsona(odgovor as Map<String, dynamic>);

    await _pohrana.sacuvaj(prijava.token, prijava.isticeUtc);

    return prijava.korisnik;
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
