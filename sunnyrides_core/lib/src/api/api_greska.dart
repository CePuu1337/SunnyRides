/// Greska koju je API opisao, u obliku koji ekran moze prikazati.
///
/// Server salje ProblemDetails i nikad stack trace, pa je [poruka] vec tekst
/// namijenjen korisniku. Aplikacija ga prikazuje kakav jeste umjesto da izmislja
/// svoju verziju - inace bi ista provjera imala dvije formulacije.
class ApiGreska implements Exception {
  ApiGreska({
    required this.status,
    required this.poruka,
    this.naslov,
    this.greskeValidacije = const {},
  });

  /// HTTP status. Ekran po njemu razlikuje odbijen zahtjev od pada sistema.
  final int status;

  final String poruka;
  final String? naslov;

  /// Greske po poljima, onako kako ih vraca validacija modela.
  /// Kljuc je naziv polja, vrijednost lista poruka za to polje.
  final Map<String, List<String>> greskeValidacije;

  /// Zahtjev odbijen zbog poslovnog pravila - korisnik moze nesto promijeniti.
  bool get jePoslovna => status == 400 || status == 409;

  bool get jeNeovlasten => status == 401;
  bool get jeZabranjeno => status == 403;
  bool get jeNijePronadjeno => status == 404;

  /// Kvar na serveru ili mrezi - korisnik tu nema sta ispraviti.
  bool get jeKvar => status >= 500 || status == 0;

  /// Prva poruka vezana za konkretno polje, ako je ima.
  String? zaPolje(String polje) {
    for (final unos in greskeValidacije.entries) {
      if (unos.key.toLowerCase() == polje.toLowerCase() && unos.value.isNotEmpty) {
        return unos.value.first;
      }
    }

    return null;
  }

  @override
  String toString() => poruka;
}
