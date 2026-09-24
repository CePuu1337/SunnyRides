import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/cjenovnik.dart';
import '../../modeli/stavka_sifrarnika.dart';
import '../../servisi/cjenovnik_servis.dart';
import '../../servisi/sifrarnik_servis.dart';
import '../../widgeti/kartica.dart';
import '../../widgeti/paginator.dart';
import '../../widgeti/polja.dart';
import '../../widgeti/sadrzaj.dart';
import 'cjenovnik_forma.dart';

class CjenovnikEkran extends StatefulWidget {
  const CjenovnikEkran({super.key});

  @override
  State<CjenovnikEkran> createState() => _CjenovnikEkranStanje();
}

class _CjenovnikEkranStanje extends State<CjenovnikEkran> {
  late final CjenovnikServis _servis;
  late final SifrarnikServis _sifrarnici;

  Strana<Cjenovnik> _strana = Strana.prazna();
  List<StavkaSifrarnika> _modeli = const [];

  String _naziv = '';
  int? _modelId;
  bool _samoVazece = false;
  int _stranica = 0;

  static const _velicinaStranice = 15;

  bool _ucitavanje = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    final klijent = context.read<ApiKlijent>();
    _servis = CjenovnikServis(klijent);
    _sifrarnici = SifrarnikServis(klijent);

    _ucitajModele();
    _ucitaj();
  }

  Future<void> _ucitajModele() async {
    try {
      final modeli = await _sifrarnici.ucitaj(SifrarnikServis.modeliVozila);

      if (mounted) {
        setState(() => _modeli = modeli);
      }
    } on ApiGreska {
      // Filter po modelu ostaje prazan.
    }
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      final strana = await _servis.lista(
        naziv: _naziv.isEmpty ? null : _naziv,
        modelVozilaId: _modelId,
        vaziNaDatum: _samoVazece ? DateTime.now() : null,
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

  Future<void> _otvoriFormu({Cjenovnik? cjenovnik}) async {
    final sacuvano = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (context) => CjenovnikForma(cjenovnik: cjenovnik),
    );

    if (sacuvano == true) {
      _ucitaj();
    }
  }

  Future<void> _obrisi(Cjenovnik cjenovnik) async {
    final potvrda = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Brisanje tarife'),
        content: Text(
          '„${cjenovnik.naziv}" se briše. Rezervacije napravljene po njoj zadržavaju '
          'iznos koji im je već obračunat.',
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
      await _servis.obrisi(cjenovnik.id);
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
    final danas = DateTime.now();

    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(Razmaci.ekranMargina),
        child: Kartica(
          naslov: 'Cjenovnik',
          podnaslov: 'Sezonske tarife, množioci i pragovi popusta',
          bezUnutrasnjegRazmaka: true,
          rastegni: true,
          akcija: ElevatedButton.icon(
            onPressed: () => _otvoriFormu(),
            icon: const Icon(Icons.add, size: 18),
            label: const Text('Nova tarifa'),
          ),
          dijete: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Padding(
                padding: const EdgeInsets.all(Razmaci.karticaUnutra),
                child: Wrap(
                  spacing: Razmaci.m,
                  runSpacing: Razmaci.m,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    PoljePretrage(
                      natpis: 'Naziv sezone',
                      naPromjenu: (tekst) {
                        _naziv = tekst;
                        _stranica = 0;
                        _ucitaj();
                      },
                    ),
                    PadajuciSifrarnik(
                      natpis: 'Model vozila',
                      sirina: 240,
                      stavke: _modeli,
                      odabrano: _modelId,
                      naPromjenu: (id) {
                        setState(() {
                          _modelId = id;
                          _stranica = 0;
                        });
                        _ucitaj();
                      },
                    ),
                    FilterChip(
                      label: const Text('Vrijedi danas'),
                      selected: _samoVazece,
                      onSelected: (odabrano) {
                        setState(() {
                          _samoVazece = odabrano;
                          _stranica = 0;
                        });
                        _ucitaj();
                      },
                    ),
                  ],
                ),
              ),
              const Divider(height: 1),
              Expanded(
                child: Sadrzaj(
                  ucitavanje: _ucitavanje,
                  greska: _greska,
                  naPonovniPokusaj: _ucitaj,
                  dijete: _strana.jePrazna
                      ? const PrazanPopis(
                          poruka: 'Nema tarifa po ovim filterima.',
                          ikona: Icons.sell_outlined,
                        )
                      : _Tabela(
                          tarife: _strana.stavke,
                          danas: danas,
                          naIzmjenu: (tarifa) =>
                              _otvoriFormu(cjenovnik: tarifa),
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
    );
  }
}

class _Tabela extends StatelessWidget {
  const _Tabela({
    required this.tarife,
    required this.danas,
    required this.naIzmjenu,
    required this.naBrisanje,
  });

  final List<Cjenovnik> tarife;
  final DateTime danas;
  final ValueChanged<Cjenovnik> naIzmjenu;
  final ValueChanged<Cjenovnik> naBrisanje;

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
              dataRowMinHeight: 52,
              dataRowMaxHeight: 58,
              columns: const [
                DataColumn(label: Text('SEZONA')),
                DataColumn(label: Text('MODEL')),
                DataColumn(label: Text('PERIOD')),
                DataColumn(label: Text('MNOŽILAC')),
                DataColumn(label: Text('SAT / DAN')),
                DataColumn(label: Text('POPUSTI')),
                DataColumn(label: Text('')),
              ],
              rows: [
                for (final tarifa in tarife)
                  DataRow(
                    cells: [
                      DataCell(
                        Row(
                          children: [
                            Text(
                              tarifa.naziv,
                              style: const TextStyle(
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                            if (tarifa.vaziNa(danas)) ...[
                              const SizedBox(width: Razmaci.s),
                              const StatusnaPilula(
                                tekst: 'Vrijedi',
                                pozadina: Boje.uspjehPozadina,
                                bojaTeksta: Boje.uspjehTekst,
                              ),
                            ],
                          ],
                        ),
                      ),
                      DataCell(Text(tarifa.model)),
                      DataCell(
                        Text(
                          '${Formati.danIMjesec(tarifa.datumOd)} – '
                          '${Formati.datum(tarifa.datumDo)}',
                        ),
                      ),
                      DataCell(
                        Text(
                          '× ${tarifa.mnozilac.toStringAsFixed(2)}',
                          style: TextStyle(
                            fontWeight: FontWeight.w600,
                            color: tarifa.mnozilac > 1
                                ? Boje.upozorenjeTekst
                                : tarifa.mnozilac < 1
                                ? Boje.uspjehTekst
                                : Boje.tekst,
                          ),
                        ),
                      ),
                      DataCell(
                        Text(
                          '${tarifa.satnaTarifa == null ? '—' : Formati.novac(tarifa.satnaTarifa!)}'
                          ' / '
                          '${tarifa.dnevnaTarifa == null ? '—' : Formati.novac(tarifa.dnevnaTarifa!)}',
                        ),
                      ),
                      DataCell(
                        Text(
                          '${tarifa.popustPrag1}d → ${tarifa.popustProcenat1.toStringAsFixed(0)} %'
                          '   ·   '
                          '${tarifa.popustPrag2}d → ${tarifa.popustProcenat2.toStringAsFixed(0)} %',
                          style: const TextStyle(fontSize: 12),
                        ),
                      ),
                      DataCell(
                        Row(
                          children: [
                            IconButton(
                              tooltip: 'Izmijeni',
                              onPressed: () => naIzmjenu(tarifa),
                              icon: const Icon(Icons.edit_outlined, size: 18),
                            ),
                            IconButton(
                              tooltip: 'Obriši',
                              onPressed: () => naBrisanje(tarifa),
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
}
