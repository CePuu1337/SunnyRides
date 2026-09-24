import 'package:flutter/foundation.dart';
import 'package:signalr_netcore/signalr_client.dart';

import '../api/okruzenje.dart';
import '../auth/pohrana_tokena.dart';

/// Veza sa hubom kroz koju obavjestenja stizu cim nastanu.
///
/// Hub nema nijednu metodu koju aplikacija moze pozvati - veza samo slusa. Server
/// korisnika cita iz tokena i sam ga svrstava u njegovu grupu, pa se ovdje ne prijavljuje
/// nista; da se prijavljuje, bilo bi dovoljno poslati tudji broj i slusati tudja
/// obavjestenja.
class VezaNotifikacija {
  VezaNotifikacija({required this.okruzenje, required this._pohrana});

  static const putanja = '/hubs/notifikacije';

  final Okruzenje okruzenje;
  final PohranaTokena _pohrana;

  HubConnection? _veza;

  /// Stize uz svako novo obavjestenje, zajedno sa brojem neprocitanih - da aplikacija
  /// zbog oznake na zvonu ne mora raditi jos jedan zahtjev.
  void Function(Map<String, dynamic> notifikacija)? naNovuNotifikaciju;
  void Function(int broj)? naBrojNeprocitanih;

  Future<void> poveziSe() async {
    if (_veza != null) {
      return;
    }

    final veza = HubConnectionBuilder()
        .withUrl(
          okruzenje.osnovnaAdresa + putanja,
          options: HttpConnectionOptions(
            // Token se cita pri svakom uspostavljanju veze, ne jednom pri gradnji -
            // ponovno povezivanje nakon prekida inace bi poslalo stari token.
            accessTokenFactory: () async => _pohrana.token ?? '',
          ),
        )
        .withAutomaticReconnect()
        .build();

    veza.on('NovaNotifikacija', (argumenti) {
      final prva = argumenti == null || argumenti.isEmpty ? null : argumenti.first;

      if (prva is Map<String, dynamic>) {
        naNovuNotifikaciju?.call(prva);
      }
    });

    veza.on('BrojNeprocitanih', (argumenti) {
      final prva = argumenti == null || argumenti.isEmpty ? null : argumenti.first;

      if (prva is Map<String, dynamic>) {
        final broj = prva['broj'];

        if (broj is num) {
          naBrojNeprocitanih?.call(broj.toInt());
        }
      }
    });

    _veza = veza;

    try {
      await veza.start();
    } catch (greska) {
      // Bez ove veze aplikacija radi - broj na zvonu se tada osvjezava zahtjevom.
      // Zato prekid veze nije greska koju korisnik treba vidjeti.
      debugPrint('Veza sa hubom nije uspostavljena: $greska');
    }
  }

  Future<void> raskini() async {
    final veza = _veza;
    _veza = null;

    if (veza == null) {
      return;
    }

    try {
      await veza.stop();
    } catch (_) {
      // Zatvaranje veze koja je vec pukla nema sta da prijavi.
    }
  }
}
