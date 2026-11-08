import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Jedan pokušaj naplate rezervacije, sa povratima koji su iz njega isplaćeni.
class Placanje {
  const Placanje({
    required this.id,
    required this.rezervacijaId,
    required this.rezervacijaBroj,
    required this.klijentImePrezime,
    required this.poslovnicaNaziv,
    required this.iznos,
    required this.naplaceniIznos,
    required this.valuta,
    required this.status,
    required this.datumKreiranja,
    required this.ukupnoVraceno,
    required this.povrati,
  });

  final int id;
  final int rezervacijaId;
  final String rezervacijaBroj;
  final String klijentImePrezime;
  final String poslovnicaNaziv;

  /// Iznos koji je server zatražio od Stripe-a.
  final double iznos;

  /// Iznos koji je Stripe stvarno primio; prazan dok naplata nije uspjela.
  final double? naplaceniIznos;

  final String valuta;
  final StatusPlacanja? status;
  final DateTime datumKreiranja;

  /// Zbir povrata koji nisu odbijeni ni poništeni - novac koji je stvarno vraćen
  /// ili je na putu.
  final double ukupnoVraceno;

  final List<Povrat> povrati;

  bool get imaNeuspjeliPovrat =>
      povrati.any((x) => x.status == StatusPlacanja.neuspjesno);

  factory Placanje.izJsona(Map<String, dynamic> json) {
    return Placanje(
      id: citajInt(json['id']),
      rezervacijaId: citajInt(json['rezervacijaId']),
      rezervacijaBroj: json['rezervacijaBroj']?.toString() ?? '',
      klijentImePrezime: json['klijentImePrezime']?.toString() ?? '',
      poslovnicaNaziv: json['poslovnicaNaziv']?.toString() ?? '',
      iznos: citajDouble(json['iznos']),
      naplaceniIznos: citajDoubleIliNista(json['naplaceniIznos']),
      valuta: json['valuta']?.toString() ?? 'EUR',
      status: StatusPlacanja.izBroja(citajIntIliNista(json['status'])),
      datumKreiranja: citajDatum(json['datumKreiranja']),
      ukupnoVraceno: citajDouble(json['ukupnoVraceno']),
      povrati: [
        for (final povrat in (json['povrati'] as List<dynamic>? ?? const []))
          Povrat.izJsona(povrat as Map<String, dynamic>),
      ],
    );
  }
}

/// Jedan povrat novca uz plaćanje.
class Povrat {
  const Povrat({
    required this.id,
    required this.iznos,
    required this.razlog,
    required this.status,
    required this.kreiraoKorisnikIme,
    required this.datumKreiranja,
  });

  final int id;
  final double iznos;
  final String razlog;
  final StatusPlacanja? status;

  /// Ko je povrat pokrenuo. Prazno kad ga je pokrenuo sistem (npr. istek držanja).
  final String? kreiraoKorisnikIme;

  final DateTime datumKreiranja;

  /// Stripe ga je odbio, pa novac nije vraćen. Osoblje ga može poslati ponovo.
  bool get mozeSePonoviti => status == StatusPlacanja.neuspjesno;

  factory Povrat.izJsona(Map<String, dynamic> json) {
    return Povrat(
      id: citajInt(json['id']),
      iznos: citajDouble(json['iznos']),
      razlog: json['razlog']?.toString() ?? '',
      status: StatusPlacanja.izBroja(citajIntIliNista(json['status'])),
      kreiraoKorisnikIme: json['kreiraoKorisnikIme']?.toString(),
      datumKreiranja: citajDatum(json['datumKreiranja']),
    );
  }
}
