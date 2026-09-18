import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../modeli/rezervacija.dart';

class RezervacijaServis {
  const RezervacijaServis(this._klijent);

  final ApiKlijent _klijent;

  Future<Strana<Rezervacija>> lista(UpitRezervacija upit) async {
    final odgovor = await _klijent.get('/api/rezervacije', upit: upit.uMapu());

    return Strana.izJsona(odgovor as Map<String, dynamic>, Rezervacija.izJsona);
  }

  Future<Rezervacija> detalji(int id) async {
    final odgovor = await _klijent.get('/api/rezervacije/$id');

    return Rezervacija.izJsona(odgovor as Map<String, dynamic>);
  }

  /// Koliko bi se vratilo kad bi se rezervacija otkazala sada.
  ///
  /// Iznos racuna server iz stvarno naplacenog, ne aplikacija iz cjenovnika -
  /// inace bi se dva racuna mogla razici, a klijentu bi se prikazao onaj pogresan.
  Future<ObracunOtkazivanja> obracunOtkazivanja(int id) async {
    final odgovor = await _klijent.get(
      '/api/rezervacije/$id/obracun-otkazivanja',
    );

    return ObracunOtkazivanja.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<void> otkazi(int id, {required int razlogId, String? napomena}) async {
    await _klijent.post(
      '/api/rezervacije/$id/otkazi',
      tijelo: {'razlogOtkazivanjaId': razlogId, 'napomena': napomena},
    );
  }

  /// Rucni unos rezervacije za klijenta, sa salterom. Identifikator klijenta ide u
  /// putanju - za sve ostalo server uzima prijavljenog korisnika iz tokena.
  Future<Rezervacija> kreirajZaKlijenta(
    int klijentId,
    Map<String, dynamic> zahtjev,
  ) async {
    final odgovor = await _klijent.post(
      '/api/rezervacije/klijent/$klijentId',
      tijelo: zahtjev,
    );

    return Rezervacija.izJsona(odgovor as Map<String, dynamic>);
  }
}
