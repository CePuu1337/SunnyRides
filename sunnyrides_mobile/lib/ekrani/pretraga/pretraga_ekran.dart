import 'dart:async';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/vozilo.dart';
import '../../servisi/katalog_servis.dart';
import '../../stanje/navigacija.dart';
import '../../widgeti/kartica_vozila.dart';
import '../../widgeti/obavjestenje.dart';
import '../vozila/detalji_vozila_ekran.dart';
import 'filteri.dart';

/// Pretraga ponude: tekst, filteri, poredak i lista rezultata.
class PretragaEkran extends StatefulWidget {
  const PretragaEkran({super.key});

  @override
  State<PretragaEkran> createState() => _PretragaEkranStanje();
}

class _PretragaEkranStanje extends State<PretragaEkran> {
  static const _velicinaStranice = 10;

  late final KatalogServis _katalog;
  late final Navigacija _navigacija;

  final _polje = TextEditingController();
  final _skrol = ScrollController();

  Timer? _odgoda;

  Filteri _filteri = const Filteri();
  List<Stavka> _tipovi = const [];
  List<Stavka> _gradovi = const [];
  DozvoljeneKategorije? _kategorije;

  final List<Vozilo> _rezultati = [];
  int _stranica = 0;
  int? _ukupno;

  bool _ucitavanje = true;
  bool _dopunjavanje = false;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _katalog = KatalogServis(context.read<ApiKlijent>());
    _navigacija = context.read<Navigacija>()..addListener(_naZahtjev);

    _skrol.addListener(_naSkrol);

    _pripremi();
  }

  @override
  void dispose() {
    _odgoda?.cancel();
    _navigacija.removeListener(_naZahtjev);
    _skrol.dispose();
    _polje.dispose();
    super.dispose();
  }

  /// Zahtjev sa pocetnog ekrana - tekst ili odabrani tip vozila.
  void _naZahtjev() {
    final zahtjev = _navigacija.preuzmiZahtjev();

    if (zahtjev == null) {
      return;
    }

    _polje.text = zahtjev.tekst ?? '';

    setState(() {
      _filteri = _filteri.kopija(
        tekst: zahtjev.tekst,
        tipVozilaId: zahtjev.tipVozilaId,
      );
    });

    _trazi();
  }

  Future<void> _pripremi() async {
    try {
      final rezultati = await Future.wait([
        _katalog.tipoviVozila(),
        _katalog.gradovi(),
        _katalog.mojeKategorije(),
      ]);

      if (!mounted) {
        return;
      }

      setState(() {
        _tipovi = rezultati[0] as List<Stavka>;
        _gradovi = rezultati[1] as List<Stavka>;
        _kategorije = rezultati[2] as DozvoljeneKategorije;
      });
    } on ApiGreska {
      // Sifarnici su pomoc pri filtriranju. Ako ne stignu, pretraga radi i bez njih.
    }

    await _trazi();
  }

  Future<void> _trazi() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
      _stranica = 0;
    });

    try {
      final strana = await _upit(0);

      if (!mounted) {
        return;
      }

      setState(() {
        _rezultati
          ..clear()
          ..addAll(strana.stavke);
        _ukupno = strana.ukupno;
        _ucitavanje = false;
      });
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() {
        _greska = greska.poruka;
        _ucitavanje = false;
      });
    }
  }

  Future<Strana<Vozilo>> _upit(int stranica) {
    return _katalog.vozila(
      upit: OsnovniUpit(
        stranica: stranica,
        velicinaStranice: _velicinaStranice,
        sortiranje: _filteri.poredak.vrijednost,
      ),
      modelNaziv: _filteri.tekst,
      tipVozilaId: _filteri.tipVozilaId,
      gradId: _filteri.gradId,
      cijenaDo: _filteri.cijenaDo,
      slobodnoOd: _filteri.imaTermin ? _filteri.datumOd : null,
      slobodnoDo: _filteri.imaTermin ? _filteri.datumDo : null,
    );
  }

  void _naSkrol() {
    if (_skrol.position.pixels < _skrol.position.maxScrollExtent - 240) {
      return;
    }

    _dopuni();
  }

  Future<void> _dopuni() async {
    final ukupno = _ukupno;

    if (_dopunjavanje || _ucitavanje) {
      return;
    }

    if (ukupno != null && _rezultati.length >= ukupno) {
      return;
    }

    setState(() => _dopunjavanje = true);

    try {
      final strana = await _upit(_stranica + 1);

      if (!mounted) {
        return;
      }

      setState(() {
        _stranica += 1;
        _rezultati.addAll(strana.stavke);
        _ukupno = strana.ukupno ?? _ukupno;
        _dopunjavanje = false;
      });
    } on ApiGreska {
      if (!mounted) {
        return;
      }

      setState(() => _dopunjavanje = false);
    }
  }

  void _naTekst(String tekst) {
    // Upit ide poslije kratke pauze, da se ne salje po jedan zahtjev na svaki znak.
    _odgoda?.cancel();
    _odgoda = Timer(const Duration(milliseconds: 400), () {
      final ocisceno = tekst.trim();

      setState(() {
        _filteri = _filteri.kopija(tekst: ocisceno.isEmpty ? null : ocisceno);
      });

      _trazi();
    });
  }

  Future<void> _otvoriFiltere() async {
    final novi = await showModalBottomSheet<Filteri>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) =>
          ListFiltera(pocetni: _filteri, tipovi: _tipovi, gradovi: _gradovi),
    );

    if (novi == null || !mounted) {
      return;
    }

    setState(() => _filteri = novi);
    await _trazi();
  }

  void _otvoriVozilo(Vozilo vozilo) {
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => DetaljiVozilaEkran(
          voziloId: vozilo.id,
          datumOd: _filteri.imaTermin ? _filteri.datumOd : null,
          datumDo: _filteri.imaTermin ? _filteri.datumDo : null,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final kategorije = _kategorije;
    final brojFiltera = _filteri.brojAktivnih;

    return Scaffold(
      backgroundColor: Boje.platno,
      appBar: AppBar(
        title: const Text('Pretraga'),
        actions: [
          Stack(
            children: [
              IconButton(
                onPressed: _otvoriFiltere,
                icon: const Icon(Icons.tune),
                tooltip: 'Filteri',
              ),
              if (brojFiltera > 0)
                Positioned(
                  right: 8,
                  top: 8,
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 4),
                    decoration: BoxDecoration(
                      color: Boje.primarnaTamnija,
                      borderRadius: BorderRadius.circular(Zaobljenja.pilula),
                    ),
                    child: Text(
                      '$brojFiltera',
                      style: const TextStyle(
                        fontSize: 9.5,
                        fontWeight: FontWeight.w700,
                        color: Colors.white,
                      ),
                    ),
                  ),
                ),
            ],
          ),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(Razmaci.l),
            child: TextField(
              controller: _polje,
              onChanged: _naTekst,
              textInputAction: TextInputAction.search,
              decoration: InputDecoration(
                hintText: 'Naziv modela, npr. Vespa',
                prefixIcon: const Icon(Icons.search, size: 20),
                suffixIcon: _polje.text.isEmpty
                    ? null
                    : IconButton(
                        icon: const Icon(Icons.close, size: 18),
                        onPressed: () {
                          _polje.clear();
                          _naTekst('');
                        },
                      ),
              ),
            ),
          ),
          if (kategorije != null)
            Padding(
              padding: const EdgeInsets.fromLTRB(
                Razmaci.l,
                0,
                Razmaci.l,
                Razmaci.m,
              ),
              child: kategorije.mozeRezervisati
                  ? Obavjestenje.info(
                      kategorije.obrazlozenje,
                      naslov: 'Prikaz je usklađen sa vašom dozvolom',
                    )
                  : Obavjestenje.upozorenje(
                      kategorije.obrazlozenje,
                      naslov: 'Vozačka dozvola nije spremna',
                    ),
            ),
          _Poredak(
            odabrani: _filteri.poredak,
            ukupno: _ukupno,
            naPromjenu: (poredak) {
              setState(() => _filteri = _filteri.kopija(poredak: poredak));
              _trazi();
            },
          ),
          Expanded(
            child: Sadrzaj(
              ucitavanje: _ucitavanje,
              greska: _greska,
              naPonovniPokusaj: _trazi,
              dijete: _rezultati.isEmpty
                  ? PrazanPopis(
                      poruka: brojFiltera > 0 || _filteri.tekst != null
                          ? 'Nema vozila za zadate uslove. Probajte drugi termin '
                                'ili manje filtera.'
                          : 'Trenutno nema vozila u ponudi.',
                      ikona: Icons.search_off,
                      akcija: brojFiltera == 0
                          ? null
                          : OutlinedButton(
                              onPressed: () {
                                setState(
                                  () => _filteri = Filteri(
                                    poredak: _filteri.poredak,
                                  ),
                                );
                                _polje.clear();
                                _trazi();
                              },
                              child: const Text('Poništi filtere'),
                            ),
                    )
                  : RefreshIndicator(
                      onRefresh: _trazi,
                      child: ListView.separated(
                        controller: _skrol,
                        padding: const EdgeInsets.fromLTRB(
                          Razmaci.l,
                          0,
                          Razmaci.l,
                          Razmaci.xl,
                        ),
                        itemCount: _rezultati.length + (_dopunjavanje ? 1 : 0),
                        separatorBuilder: (_, _) =>
                            const SizedBox(height: Razmaci.m),
                        itemBuilder: (context, indeks) {
                          if (indeks >= _rezultati.length) {
                            return const Padding(
                              padding: EdgeInsets.all(Razmaci.l),
                              child: Center(child: CircularProgressIndicator()),
                            );
                          }

                          final vozilo = _rezultati[indeks];

                          return KarticaVozila(
                            vozilo: vozilo,
                            ocjena: vozilo.prosjecnaOcjena,
                            naDodir: () => _otvoriVozilo(vozilo),
                          );
                        },
                      ),
                    ),
            ),
          ),
        ],
      ),
    );
  }
}

class _Poredak extends StatelessWidget {
  const _Poredak({
    required this.odabrani,
    required this.naPromjenu,
    this.ukupno,
  });

  final Poredak odabrani;
  final int? ukupno;
  final ValueChanged<Poredak> naPromjenu;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(Razmaci.l, 0, Razmaci.s, Razmaci.s),
      child: Row(
        children: [
          Expanded(
            child: Text(
              ukupno == null ? '' : '$ukupno ${_oblik(ukupno!)}',
              style: const TextStyle(fontSize: 12.5, color: Boje.tekstPrigusen),
            ),
          ),
          PopupMenuButton<Poredak>(
            initialValue: odabrani,
            onSelected: naPromjenu,
            tooltip: 'Poredak',
            itemBuilder: (context) => [
              for (final poredak in Poredak.values)
                PopupMenuItem<Poredak>(
                  value: poredak,
                  child: Text(poredak.naziv),
                ),
            ],
            child: Padding(
              padding: const EdgeInsets.symmetric(
                horizontal: Razmaci.s,
                vertical: Razmaci.xs,
              ),
              child: Row(
                children: [
                  const Icon(Icons.sort, size: 17, color: Boje.tekstBlazi),
                  const SizedBox(width: Razmaci.xs),
                  Text(
                    odabrani.naziv,
                    style: const TextStyle(
                      fontSize: 12.5,
                      color: Boje.tekstBlazi,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  static String _oblik(int broj) {
    if (broj % 10 == 1 && broj % 100 != 11) {
      return 'vozilo';
    }

    if (broj % 10 >= 2 &&
        broj % 10 <= 4 &&
        (broj % 100 < 12 || broj % 100 > 14)) {
      return 'vozila';
    }

    return 'vozila';
  }
}
