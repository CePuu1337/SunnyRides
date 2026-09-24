import 'dart:typed_data';

import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Vlastiti nalog i vlastita dozvola.
///
/// Nijedan poziv ne nosi identifikator korisnika - server ga cita iz tokena, pa se
/// tudji profil ne moze ni adresirati.
class ProfilServis {
  const ProfilServis(this._klijent);

  final ApiKlijent _klijent;

  Future<Korisnik> moj() async {
    final odgovor = await _klijent.get('/api/profil');

    return Korisnik.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<Korisnik> azuriraj({
    required String ime,
    required String prezime,
    required String email,
    required String telefon,
    required DateTime datumRodjenja,
  }) async {
    final odgovor = await _klijent.put(
      '/api/profil',
      tijelo: {
        'ime': ime,
        'prezime': prezime,
        'email': email,
        'telefon': telefon.isEmpty ? null : telefon,
        'datumRodjenja': datumRodjenja.toUtc().toIso8601String(),
      },
    );

    return Korisnik.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<Korisnik> postaviSliku({
    required String imeFajla,
    required List<int> sadrzaj,
  }) async {
    final odgovor = await _klijent.posaljiFajl(
      '/api/profil/slika',
      nazivPolja: 'fajl',
      imeFajla: imeFajla,
      sadrzaj: sadrzaj,
    );

    return Korisnik.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<Korisnik> ukloniSliku() async {
    final odgovor = await _klijent.delete('/api/profil/slika');

    return Korisnik.izJsona(odgovor as Map<String, dynamic>);
  }

  /// Vlastita dozvola, ili nista kad je klijent jos nije prijavio.
  Future<VozackaDozvola?> mojaDozvola() async {
    final odgovor = await _klijent.get('/api/dozvole/moja');

    return odgovor is Map<String, dynamic>
        ? VozackaDozvola.izJsona(odgovor)
        : null;
  }

  Future<VozackaDozvola> prijaviDozvolu({
    required String brojDozvole,
    required DateTime datumIzdavanja,
    required DateTime datumIsteka,
    required List<int> kategorijaIds,
  }) async {
    final odgovor = await _klijent.post(
      '/api/dozvole',
      tijelo: {
        'brojDozvole': brojDozvole,
        'datumIzdavanja': datumIzdavanja.toUtc().toIso8601String(),
        'datumIsteka': datumIsteka.toUtc().toIso8601String(),
        'kategorijaIds': kategorijaIds,
      },
    );

    return VozackaDozvola.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<VozackaDozvola> postaviFotografiju({
    required StranaDozvole strana,
    required String imeFajla,
    required List<int> sadrzaj,
  }) async {
    final odgovor = await _klijent.posaljiFajl(
      '/api/dozvole/moja/fotografija?strana=${strana.vrijednost}',
      nazivPolja: 'fajl',
      imeFajla: imeFajla,
      sadrzaj: sadrzaj,
    );

    return VozackaDozvola.izJsona(odgovor as Map<String, dynamic>);
  }

  Future<Uint8List> fotografija(int dozvolaId, StranaDozvole strana) {
    return _klijent.bajtovi(
      '/api/dozvole/$dozvolaId/fotografija',
      upit: {'strana': strana.vrijednost},
    );
  }

  Future<List<KategorijaDozvole>> kategorije() async {
    final odgovor = await _klijent.get(
      '/api/kategorije-dozvola',
      upit: {
        'page': 0,
        'pageSize': 50,
        'includeTotalCount': false,
        'orderBy': 'Oznaka',
      },
    );

    return Strana.izJsona(
      odgovor is Map<String, dynamic> ? odgovor : const {},
      KategorijaDozvole.izJsona,
    ).stavke;
  }
}

/// Kategorija dozvole iz sifarnika, sa oznakom i opisom.
class KategorijaDozvole {
  const KategorijaDozvole({
    required this.id,
    required this.oznaka,
    required this.opis,
  });

  final int id;
  final String oznaka;
  final String opis;

  factory KategorijaDozvole.izJsona(Map<String, dynamic> json) {
    return KategorijaDozvole(
      id: citajInt(json['id']),
      oznaka: json['oznaka']?.toString() ?? '',
      opis: json['opis']?.toString() ?? '',
    );
  }
}
