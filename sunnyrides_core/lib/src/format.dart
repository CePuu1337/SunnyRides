import 'package:intl/intl.dart';

/// Formatiranje brojeva, novca i datuma, na jednom mjestu.
///
/// Server salje vrijeme u UTC-u, a korisnik ga gleda u svojoj zoni. Pretvaranje se
/// radi ovdje, u trenutku ispisa - ako se radi negdje ranije, prije ili kasnije se
/// desi da isti datum na dva ekrana pise razlicito.
class Formati {
  const Formati._();

  static final _novac = NumberFormat('#,##0.00', 'bs');
  static final _cijelBroj = NumberFormat('#,##0', 'bs');
  static final _decimala = NumberFormat('#,##0.#', 'bs');

  static const _mjeseci = <String>[
    'januar',
    'februar',
    'mart',
    'april',
    'maj',
    'juni',
    'juli',
    'avgust',
    'septembar',
    'oktobar',
    'novembar',
    'decembar',
  ];

  /// Iznos sa valutom, na primjer "1.234,56 €".
  static String novac(num iznos) => '${_novac.format(iznos)} €';

  /// Iznos bez valute, kad kolona vec ima zaglavlje sa oznakom.
  static String iznos(num vrijednost) => _novac.format(vrijednost);

  static String broj(num vrijednost) => _cijelBroj.format(vrijednost);

  static String postotak(num vrijednost) => '${_decimala.format(vrijednost)} %';

  /// Promjena u odnosu na raniji period, sa predznakom.
  static String promjena(num vrijednost) {
    final znak = vrijednost > 0 ? '+' : '';

    return '$znak${_decimala.format(vrijednost)} %';
  }

  static String datum(DateTime vrijeme) {
    final lokalno = vrijeme.toLocal();

    return DateFormat('dd.MM.yyyy.').format(lokalno);
  }

  static String vrijeme(DateTime trenutak) {
    return DateFormat('HH:mm').format(trenutak.toLocal());
  }

  static String datumIVrijeme(DateTime trenutak) {
    return DateFormat('dd.MM.yyyy. HH:mm').format(trenutak.toLocal());
  }

  /// Kratki oblik za tabele i kalendar, bez godine.
  static String danIMjesec(DateTime trenutak) {
    return DateFormat('dd.MM.').format(trenutak.toLocal());
  }

  /// Naziv mjeseca na nasem jeziku. DateFormat bi ga ispisao engleski dok se ne
  /// ucitaju podaci o jeziku, a za dvanaest rijeci to ne vrijedi vuci.
  static String mjesec(DateTime trenutak) => _mjeseci[trenutak.toLocal().month - 1];

  /// Razmak izmedju dva trenutka, u obliku "3 dana" ili "5 sati".
  static String trajanje(DateTime od, DateTime doTrenutka) {
    final razlika = doTrenutka.difference(od);

    if (razlika.inHours < 24) {
      final sati = razlika.inHours;

      return '$sati ${_oblik(sati, 'sat', 'sata', 'sati')}';
    }

    final dana = razlika.inDays;

    return '$dana ${_oblik(dana, 'dan', 'dana', 'dana')}';
  }

  static String _oblik(int broj, String jednina, String malo, String mnozina) {
    final zadnja = broj % 10;
    final zadnjeDvije = broj % 100;

    if (zadnja == 1 && zadnjeDvije != 11) {
      return jednina;
    }

    if (zadnja >= 2 && zadnja <= 4 && (zadnjeDvije < 12 || zadnjeDvije > 14)) {
      return malo;
    }

    return mnozina;
  }
}
