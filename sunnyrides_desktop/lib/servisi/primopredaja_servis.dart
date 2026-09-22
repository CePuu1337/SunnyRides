import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../modeli/primopredaja.dart';

class PrimopredajaServis {
  const PrimopredajaServis(this._klijent);

  final ApiKlijent _klijent;

  Future<Strana<RasporedStavka>> raspored({
    required DateTime od,
    required DateTime doDatuma,
    int? poslovnicaId,
    int stranica = 0,
    int velicinaStranice = 50,
  }) async {
    final odgovor = await _klijent.get(
      '/api/primopredaje/raspored',
      upit: {
        'od': od,
        'do': doDatuma,
        'poslovnicaId': poslovnicaId,
        'page': stranica,
        'pageSize': velicinaStranice,
        'includeTotalCount': true,
      },
    );

    return Strana.izJsona(
      odgovor as Map<String, dynamic>,
      RasporedStavka.izJsona,
    );
  }

  /// Obracun za formu povrata. Nista ne upisuje, pa se moze zvati na svaku izmjenu.
  Future<ObracunPovrata> obracunPovrata(
    int rezervacijaId, {
    double? iznosStete,
    DateTime? datumPovrata,
  }) async {
    final odgovor = await _klijent.get(
      '/api/primopredaje/obracun-povrata/$rezervacijaId',
      upit: {'iznosStete': iznosStete, 'datumPovrata': datumPovrata},
    );

    return ObracunPovrata.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<Primopredaja> izdaj({
    required int rezervacijaId,
    required int kilometraza,
    required int nivoGoriva,
    required bool kontrolnaListaProdjena,
    String? napomena,
    DateTime? datumIzdavanja,
    List<FajlZaSlanje> fotografije = const [],
  }) async {
    final odgovor = await _klijent.posaljiFormu(
      '/api/primopredaje/izdavanje',
      polja: {
        'rezervacijaId': rezervacijaId,
        'kilometraza': kilometraza,
        'nivoGoriva': nivoGoriva,
        'kontrolnaListaProdjena': kontrolnaListaProdjena,
        'napomena': napomena,
        'datumIzdavanja': datumIzdavanja,
      },
      fajlovi: fotografije,
    );

    return Primopredaja.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<Primopredaja> vrati({
    required int rezervacijaId,
    required int kilometraza,
    required int nivoGoriva,
    required bool imaOstecenje,
    String? opisStete,
    double? iznosStete,
    String? napomena,
    DateTime? blokirajVoziloDo,
    DateTime? datumPovrata,
    List<FajlZaSlanje> fotografije = const [],
  }) async {
    final odgovor = await _klijent.posaljiFormu(
      '/api/primopredaje/povrat',
      polja: {
        'rezervacijaId': rezervacijaId,
        'kilometraza': kilometraza,
        'nivoGoriva': nivoGoriva,
        'imaOstecenje': imaOstecenje,
        'opisStete': opisStete,
        'iznosStete': iznosStete,
        'napomena': napomena,
        'blokirajVoziloDo': blokirajVoziloDo,
        'datumPovrata': datumPovrata,
      },
      fajlovi: fotografije,
    );

    return Primopredaja.izJsona(odgovor as Map<String, dynamic>);
  }
}
