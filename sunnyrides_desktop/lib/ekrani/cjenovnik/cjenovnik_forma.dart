import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/cjenovnik.dart';
import '../../modeli/stavka_sifrarnika.dart';
import '../../servisi/cjenovnik_servis.dart';
import '../../servisi/sifrarnik_servis.dart';
import '../../widgeti/dijalog_forme.dart';
import '../../widgeti/obavjestenje.dart';

/// Unos sezonske tarife za jedan model vozila.
class CjenovnikForma extends StatefulWidget {
  const CjenovnikForma({super.key, this.cjenovnik});

  final Cjenovnik? cjenovnik;

  @override
  State<CjenovnikForma> createState() => _CjenovnikFormaStanje();
}

class _CjenovnikFormaStanje extends State<CjenovnikForma> {
  final _forma = GlobalKey<FormState>();

  late final TextEditingController _naziv;
  late final TextEditingController _mnozilac;
  late final TextEditingController _satna;
  late final TextEditingController _dnevna;
  late final TextEditingController _prag1;
  late final TextEditingController _procenat1;
  late final TextEditingController _prag2;
  late final TextEditingController _procenat2;

  late final CjenovnikServis _servis;
  late final SifrarnikServis _sifrarnici;

  List<StavkaSifrarnika> _modeli = const [];
  int? _modelId;
  late DateTime _datumOd;
  late DateTime _datumDo;

  bool _ucitavanje = true;
  bool _snimanje = false;
  String? _greska;

  bool get _jeIzmjena => widget.cjenovnik != null;

  @override
  void initState() {
    super.initState();

    final klijent = context.read<ApiKlijent>();
    _servis = CjenovnikServis(klijent);
    _sifrarnici = SifrarnikServis(klijent);

    final cjenovnik = widget.cjenovnik;

    _naziv = TextEditingController(text: cjenovnik?.naziv ?? '');
    _mnozilac = TextEditingController(
      text: (cjenovnik?.mnozilac ?? 1).toStringAsFixed(2),
    );
    _satna = TextEditingController(text: _iznos(cjenovnik?.satnaTarifa));
    _dnevna = TextEditingController(text: _iznos(cjenovnik?.dnevnaTarifa));
    _prag1 = TextEditingController(
      text: (cjenovnik?.popustPrag1 ?? 7).toString(),
    );
    _procenat1 = TextEditingController(
      text: (cjenovnik?.popustProcenat1 ?? 10).toStringAsFixed(0),
    );
    _prag2 = TextEditingController(
      text: (cjenovnik?.popustPrag2 ?? 30).toString(),
    );
    _procenat2 = TextEditingController(
      text: (cjenovnik?.popustProcenat2 ?? 20).toStringAsFixed(0),
    );

    _modelId = cjenovnik?.modelVozilaId;
    _datumOd = cjenovnik?.datumOd.toLocal() ?? DateTime.now();
    _datumDo =
        cjenovnik?.datumDo.toLocal() ??
        DateTime.now().add(const Duration(days: 90));

    _ucitajModele();
  }

  static String _iznos(double? vrijednost) =>
      vrijednost == null ? '' : vrijednost.toStringAsFixed(2);

  @override
  void dispose() {
    _naziv.dispose();
    _mnozilac.dispose();
    _satna.dispose();
    _dnevna.dispose();
    _prag1.dispose();
    _procenat1.dispose();
    _prag2.dispose();
    _procenat2.dispose();
    super.dispose();
  }

  Future<void> _ucitajModele() async {
    try {
      final modeli = await _sifrarnici.ucitaj(SifrarnikServis.modeliVozila);

      if (mounted) {
        setState(() {
          _modeli = modeli;
          _ucitavanje = false;
        });
      }
    } on ApiGreska catch (greska) {
      if (mounted) {
        setState(() {
          _ucitavanje = false;
          _greska = greska.poruka;
        });
      }
    }
  }

  Future<void> _odaberiDatum({required bool pocetak}) async {
    final datum = await showDatePicker(
      context: context,
      initialDate: pocetak ? _datumOd : _datumDo,
      firstDate: DateTime(DateTime.now().year - 1),
      lastDate: DateTime(DateTime.now().year + 3, 12, 31),
    );

    if (datum == null) {
      return;
    }

    setState(() {
      if (pocetak) {
        _datumOd = datum;

        if (!_datumDo.isAfter(_datumOd)) {
          _datumDo = _datumOd.add(const Duration(days: 30));
        }
      } else {
        _datumDo = datum;
      }
    });
  }

  double? _broj(String tekst) {
    final ociscen = tekst.trim().replaceAll(',', '.');

    return ociscen.isEmpty ? null : double.tryParse(ociscen);
  }

  Future<void> _sacuvaj() async {
    if (!_forma.currentState!.validate()) {
      return;
    }

    if (_modelId == null) {
      setState(() => _greska = 'Odaberite model vozila.');

      return;
    }

    if (!_datumDo.isAfter(_datumOd)) {
      setState(() => _greska = 'Kraj sezone mora biti poslije početka.');

      return;
    }

    setState(() {
      _snimanje = true;
      _greska = null;
    });

    final zahtjev = <String, dynamic>{
      'modelVozilaId': _modelId,
      'naziv': _naziv.text.trim(),
      'datumOd': _datumOd.toUtc().toIso8601String(),
      'datumDo': _datumDo.toUtc().toIso8601String(),
      'mnozilac': _broj(_mnozilac.text) ?? 1,
      'satnaTarifa': _broj(_satna.text),
      'dnevnaTarifa': _broj(_dnevna.text),
      'popustPrag1': int.tryParse(_prag1.text.trim()) ?? 0,
      'popustProcenat1': _broj(_procenat1.text) ?? 0,
      'popustPrag2': int.tryParse(_prag2.text.trim()) ?? 0,
      'popustProcenat2': _broj(_procenat2.text) ?? 0,
    };

    try {
      if (_jeIzmjena) {
        await _servis.izmijeni(widget.cjenovnik!.id, zahtjev);
      } else {
        await _servis.dodaj(zahtjev);
      }

      if (!mounted) {
        return;
      }

      Navigator.of(context).pop(true);
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() {
        _snimanje = false;
        _greska = greska.poruka;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return DijalogForme(
      naslov: _jeIzmjena ? 'Izmjena tarife' : 'Nova sezonska tarifa',
      podnaslov: 'Vrijedi za jedan model vozila u zadatom periodu',
      greska: _greska,
      uToku: _snimanje,
      sirina: 680,
      naSnimanje: _sacuvaj,
      dijete: _ucitavanje
          ? const Padding(
              padding: EdgeInsets.all(Razmaci.xxl),
              child: Center(child: CircularProgressIndicator()),
            )
          : Form(
              key: _forma,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  RedPolja(
                    lijevo: TextFormField(
                      controller: _naziv,
                      decoration: const InputDecoration(
                        labelText: 'Naziv sezone',
                        hintText: 'Glavna sezona 2026',
                      ),
                      validator: (vrijednost) {
                        final tekst = vrijednost?.trim() ?? '';

                        if (tekst.length < 2 || tekst.length > 100) {
                          return 'Naziv mora imati između 2 i 100 znakova.';
                        }

                        return null;
                      },
                    ),
                    desno: DropdownButtonFormField<int>(
                      initialValue: _modeli.any((x) => x.id == _modelId)
                          ? _modelId
                          : null,
                      isExpanded: true,
                      decoration: const InputDecoration(
                        labelText: 'Model vozila',
                      ),
                      items: [
                        for (final model in _modeli)
                          DropdownMenuItem(
                            value: model.id,
                            child: Text(model.naziv),
                          ),
                      ],
                      onChanged: (id) => setState(() => _modelId = id),
                      validator: (vrijednost) =>
                          vrijednost == null ? 'Odaberite model.' : null,
                    ),
                  ),
                  const SizedBox(height: Razmaci.l),
                  RedPolja(
                    lijevo: _PoljeDatuma(
                      natpis: 'Sezona od',
                      datum: _datumOd,
                      naPritisak: () => _odaberiDatum(pocetak: true),
                    ),
                    desno: _PoljeDatuma(
                      natpis: 'Sezona do',
                      datum: _datumDo,
                      naPritisak: () => _odaberiDatum(pocetak: false),
                    ),
                  ),
                  const SizedBox(height: Razmaci.xl),
                  Obavjestenje.info(
                    'Tarife su neobavezne — prazno znači da se koristi ona upisana na '
                    'samom vozilu. Množilac se primjenjuje na tarifu koja na kraju '
                    'važi, pa 1,30 znači trideset posto skuplje.',
                  ),
                  const SizedBox(height: Razmaci.l),
                  RedPolja(
                    lijevo: TextFormField(
                      controller: _mnozilac,
                      keyboardType: const TextInputType.numberWithOptions(
                        decimal: true,
                      ),
                      decoration: const InputDecoration(labelText: 'Množilac'),
                      validator: (vrijednost) {
                        final broj = _broj(vrijednost ?? '');

                        if (broj == null || broj < 0.1 || broj > 10) {
                          return 'Množilac mora biti između 0,1 i 10.';
                        }

                        return null;
                      },
                    ),
                    desno: const SizedBox.shrink(),
                  ),
                  const SizedBox(height: Razmaci.l),
                  RedPolja(
                    lijevo: TextFormField(
                      controller: _satna,
                      keyboardType: const TextInputType.numberWithOptions(
                        decimal: true,
                      ),
                      decoration: const InputDecoration(
                        labelText: 'Satna tarifa',
                        suffixText: '€',
                        helperText: 'Prazno — sa vozila',
                      ),
                      validator: (vrijednost) => _tarifa(vrijednost),
                    ),
                    desno: TextFormField(
                      controller: _dnevna,
                      keyboardType: const TextInputType.numberWithOptions(
                        decimal: true,
                      ),
                      decoration: const InputDecoration(
                        labelText: 'Dnevna tarifa',
                        suffixText: '€',
                        helperText: 'Prazno — sa vozila',
                      ),
                      validator: (vrijednost) => _tarifa(vrijednost),
                    ),
                  ),
                  const SizedBox(height: Razmaci.xl),
                  const Text(
                    'Pragovi popusta',
                    style: TextStyle(
                      fontSize: 13.5,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: Razmaci.xs),
                  const Text(
                    'Najam duži od praga dobija odgovarajući popust. Pragovi su ovdje, '
                    'a ne u kodu, jer klijentu na detaljima vozila treba prikazati '
                    'tačno one koji se stvarno primjenjuju.',
                    style: TextStyle(
                      color: Boje.tekstPrigusen,
                      fontSize: 12,
                      height: 1.4,
                    ),
                  ),
                  const SizedBox(height: Razmaci.m),
                  RedPolja(
                    lijevo: TextFormField(
                      controller: _prag1,
                      keyboardType: TextInputType.number,
                      inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                      decoration: const InputDecoration(
                        labelText: 'Prvi prag',
                        suffixText: 'dana',
                      ),
                      validator: (vrijednost) => _prag(vrijednost),
                    ),
                    desno: TextFormField(
                      controller: _procenat1,
                      keyboardType: const TextInputType.numberWithOptions(
                        decimal: true,
                      ),
                      decoration: const InputDecoration(
                        labelText: 'Popust',
                        suffixText: '%',
                      ),
                      validator: (vrijednost) => _procenat(vrijednost),
                    ),
                  ),
                  const SizedBox(height: Razmaci.l),
                  RedPolja(
                    lijevo: TextFormField(
                      controller: _prag2,
                      keyboardType: TextInputType.number,
                      inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                      decoration: const InputDecoration(
                        labelText: 'Drugi prag',
                        suffixText: 'dana',
                      ),
                      validator: (vrijednost) => _prag(vrijednost),
                    ),
                    desno: TextFormField(
                      controller: _procenat2,
                      keyboardType: const TextInputType.numberWithOptions(
                        decimal: true,
                      ),
                      decoration: const InputDecoration(
                        labelText: 'Popust',
                        suffixText: '%',
                      ),
                      validator: (vrijednost) => _procenat(vrijednost),
                    ),
                  ),
                ],
              ),
            ),
    );
  }

  String? _tarifa(String? vrijednost) {
    final tekst = (vrijednost ?? '').trim();

    if (tekst.isEmpty) {
      return null;
    }

    final broj = _broj(tekst);

    if (broj == null || broj <= 0 || broj > 10000) {
      return 'Tarifa mora biti veća od nule.';
    }

    return null;
  }

  String? _prag(String? vrijednost) {
    final broj = int.tryParse((vrijednost ?? '').trim());

    if (broj == null || broj < 1 || broj > 365) {
      return 'Prag mora biti između 1 i 365 dana.';
    }

    return null;
  }

  String? _procenat(String? vrijednost) {
    final broj = _broj(vrijednost ?? '');

    if (broj == null || broj < 0 || broj > 100) {
      return 'Popust mora biti između 0 i 100 posto.';
    }

    return null;
  }
}

class _PoljeDatuma extends StatelessWidget {
  const _PoljeDatuma({
    required this.natpis,
    required this.datum,
    required this.naPritisak,
  });

  final String natpis;
  final DateTime datum;
  final VoidCallback naPritisak;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: naPritisak,
      borderRadius: BorderRadius.circular(Zaobljenja.polje),
      child: InputDecorator(
        decoration: InputDecoration(
          labelText: natpis,
          suffixIcon: const Icon(Icons.event_outlined, size: 18),
        ),
        child: Text(Formati.datum(datum)),
      ),
    );
  }
}
