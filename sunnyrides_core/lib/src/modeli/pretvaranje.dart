/// Sitni pomocnici za citanje JSON-a, da se ista provjera ne pise u svakom modelu.
library;

int citajInt(dynamic vrijednost, {int podrazumijevano = 0}) {
  if (vrijednost is int) {
    return vrijednost;
  }

  if (vrijednost is num) {
    return vrijednost.toInt();
  }

  if (vrijednost is String) {
    return int.tryParse(vrijednost) ?? podrazumijevano;
  }

  return podrazumijevano;
}

double citajDouble(dynamic vrijednost, {double podrazumijevano = 0}) {
  if (vrijednost is num) {
    return vrijednost.toDouble();
  }

  if (vrijednost is String) {
    return double.tryParse(vrijednost) ?? podrazumijevano;
  }

  return podrazumijevano;
}

double? citajDoubleIliNista(dynamic vrijednost) {
  if (vrijednost == null) {
    return null;
  }

  return citajDouble(vrijednost);
}

bool citajBool(dynamic vrijednost, {bool podrazumijevano = false}) {
  if (vrijednost is bool) {
    return vrijednost;
  }

  if (vrijednost is String) {
    return vrijednost.toLowerCase() == 'true';
  }

  return podrazumijevano;
}

/// Server salje vrijeme u UTC-u. Ako oznaka vremenske zone nedostaje, datum se
/// svejedno tretira kao UTC - inace bi se pri prikazu pomjerio za razliku zone.
DateTime citajDatum(dynamic vrijednost) {
  return citajDatumIliNista(vrijednost) ?? DateTime.fromMillisecondsSinceEpoch(0, isUtc: true);
}

DateTime? citajDatumIliNista(dynamic vrijednost) {
  if (vrijednost == null) {
    return null;
  }

  final tekst = vrijednost.toString();

  if (tekst.isEmpty) {
    return null;
  }

  final datum = DateTime.tryParse(tekst);

  if (datum == null) {
    return null;
  }

  return datum.isUtc ? datum : DateTime.utc(
    datum.year,
    datum.month,
    datum.day,
    datum.hour,
    datum.minute,
    datum.second,
    datum.millisecond,
  );
}

List<String> citajTekstove(dynamic vrijednost) {
  if (vrijednost is List) {
    return vrijednost.map((x) => x.toString()).toList();
  }

  return const [];
}

List<T> citajListu<T>(dynamic vrijednost, T Function(Map<String, dynamic>) pretvori) {
  if (vrijednost is List) {
    return vrijednost
        .whereType<Map<String, dynamic>>()
        .map(pretvori)
        .toList();
  }

  return <T>[];
}
