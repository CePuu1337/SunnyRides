import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../modeli/stavka_sifrarnika.dart';

/// Ucitavanje sifrarnika za padajuce liste.
///
/// Rezultat se pamti dok aplikacija radi. Gradovi i marke se ne mijenjaju dok
/// uposlenik radi, a bez pamcenja bi se ista lista dohvatala pri svakom otvaranju
/// filtera i svakog dijaloga.
class SifrarnikServis {
  SifrarnikServis(this._klijent);

  final ApiKlijent _klijent;
  final _zapamceno = <String, List<StavkaSifrarnika>>{};

  static const drzave = '/api/drzave';
  static const gradovi = '/api/gradovi';
  static const marke = '/api/marke';
  static const modeliVozila = '/api/modeli-vozila';
  static const tipoviVozila = '/api/tipovi-vozila';
  static const tipoviGoriva = '/api/tipovi-goriva';
  static const kategorijeDozvola = '/api/kategorije-dozvola';
  static const pravilaKategorija = '/api/pravila-kategorija';
  static const poslovnice = '/api/poslovnice';
  static const paketiOsiguranja = '/api/paketi-osiguranja';
  static const vrsteOpreme = '/api/vrste-opreme';
  static const razloziOtkazivanja = '/api/razlozi-otkazivanja';

  Future<List<StavkaSifrarnika>> ucitaj(String putanja) async {
    final zapamcena = _zapamceno[putanja];

    if (zapamcena != null) {
      return zapamcena;
    }

    // Sifrarnici su male tabele, ali endpoint je ipak paginiran, pa se trazi
    // dovoljno velika stranica umjesto da se oslanja na podrazumijevanu.
    final odgovor = await _klijent.get(
      putanja,
      upit: {'page': 0, 'pageSize': 100, 'includeTotalCount': false},
    );

    final strana = Strana.izJsona(
      odgovor as Map<String, dynamic>,
      StavkaSifrarnika.izJsona,
    );

    _zapamceno[putanja] = strana.stavke;

    return strana.stavke;
  }

  /// Poziva se kad ekran izmijeni sifrarnik, da sljedeca lista ne bude zastarjela.
  void zaboravi(String putanja) => _zapamceno.remove(putanja);

  void zaboraviSve() => _zapamceno.clear();
}

/// Citanje i pisanje jednog sifrarnika, bez obzira kojeg.
///
/// Sifrarnici imaju isti oblik rute i istu podjelu ovlastenja, pa im ne treba svaki
/// svoj servis. Zapisi se ovdje ne pretvaraju u modele nego ostaju mape - ekran koji
/// ih prikazuje je generican i cita ih po kljucu iz definicije.
class SifrarnikCrudServis {
  const SifrarnikCrudServis(this._klijent);

  final ApiKlijent _klijent;

  Future<Strana<Map<String, dynamic>>> lista(
    String putanja, {
    int stranica = 0,
    int velicinaStranice = 20,
  }) async {
    final odgovor = await _klijent.get(
      putanja,
      upit: {
        'page': stranica,
        'pageSize': velicinaStranice,
        'includeTotalCount': true,
      },
    );

    return Strana.izJsona(odgovor as Map<String, dynamic>, (json) => json);
  }

  Future<void> dodaj(String putanja, Map<String, dynamic> zahtjev) async {
    await _klijent.post(putanja, tijelo: zahtjev);
  }

  Future<void> izmijeni(
    String putanja,
    int id,
    Map<String, dynamic> zahtjev,
  ) async {
    await _klijent.put('$putanja/$id', tijelo: zahtjev);
  }

  Future<void> obrisi(String putanja, int id) async {
    await _klijent.delete('$putanja/$id');
  }
}
