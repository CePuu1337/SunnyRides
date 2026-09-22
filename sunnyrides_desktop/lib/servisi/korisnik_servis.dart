import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Uloga koju nalog moze imati. Uloge su fiksne i ne unose se kroz aplikaciju.
class Uloga {
  const Uloga({required this.id, required this.naziv, this.opis});

  final int id;
  final String naziv;
  final String? opis;

  factory Uloga.izJsona(Map<String, dynamic> json) {
    return Uloga(
      id: citajInt(json['id']),
      naziv: json['naziv']?.toString() ?? '',
      opis: json['opis']?.toString(),
    );
  }
}

class KorisnikServis {
  const KorisnikServis(this._klijent);

  final ApiKlijent _klijent;

  Future<Strana<Korisnik>> lista({
    String? tekst,
    String? uloga,
    bool? aktivan,
    bool? blokiran,
    int stranica = 0,
    int velicinaStranice = 15,
  }) async {
    final odgovor = await _klijent.get(
      '/api/korisnici',
      upit: {
        'page': stranica,
        'pageSize': velicinaStranice,
        'includeTotalCount': true,
        'tekst': tekst,
        'uloga': uloga,
        'aktivan': aktivan,
        'blokiran': blokiran,
      },
    );

    return Strana.izJsona(odgovor as Map<String, dynamic>, Korisnik.izJsona);
  }

  Future<List<Uloga>> uloge() async {
    final odgovor = await _klijent.get('/api/korisnici/uloge');

    if (odgovor is! List) {
      return const [];
    }

    return odgovor
        .whereType<Map<String, dynamic>>()
        .map(Uloga.izJsona)
        .toList();
  }

  Future<Korisnik> dodaj(Map<String, dynamic> zahtjev) async {
    final odgovor = await _klijent.post('/api/korisnici', tijelo: zahtjev);

    return Korisnik.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<Korisnik> izmijeni(int id, Map<String, dynamic> zahtjev) async {
    final odgovor = await _klijent.put('/api/korisnici/$id', tijelo: zahtjev);

    return Korisnik.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<Korisnik> postaviUloge(int id, List<int> ulogeIds) async {
    final odgovor = await _klijent.put(
      '/api/korisnici/$id/uloge',
      tijelo: {'ulogeIds': ulogeIds},
    );

    return Korisnik.izJsona(odgovor as Map<String, dynamic>);
  }

  /// Administratorski reset - stara lozinka se ne trazi, jer je administrator ne zna.
  Future<void> resetujLozinku(int id, String nova, String potvrda) async {
    await _klijent.post(
      '/api/korisnici/$id/reset-lozinke',
      tijelo: {'novaLozinka': nova, 'potvrdaNoveLozinke': potvrda},
    );
  }

  Future<Korisnik> blokiraj(int id) async {
    final odgovor = await _klijent.post('/api/korisnici/$id/blokiraj');

    return Korisnik.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<Korisnik> odblokiraj(int id) async {
    final odgovor = await _klijent.post('/api/korisnici/$id/odblokiraj');

    return Korisnik.izJsona(odgovor as Map<String, dynamic>);
  }

  /// Brisanje na serveru znaci deaktivaciju - historija najmova mora ostati.
  Future<void> deaktiviraj(int id) async {
    await _klijent.delete('/api/korisnici/$id');
  }
}
