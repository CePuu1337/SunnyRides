/// Jedna stranica rezultata, onako kako je vraca svaki list endpoint.
class Strana<T> {
  const Strana({required this.stavke, this.ukupno});

  final List<T> stavke;

  /// Ukupan broj zapisa, ako je zatrazen. Paginator bez njega ne zna koliko stranica ima.
  final int? ukupno;

  bool get jePrazna => stavke.isEmpty;

  static Strana<T> izJsona<T>(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) pretvori,
  ) {
    final stavke = (json['items'] as List<dynamic>? ?? const [])
        .map((x) => pretvori(x as Map<String, dynamic>))
        .toList();

    return Strana<T>(stavke: stavke, ukupno: json['totalCount'] as int?);
  }

  static Strana<T> prazna<T>() => Strana<T>(stavke: const [], ukupno: 0);
}

/// Parametri koje razumije svaki list endpoint.
///
/// Stranica se broji od nule, jer tako broji i server. Ekran koji korisniku pise
/// "stranica 1" sam dodaje jedinicu.
class OsnovniUpit {
  const OsnovniUpit({
    this.stranica = 0,
    this.velicinaStranice = 20,
    this.sortiranje,
    this.ukljuciUkupno = true,
  });

  final int stranica;
  final int velicinaStranice;
  final String? sortiranje;
  final bool ukljuciUkupno;

  Map<String, dynamic> uMapu() {
    return {
      'page': stranica,
      'pageSize': velicinaStranice,
      if (sortiranje != null && sortiranje!.isNotEmpty) 'orderBy': sortiranje,
      'includeTotalCount': ukljuciUkupno,
    };
  }
}
