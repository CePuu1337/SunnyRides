import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../modeli/placanje.dart';

/// Rezervacije prijavljenog klijenta.
///
/// Nijedan poziv ne salje iznos ni status - server ih racuna i postavlja sam.
class RezervacijaServis {
  const RezervacijaServis(this._klijent);

  final ApiKlijent _klijent;

  Future<Strana<Rezervacija>> moje({
    required int stranica,
    int velicinaStranice = 10,
    StatusRezervacije? status,
    bool? aktivne,
  }) async {
    final odgovor = await _klijent.get(
      '/api/rezervacije',
      upit: {
        'page': stranica,
        'pageSize': velicinaStranice,
        'includeTotalCount': true,
        'orderBy': 'DatumOd desc',
        'status': status?.vrijednost,
        'samoAktivne': aktivne,
      },
    );

    return Strana.izJsona(_mapa(odgovor), Rezervacija.izJsona);
  }

  Future<Rezervacija> detalji(int id) async {
    final odgovor = await _klijent.get('/api/rezervacije/$id');

    return Rezervacija.izJsona(_mapa(odgovor));
  }

  Future<Rezervacija> kreiraj(ZahtjevRezervacije zahtjev) async {
    final odgovor = await _klijent.post(
      '/api/rezervacije',
      tijelo: zahtjev.uJson(),
    );

    return Rezervacija.izJsona(_mapa(odgovor));
  }

  Future<ObracunOtkazivanja> obracunOtkazivanja(int id) async {
    final odgovor = await _klijent.get(
      '/api/rezervacije/$id/obracun-otkazivanja',
    );

    return ObracunOtkazivanja.izJsona(_mapa(odgovor));
  }

  Future<Rezervacija> otkazi(
    int id, {
    required int razlogId,
    String? napomena,
  }) async {
    final odgovor = await _klijent.post(
      '/api/rezervacije/$id/otkazi',
      tijelo: {'razlogOtkazivanjaId': razlogId, 'napomena': napomena},
    );

    return Rezervacija.izJsona(_mapa(odgovor));
  }

  /// Priprema naplatu. Tijela nema - iznos i valutu odredjuje server.
  Future<PlatniIntent> platniIntent(int rezervacijaId) async {
    final odgovor = await _klijent.post(
      '/api/rezervacije/$rezervacijaId/payment-intent',
    );

    return PlatniIntent.izJsona(_mapa(odgovor));
  }

  /// Serverska potvrda naplate. Uspjeh koji javi PaymentSheet nije dokaz - server
  /// pita Stripe i tek onda mijenja status.
  Future<bool> potvrdiPlacanje(int placanjeId) async {
    final odgovor = await _klijent.post('/api/placanja/$placanjeId/confirm');

    return citajBool(_mapa(odgovor)['isPaid']);
  }

  Future<List<RazlogOtkazivanja>> razloziOtkazivanja() async {
    final odgovor = await _klijent.get(
      '/api/razlozi-otkazivanja',
      upit: {
        'page': 0,
        'pageSize': 50,
        'includeTotalCount': false,
        'orderBy': 'Naziv',
        'zaKlijenta': true,
        'aktivan': true,
      },
    );

    return Strana.izJsona(_mapa(odgovor), RazlogOtkazivanja.izJsona).stavke;
  }

  static Map<String, dynamic> _mapa(dynamic odgovor) {
    return odgovor is Map<String, dynamic> ? odgovor : const {};
  }
}

/// Razlog iz padajuce liste pri otkazivanju.
class RazlogOtkazivanja {
  const RazlogOtkazivanja({
    required this.id,
    required this.naziv,
    required this.traziNapomenu,
  });

  final int id;
  final String naziv;

  /// Uz neke razloge ("Ostalo") napomena je obavezna.
  final bool traziNapomenu;

  factory RazlogOtkazivanja.izJsona(Map<String, dynamic> json) {
    return RazlogOtkazivanja(
      id: citajInt(json['id']),
      naziv: json['naziv']?.toString() ?? '',
      traziNapomenu: citajBool(json['traziNapomenu']),
    );
  }
}
