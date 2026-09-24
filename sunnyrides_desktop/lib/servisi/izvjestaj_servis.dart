import 'dart:typed_data';

import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../modeli/izvjestaj.dart';

/// Koji izvjestaj se gleda. Oba imaju isti oblik poziva, pa se razlikuju samo rutom.
enum VrstaIzvjestaja {
  iskoristenostFlote('iskoristenost-flote', 'Iskorištenost flote'),
  finansijskiPregled('finansijski-pregled', 'Finansijski pregled');

  const VrstaIzvjestaja(this.ruta, this.naziv);

  final String ruta;
  final String naziv;
}

class IzvjestajServis {
  const IzvjestajServis(this._klijent);

  final ApiKlijent _klijent;

  Map<String, dynamic> _upit(
    DateTime od,
    DateTime doDatuma,
    int? poslovnicaId,
  ) {
    return {'od': od, 'do': doDatuma, 'poslovnicaId': poslovnicaId};
  }

  Future<IskoristenostFlote> iskoristenost(
    DateTime od,
    DateTime doDatuma,
    int? poslovnicaId,
  ) async {
    final odgovor = await _klijent.get(
      '/api/izvjestaji/iskoristenost-flote',
      upit: _upit(od, doDatuma, poslovnicaId),
    );

    return IskoristenostFlote.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<FinansijskiPregled> finansijski(
    DateTime od,
    DateTime doDatuma,
    int? poslovnicaId,
  ) async {
    final odgovor = await _klijent.get(
      '/api/izvjestaji/finansijski-pregled',
      upit: _upit(od, doDatuma, poslovnicaId),
    );

    return FinansijskiPregled.izJsona(odgovor as Map<String, dynamic>);
  }

  /// PDF se ne gradi u aplikaciji nego na serveru, iz istog podatka koji je pregled
  /// vec prikazao - pa dokument i pregled ne mogu pokazivati razlicite brojeve.
  Future<Uint8List> pdf(
    VrstaIzvjestaja vrsta,
    DateTime od,
    DateTime doDatuma,
    int? poslovnicaId,
  ) {
    return _klijent.bajtovi(
      '/api/izvjestaji/${vrsta.ruta}/pdf',
      upit: _upit(od, doDatuma, poslovnicaId),
    );
  }
}
