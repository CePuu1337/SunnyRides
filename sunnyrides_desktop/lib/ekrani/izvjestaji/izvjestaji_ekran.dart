import 'dart:typed_data';

import 'package:file_selector/file_selector.dart';
import 'package:flutter/material.dart';
import 'package:printing/printing.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/izvjestaj.dart';
import '../../modeli/stavka_sifrarnika.dart';
import '../../servisi/izvjestaj_servis.dart';
import '../../servisi/sifrarnik_servis.dart';
import '../../widgeti/kartica.dart';
import '../../widgeti/polja.dart';
import '../../widgeti/sadrzaj.dart';

/// Izvjestaji: parametri, pregled prije generisanja, pa PDF.
///
/// Pregled nije ukras nego uslov iz uputstva - korisnik provjeri sta je odabrao prije
/// nego dobije dokument. PDF se gradi na serveru iz istog podatka koji pregled vec
/// prikazuje, pa dokument i ekran ne mogu pokazivati razlicite brojeve.
class IzvjestajiEkran extends StatefulWidget {
  const IzvjestajiEkran({super.key});

  @override
  State<IzvjestajiEkran> createState() => _IzvjestajiEkranStanje();
}

class _IzvjestajiEkranStanje extends State<IzvjestajiEkran> {
  late final IzvjestajServis _servis;
  late final SifrarnikServis _sifrarnici;

  VrstaIzvjestaja _vrsta = VrstaIzvjestaja.iskoristenostFlote;
  late DateTime _od;
  late DateTime _doDatuma;
  int? _poslovnicaId;

  List<StavkaSifrarnika> _poslovnice = const [];
  IskoristenostFlote? _flota;
  FinansijskiPregled? _finansije;

  bool _ucitavanje = true;
  bool _pdfUToku = false;
  String? _greska;

  @override
  void initState() {
    super.initState();

    final klijent = context.read<ApiKlijent>();
    _servis = IzvjestajServis(klijent);
    _sifrarnici = SifrarnikServis(klijent);

    // Podrazumijevano zadnjih dvanaest mjeseci - dovoljno da se vidi sezonalnost,
    // a da izvjestaj ostane citljiv.
    final danas = DateTime.now();
    _doDatuma = DateTime(danas.year, danas.month, danas.day);
    _od = DateTime(danas.year - 1, danas.month, danas.day);

    _ucitajPoslovnice();
    _ucitaj();
  }

  Future<void> _ucitajPoslovnice() async {
    try {
      final poslovnice = await _sifrarnici.ucitaj(SifrarnikServis.poslovnice);

      if (mounted) {
        setState(() => _poslovnice = poslovnice);
      }
    } on ApiGreska {
      // Filter ostaje prazan, izvjestaj se i dalje moze generisati za cijelu agenciju.
    }
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      if (_vrsta == VrstaIzvjestaja.iskoristenostFlote) {
        final flota = await _servis.iskoristenost(
          _od,
          _doDatuma,
          _poslovnicaId,
        );

        if (!mounted) {
          return;
        }

        setState(() {
          _flota = flota;
          _ucitavanje = false;
        });
      } else {
        final finansije = await _servis.finansijski(
          _od,
          _doDatuma,
          _poslovnicaId,
        );

        if (!mounted) {
          return;
        }

        setState(() {
          _finansije = finansije;
          _ucitavanje = false;
        });
      }
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

  Future<void> _odaberiPeriod() async {
    final raspon = await showDateRangePicker(
      context: context,
      firstDate: DateTime(2020),
      lastDate: DateTime(DateTime.now().year + 1, 12, 31),
      initialDateRange: DateTimeRange(start: _od, end: _doDatuma),
      helpText: 'Period izvještaja',
      saveText: 'Primijeni',
    );

    if (raspon == null) {
      return;
    }

    setState(() {
      _od = raspon.start;
      _doDatuma = raspon.end;
    });

    _ucitaj();
  }

  Future<Uint8List> _pdf() {
    return _servis.pdf(_vrsta, _od, _doDatuma, _poslovnicaId);
  }

  String get _imeFajla {
    final period = '${Formati.datum(_od)}-${Formati.datum(_doDatuma)}'
        .replaceAll('.', '')
        .replaceAll(' ', '');

    return '${_vrsta.ruta}-$period.pdf';
  }

  /// Stampa ide kroz sistemski dijalog, sa PDF-om koji je server vec napravio.
  Future<void> _stampaj() async {
    setState(() {
      _pdfUToku = true;
      _greska = null;
    });

    try {
      final bajtovi = await _pdf();

      await Printing.layoutPdf(
        onLayout: (format) async => bajtovi,
        name: _imeFajla,
      );
    } on ApiGreska catch (greska) {
      if (mounted) {
        setState(() => _greska = greska.poruka);
      }
    } finally {
      if (mounted) {
        setState(() => _pdfUToku = false);
      }
    }
  }

  Future<void> _preuzmi() async {
    setState(() {
      _pdfUToku = true;
      _greska = null;
    });

    try {
      final bajtovi = await _pdf();

      final mjesto = await getSaveLocation(
        suggestedName: _imeFajla,
        acceptedTypeGroups: const [
          XTypeGroup(label: 'PDF', extensions: ['pdf']),
        ],
      );

      if (mjesto == null) {
        return;
      }

      final fajl = XFile.fromData(
        bajtovi,
        mimeType: 'application/pdf',
        name: _imeFajla,
      );

      await fajl.saveTo(mjesto.path);

      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Izvještaj je sačuvan: ${mjesto.path}')),
      );
    } on ApiGreska catch (greska) {
      if (mounted) {
        setState(() => _greska = greska.poruka);
      }
    } finally {
      if (mounted) {
        setState(() => _pdfUToku = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(Razmaci.ekranMargina),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Kartica(
              naslov: 'Parametri izvještaja',
              podnaslov:
                  'Pregled se osvježava odmah, PDF se pravi tek na zahtjev',
              dijete: Wrap(
                spacing: Razmaci.m,
                runSpacing: Razmaci.m,
                crossAxisAlignment: WrapCrossAlignment.center,
                children: [
                  SizedBox(
                    width: 260,
                    child: DropdownButtonFormField<VrstaIzvjestaja>(
                      initialValue: _vrsta,
                      isExpanded: true,
                      decoration: const InputDecoration(labelText: 'Izvještaj'),
                      items: [
                        for (final vrsta in VrstaIzvjestaja.values)
                          DropdownMenuItem(
                            value: vrsta,
                            child: Text(vrsta.naziv),
                          ),
                      ],
                      onChanged: (vrsta) {
                        if (vrsta == null) {
                          return;
                        }

                        setState(() => _vrsta = vrsta);
                        _ucitaj();
                      },
                    ),
                  ),
                  OutlinedButton.icon(
                    onPressed: _odaberiPeriod,
                    icon: const Icon(Icons.date_range_outlined, size: 17),
                    label: Text(
                      '${Formati.datum(_od)} – ${Formati.datum(_doDatuma)}',
                    ),
                  ),
                  PadajuciSifrarnik(
                    natpis: 'Poslovnica',
                    svePoljeNatpis: 'Cijela agencija',
                    sirina: 230,
                    stavke: _poslovnice,
                    odabrano: _poslovnicaId,
                    naPromjenu: (id) {
                      setState(() => _poslovnicaId = id);
                      _ucitaj();
                    },
                  ),
                  const SizedBox(width: Razmaci.l),
                  OutlinedButton.icon(
                    onPressed: _pdfUToku ? null : _stampaj,
                    icon: const Icon(Icons.print_outlined, size: 17),
                    label: const Text('Ispis'),
                  ),
                  ElevatedButton.icon(
                    onPressed: _pdfUToku ? null : _preuzmi,
                    icon: _pdfUToku
                        ? const SizedBox(
                            width: 15,
                            height: 15,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Boje.naPrimarnoj,
                            ),
                          )
                        : const Icon(Icons.picture_as_pdf_outlined, size: 17),
                    label: const Text('Preuzmi PDF'),
                  ),
                ],
              ),
            ),
            const SizedBox(height: Razmaci.l),

            // Pregled zauzima sve sto pretekne ispod parametara, pa se na velikom
            // ekranu vidi vise redova umjesto praznine.
            Expanded(
              child: Sadrzaj(
                ucitavanje: _ucitavanje,
                greska: _greska,
                naPonovniPokusaj: _ucitaj,
                dijete: _vrsta == VrstaIzvjestaja.iskoristenostFlote
                    ? _PregledFlote(izvjestaj: _flota)
                    : _PregledFinansija(izvjestaj: _finansije),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _PregledFlote extends StatelessWidget {
  const _PregledFlote({required this.izvjestaj});

  final IskoristenostFlote? izvjestaj;

  @override
  Widget build(BuildContext context) {
    final podaci = izvjestaj;

    if (podaci == null) {
      return const SizedBox.shrink();
    }

    return Kartica(
      rastegni: true,
      naslov: 'Iskorištenost flote',
      podnaslov: podaci.poslovnica == null
          ? 'Cijela agencija'
          : 'Poslovnica: ${podaci.poslovnica}',
      bezUnutrasnjegRazmaka: true,
      dijete: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _Zbir(
            stavke: [
              ('Vozila', Formati.broj(podaci.zbir.brojVozila)),
              ('Najmova', Formati.broj(podaci.zbir.brojNajmova)),
              ('Dana izdato', podaci.zbir.danaIzdato.toStringAsFixed(1)),
              ('Iskorištenost', Formati.postotak(podaci.zbir.iskoristenost)),
              ('Prihod', Formati.novac(podaci.zbir.prihod)),
              (
                'Prosječna ocjena',
                podaci.zbir.prosjecnaOcjena == null
                    ? '—'
                    : podaci.zbir.prosjecnaOcjena!.toStringAsFixed(2),
              ),
            ],
          ),
          const Divider(height: 1),
          Expanded(
            child: podaci.stavke.isEmpty
                ? const PrazanPopis(
                    poruka: 'U ovom periodu nema podataka o najmovima.',
                    ikona: Icons.insert_chart_outlined,
                  )
                : _Tabela(
                    kolone: const [
                      'VOZILO',
                      'TIP',
                      'POSLOVNICA',
                      'NAJMOVA',
                      'DANA',
                      'ISKORIŠTENOST',
                      'PRIHOD',
                      'OCJENA',
                    ],
                    redovi: [
                      for (final stavka in podaci.stavke)
                        [
                          '${stavka.vozilo}\n${stavka.registarskaOznaka}',
                          stavka.tipVozila,
                          stavka.poslovnica,
                          Formati.broj(stavka.brojNajmova),
                          stavka.danaIzdato.toStringAsFixed(1),
                          Formati.postotak(stavka.iskoristenost),
                          Formati.novac(stavka.prihod),
                          stavka.prosjecnaOcjena == null
                              ? '—'
                              : '${stavka.prosjecnaOcjena!.toStringAsFixed(2)} '
                                    '(${stavka.brojOcjena})',
                        ],
                    ],
                  ),
          ),
        ],
      ),
    );
  }
}

class _PregledFinansija extends StatelessWidget {
  const _PregledFinansija({required this.izvjestaj});

  final FinansijskiPregled? izvjestaj;

  @override
  Widget build(BuildContext context) {
    final podaci = izvjestaj;

    if (podaci == null) {
      return const SizedBox.shrink();
    }

    return Kartica(
      rastegni: true,
      naslov: 'Finansijski pregled',
      podnaslov: 'Iz plaćanja, ne iz iznosa rezervacija — novac koji je stvarno prešao',
      bezUnutrasnjegRazmaka: true,
      dijete: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _Zbir(
            stavke: [
              ('Rezervacija', Formati.broj(podaci.zbir.brojRezervacija)),
              ('Naplaćeno', Formati.novac(podaci.zbir.naplaceno)),
              ('Refundirano', Formati.novac(podaci.zbir.refundirano)),
              ('Neto prihod', Formati.novac(podaci.zbir.netoPrihod)),
              (
                'Prosječan najam',
                Formati.novac(podaci.zbir.prosjecnaVrijednostNajma),
              ),
            ],
          ),
          const Divider(height: 1),
          Expanded(
            child: podaci.stavke.isEmpty
                ? const PrazanPopis(
                    poruka: 'U ovom periodu nema naplaćenih rezervacija.',
                    ikona: Icons.insert_chart_outlined,
                  )
                : _Tabela(
                    kolone: const [
                      'PERIOD',
                      'POSLOVNICA',
                      'REZERVACIJA',
                      'NAPLAĆENO',
                      'REFUNDIRANO',
                      'NETO',
                      'PROSJEK',
                    ],
                    redovi: [
                      for (final stavka in podaci.stavke)
                        [
                          stavka.period,
                          stavka.poslovnica,
                          Formati.broj(stavka.brojRezervacija),
                          Formati.novac(stavka.naplaceno),
                          Formati.novac(stavka.refundirano),
                          Formati.novac(stavka.netoPrihod),
                          Formati.novac(stavka.prosjecnaVrijednostNajma),
                        ],
                    ],
                  ),
          ),
        ],
      ),
    );
  }
}

class _Zbir extends StatelessWidget {
  const _Zbir({required this.stavke});

  final List<(String, String)> stavke;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(Razmaci.karticaUnutra),
      child: Wrap(
        spacing: Razmaci.xxl,
        runSpacing: Razmaci.l,
        children: [
          for (final (natpis, vrijednost) in stavke)
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  natpis.toUpperCase(),
                  style: const TextStyle(
                    color: Boje.tekstPrigusen,
                    fontSize: 11,
                    fontWeight: FontWeight.w600,
                    letterSpacing: 0.4,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  vrijednost,
                  style: const TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
        ],
      ),
    );
  }
}

class _Tabela extends StatelessWidget {
  const _Tabela({required this.kolone, required this.redovi});

  final List<String> kolone;
  final List<List<String>> redovi;

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
              dataRowMinHeight: 46,
              dataRowMaxHeight: 58,
              columns: [
                for (final kolona in kolone) DataColumn(label: Text(kolona)),
              ],
              rows: [
                for (final red in redovi)
                  DataRow(
                    cells: [for (final celija in red) DataCell(Text(celija))],
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
