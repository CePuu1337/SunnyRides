import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../servisi/profil_servis.dart';
import '../../widgeti/obavjestenje.dart';

/// Prijava ili izmjena vlastite dozvole.
///
/// Status se ne bira - nova i izmijenjena dozvola uvijek idu na provjeru. Da klijent
/// moze postaviti status, sam bi sebi odobrio dozvolu.
class PrijavaDozvoleEkran extends StatefulWidget {
  const PrijavaDozvoleEkran({super.key, this.postojeca});

  final VozackaDozvola? postojeca;

  @override
  State<PrijavaDozvoleEkran> createState() => _PrijavaDozvoleEkranStanje();
}

class _PrijavaDozvoleEkranStanje extends State<PrijavaDozvoleEkran> {
  final _forma = GlobalKey<FormState>();

  late final ProfilServis _servis;
  late final TextEditingController _broj;

  DateTime? _izdavanje;
  DateTime? _istek;

  List<KategorijaDozvole> _kategorije = const [];
  final Set<int> _odabrane = {};

  bool _ucitavanje = true;
  bool _slanje = false;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = ProfilServis(context.read<ApiKlijent>());
    _broj = TextEditingController(text: widget.postojeca?.brojDozvole ?? '');

    final postojeca = widget.postojeca;

    if (postojeca != null) {
      _izdavanje = postojeca.datumIzdavanja.toLocal();
      _istek = postojeca.datumIsteka.toLocal();
      _odabrane.addAll(postojeca.kategorijaIds);
    }

    _ucitaj();
  }

  @override
  void dispose() {
    _broj.dispose();
    super.dispose();
  }

  Future<void> _ucitaj() async {
    try {
      final kategorije = await _servis.kategorije();

      if (!mounted) {
        return;
      }

      setState(() {
        _kategorije = kategorije;
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

  Future<void> _odaberiDatum({required bool izdavanje}) async {
    final danas = DateTime.now();
    final polazni = izdavanje ? _izdavanje : _istek;

    final datum = await showDatePicker(
      context: context,
      initialDate:
          polazni ?? (izdavanje ? danas : danas.add(const Duration(days: 365))),
      firstDate: DateTime(danas.year - 60),
      lastDate: DateTime(danas.year + 20),
      helpText: izdavanje ? 'Datum izdavanja' : 'Datum isteka',
    );

    if (datum == null) {
      return;
    }

    setState(() {
      if (izdavanje) {
        _izdavanje = datum;
      } else {
        _istek = datum;
      }
    });
  }

  Future<void> _sacuvaj() async {
    if (!(_forma.currentState?.validate() ?? false)) {
      return;
    }

    if (_izdavanje == null || _istek == null) {
      setState(() => _greska = 'Unesite datum izdavanja i datum isteka.');

      return;
    }

    if (_odabrane.isEmpty) {
      setState(() => _greska = 'Odaberite najmanje jednu kategoriju.');

      return;
    }

    setState(() {
      _slanje = true;
      _greska = null;
    });

    try {
      await _servis.prijaviDozvolu(
        brojDozvole: _broj.text.trim(),
        datumIzdavanja: _izdavanje!,
        datumIsteka: _istek!,
        kategorijaIds: _odabrane.toList(),
      );

      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Dozvola je poslana na provjeru.')),
      );

      Navigator.of(context).pop();
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() {
        _slanje = false;
        _greska = greska.poruka;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Boje.platno,
      appBar: AppBar(
        title: Text(
          widget.postojeca == null ? 'Prijava dozvole' : 'Izmjena dozvole',
        ),
      ),
      body: _ucitavanje
          ? const Center(child: CircularProgressIndicator())
          : Form(
              key: _forma,
              child: ListView(
                padding: const EdgeInsets.all(Razmaci.l),
                children: [
                  if (_greska != null) ...[
                    Obavjestenje.greska(_greska!),
                    const SizedBox(height: Razmaci.l),
                  ],
                  TextFormField(
                    controller: _broj,
                    decoration: const InputDecoration(
                      labelText: 'Broj dozvole',
                    ),
                    validator: (vrijednost) {
                      final tekst = vrijednost?.trim() ?? '';

                      if (tekst.length < 4) {
                        return 'Broj dozvole mora imati najmanje 4 znaka.';
                      }

                      return null;
                    },
                  ),
                  const SizedBox(height: Razmaci.m),
                  _DatumPolje(
                    natpis: 'Datum izdavanja',
                    vrijednost: _izdavanje,
                    naDodir: () => _odaberiDatum(izdavanje: true),
                  ),
                  const SizedBox(height: Razmaci.m),
                  _DatumPolje(
                    natpis: 'Datum isteka',
                    vrijednost: _istek,
                    naDodir: () => _odaberiDatum(izdavanje: false),
                  ),
                  const SizedBox(height: Razmaci.l),
                  const Text(
                    'Kategorije upisane na dozvoli',
                    style: TextStyle(
                      fontSize: 13.5,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: Razmaci.s),
                  Wrap(
                    spacing: Razmaci.s,
                    runSpacing: Razmaci.s,
                    children: [
                      for (final kategorija in _kategorije)
                        FilterChip(
                          label: Text(kategorija.oznaka),
                          tooltip: kategorija.opis,
                          selected: _odabrane.contains(kategorija.id),
                          onSelected: (odabrano) => setState(() {
                            if (odabrano) {
                              _odabrane.add(kategorija.id);
                            } else {
                              _odabrane.remove(kategorija.id);
                            }
                          }),
                        ),
                    ],
                  ),
                  const SizedBox(height: Razmaci.l),
                  Obavjestenje.info(
                    'Upišite samo kategorije koje stvarno stoje na dozvoli. '
                    'Uposlenik ih provjerava sa fotografije zadnje strane.',
                  ),
                  const SizedBox(height: Razmaci.xl),
                  FilledButton(
                    onPressed: _slanje ? null : _sacuvaj,
                    child: Text(_slanje ? 'Slanje...' : 'Pošalji na provjeru'),
                  ),
                ],
              ),
            ),
    );
  }
}

class _DatumPolje extends StatelessWidget {
  const _DatumPolje({
    required this.natpis,
    required this.vrijednost,
    required this.naDodir,
  });

  final String natpis;
  final DateTime? vrijednost;
  final VoidCallback naDodir;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: naDodir,
      borderRadius: BorderRadius.circular(Zaobljenja.polje),
      child: InputDecorator(
        decoration: InputDecoration(labelText: natpis),
        child: Text(
          vrijednost == null ? 'Odaberite datum' : Formati.datum(vrijednost!),
          style: TextStyle(
            color: vrijednost == null ? Boje.tekstPrigusen : Boje.tekst,
          ),
        ),
      ),
    );
  }
}
