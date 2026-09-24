import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/definicija_sifrarnika.dart';
import '../../servisi/sifrarnik_servis.dart';
import '../../widgeti/kartica.dart';
import '../../widgeti/paginator.dart';
import '../../widgeti/sadrzaj.dart';
import 'poslovnica_forma.dart';
import 'sifrarnik_forma.dart';

/// Odrzavanje sifrarnika: spisak lijevo, tabela odabranog desno.
class SifrarniciEkran extends StatefulWidget {
  const SifrarniciEkran({super.key});

  @override
  State<SifrarniciEkran> createState() => _SifrarniciEkranStanje();
}

class _SifrarniciEkranStanje extends State<SifrarniciEkran> {
  /// Poslovnice su sifrarnik kao i svaki drugi, ali imaju kartu za koordinate, pa
  /// im forma ostaje vlastita. U spisku stoje uz ostale.
  static const _poslovnice = DefinicijaSifrarnika(
    naziv: 'Poslovnice',
    jednina: 'poslovnicu',
    putanja: SifrarnikServis.poslovnice,
    opis: 'Mjesta preuzimanja vozila, sa lokacijom na karti',
    kolone: [
      KolonaSifrarnika(kljuc: 'naziv', natpis: 'NAZIV'),
      KolonaSifrarnika(kljuc: 'gradNaziv', natpis: 'GRAD', sirina: 160),
      KolonaSifrarnika(kljuc: 'adresa', natpis: 'ADRESA'),
      KolonaSifrarnika(
        kljuc: 'radnoVrijeme',
        natpis: 'RADNO VRIJEME',
        sirina: 220,
      ),
    ],
    polja: [],
  );

  static final _sviSifrarnici = <DefinicijaSifrarnika>[
    ...DefinicijaSifrarnika.sve,
    _poslovnice,
  ];

  late final SifrarnikCrudServis _servis;

  DefinicijaSifrarnika _odabrani = _sviSifrarnici.first;
  Strana<Map<String, dynamic>> _strana = Strana.prazna();
  int _stranica = 0;

  static const _velicinaStranice = 20;

  bool _ucitavanje = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = SifrarnikCrudServis(context.read<ApiKlijent>());
    _ucitaj();
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      final strana = await _servis.lista(
        _odabrani.putanja,
        stranica: _stranica,
        velicinaStranice: _velicinaStranice,
      );

      if (!mounted) {
        return;
      }

      setState(() {
        _strana = strana;
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

  void _odaberi(DefinicijaSifrarnika definicija) {
    setState(() {
      _odabrani = definicija;
      _stranica = 0;
      _strana = Strana.prazna();
    });

    _ucitaj();
  }

  Future<void> _otvoriFormu({Map<String, dynamic>? zapis}) async {
    final jePoslovnica = _odabrani.putanja == SifrarnikServis.poslovnice;

    final sacuvano = await showDialog<Object?>(
      context: context,
      barrierDismissible: false,
      builder: (context) => jePoslovnica
          ? PoslovnicaForma(poslovnica: zapis)
          : SifrarnikForma(definicija: _odabrani, zapis: zapis),
    );

    if (sacuvano != null) {
      _ucitaj();
    }
  }

  Future<void> _obrisi(Map<String, dynamic> zapis) async {
    final opis = zapis['naziv'] ?? zapis['oznaka'] ?? 'Odabrani zapis';

    final potvrda = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('Brisanje: ${_odabrani.nazivJednine}'),
        content: Text(
          '„$opis" se briše. Ako se zapis već koristi negdje u sistemu, server će '
          'brisanje odbiti i reći gdje.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Odustani'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Obriši'),
          ),
        ],
      ),
    );

    if (potvrda != true) {
      return;
    }

    try {
      await _servis.obrisi(_odabrani.putanja, citajInt(zapis['id']));
      _ucitaj();
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(greska.poruka)));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(Razmaci.ekranMargina),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            SizedBox(
              width: 250,
              child: Card(
                child: ListView(
                  padding: const EdgeInsets.symmetric(vertical: Razmaci.s),
                  children: [
                    for (final definicija in _sviSifrarnici)
                      _StavkaSpiska(
                        definicija: definicija,
                        odabrana: definicija.putanja == _odabrani.putanja,
                        naPritisak: () => _odaberi(definicija),
                      ),
                  ],
                ),
              ),
            ),
            const SizedBox(width: Razmaci.l),
            Expanded(
              child: Kartica(
                naslov: _odabrani.naziv,
                podnaslov: _odabrani.opis,
                bezUnutrasnjegRazmaka: true,
                rastegni: true,
                akcija: ElevatedButton.icon(
                  onPressed: () => _otvoriFormu(),
                  icon: const Icon(Icons.add, size: 18),
                  label: const Text('Novi unos'),
                ),
                dijete: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Expanded(
                      child: Sadrzaj(
                        ucitavanje: _ucitavanje,
                        greska: _greska,
                        naPonovniPokusaj: _ucitaj,
                        dijete: _strana.jePrazna
                            ? const PrazanPopis(
                                poruka: 'Ovaj šifarnik je prazan.',
                                ikona: Icons.list_alt_outlined,
                              )
                            : _Tabela(
                                definicija: _odabrani,
                                zapisi: _strana.stavke,
                                naIzmjenu: (zapis) =>
                                    _otvoriFormu(zapis: zapis),
                                naBrisanje: _obrisi,
                              ),
                      ),
                    ),
                    Paginator(
                      stranica: _stranica,
                      velicinaStranice: _velicinaStranice,
                      prikazano: _strana.stavke.length,
                      ukupno: _strana.ukupno,
                      naStranicu: (stranica) {
                        setState(() => _stranica = stranica);
                        _ucitaj();
                      },
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _StavkaSpiska extends StatelessWidget {
  const _StavkaSpiska({
    required this.definicija,
    required this.odabrana,
    required this.naPritisak,
  });

  final DefinicijaSifrarnika definicija;
  final bool odabrana;
  final VoidCallback naPritisak;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: odabrana ? Boje.primarnaSvijetla : Colors.transparent,
      child: InkWell(
        onTap: naPritisak,
        child: Container(
          padding: const EdgeInsets.symmetric(
            horizontal: Razmaci.l,
            vertical: Razmaci.m,
          ),
          decoration: BoxDecoration(
            border: Border(
              left: BorderSide(
                color: odabrana ? Boje.primarna : Colors.transparent,
                width: 3,
              ),
            ),
          ),
          child: Text(
            definicija.naziv,
            style: TextStyle(
              fontSize: 13.5,
              fontWeight: odabrana ? FontWeight.w600 : FontWeight.w400,
              color: odabrana ? Boje.primarnaTamnija : Boje.tekstBlazi,
            ),
          ),
        ),
      ),
    );
  }
}

class _Tabela extends StatelessWidget {
  const _Tabela({
    required this.definicija,
    required this.zapisi,
    required this.naIzmjenu,
    required this.naBrisanje,
  });

  final DefinicijaSifrarnika definicija;
  final List<Map<String, dynamic>> zapisi;
  final ValueChanged<Map<String, dynamic>> naIzmjenu;
  final ValueChanged<Map<String, dynamic>> naBrisanje;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, ogranicenja) => SingleChildScrollView(
        child: SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: ConstrainedBox(
            constraints: BoxConstraints(minWidth: ogranicenja.maxWidth),
            child: DataTable(
              columnSpacing: Razmaci.l,
              horizontalMargin: Razmaci.karticaUnutra,
              headingRowHeight: 40,
              dataRowMinHeight: 48,
              dataRowMaxHeight: 54,
              columns: [
                for (final kolona in definicija.kolone)
                  DataColumn(label: Text(kolona.natpis)),
                const DataColumn(label: Text('')),
              ],
              rows: [
                for (final zapis in zapisi)
                  DataRow(
                    cells: [
                      for (final kolona in definicija.kolone)
                        DataCell(_celija(kolona, zapis[kolona.kljuc])),
                      DataCell(
                        Row(
                          children: [
                            IconButton(
                              tooltip: 'Izmijeni',
                              onPressed: () => naIzmjenu(zapis),
                              icon: const Icon(Icons.edit_outlined, size: 18),
                            ),
                            IconButton(
                              tooltip: 'Obriši',
                              onPressed: () => naBrisanje(zapis),
                              icon: const Icon(
                                Icons.delete_outline,
                                size: 18,
                                color: Boje.greskaTekst,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _celija(KolonaSifrarnika kolona, dynamic vrijednost) {
    if (kolona.jeZastavica) {
      final tacno = citajBool(vrijednost);

      return Icon(
        tacno ? Icons.check_circle : Icons.remove,
        size: 17,
        color: tacno ? Boje.uspjeh : Boje.ivicaJaca,
      );
    }

    final tekst = vrijednost?.toString() ?? '';

    return SizedBox(
      width: kolona.sirina,
      child: Text(
        tekst.isEmpty ? '—' : tekst,
        overflow: TextOverflow.ellipsis,
        style: TextStyle(
          color: tekst.isEmpty ? Boje.tekstPrigusen : Boje.tekst,
        ),
      ),
    );
  }
}
