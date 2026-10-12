/// Provjere unosa koje dijele obje aplikacije.
///
/// Pravila su ista kao u request objektima na serveru, da korisnik gresku vidi ispod
/// polja prije nego zahtjev ode. Svaka poruka kaze i kakav se unos ocekuje, ne samo
/// da nesto nije u redu.
class Validatori {
  const Validatori._();

  static final _oblikKorisnickogImena = RegExp(r'^[a-zA-Z0-9._-]+$');
  static final _oblikEmaila = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');
  static final _oblikTelefona = RegExp(r'^\+387 6\d{1} \d{3} \d{3}$');

  static String? korisnickoIme(String? vrijednost) {
    final tekst = vrijednost?.trim() ?? '';

    if (tekst.length < 3 || tekst.length > 50) {
      return 'Korisničko ime mora imati između 3 i 50 znakova.';
    }

    if (!_oblikKorisnickogImena.hasMatch(tekst)) {
      return 'Dozvoljena su slova bez kvačica, brojevi, tačka, donja crta i '
          'crtica, npr. ammar.puce';
    }

    return null;
  }

  static String? email(String? vrijednost) {
    final tekst = vrijednost?.trim() ?? '';

    if (tekst.isEmpty) {
      return 'Unesite email adresu.';
    }

    return _oblikEmaila.hasMatch(tekst)
        ? null
        : 'Unesite email u obliku ime@primjer.com.';
  }

  /// Telefon je na nekim formama neobavezan. Tada se prazno polje prihvata, a
  /// popunjeno mora imati ispravan oblik.
  static String? telefon(String? vrijednost, {bool obavezan = true}) {
    final tekst = vrijednost?.trim() ?? '';

    if (tekst.isEmpty) {
      return obavezan ? 'Unesite broj telefona, npr. +387 61 234 567.' : null;
    }

    return _oblikTelefona.hasMatch(tekst)
        ? null
        : 'Unesite broj u obliku +387 6X XXX XXX, npr. +387 61 234 567.';
  }

  /// Ista granica kao na serveru: od 6 do 100 znakova.
  static String? lozinka(String? vrijednost) {
    final duzina = vrijednost?.length ?? 0;

    return duzina < 6 || duzina > 100
        ? 'Lozinka mora imati od 6 do 100 znakova.'
        : null;
  }

  /// Potvrda se poredi sa lozinkom upisanom u polje iznad.
  static String? potvrdaLozinke(String? vrijednost, String lozinka) {
    return vrijednost == lozinka ? null : 'Lozinke se ne poklapaju.';
  }
}
