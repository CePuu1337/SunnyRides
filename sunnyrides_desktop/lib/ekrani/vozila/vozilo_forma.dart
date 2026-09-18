import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/stavka_sifrarnika.dart';
import '../../modeli/vozilo.dart';
import '../../servisi/sifrarnik_servis.dart';
import '../../servisi/vozilo_servis.dart';
import '../../widgeti/dijalog_forme.dart';
import '../sifrarnici/poslovnica_forma.dart';
import 'galerija_vozila.dart';

/// Unos novog vozila i izmjena postojeceg, u istom dijalogu.
///
/// Kategorija vozacke dozvole se ne unosi - ona je svojstvo modela, ne primjerka,
/// pa se ovdje samo prikazuje kad se model odabere.
class VoziloForma extends StatefulWidget {
  const VoziloForma({super.key, this.vozilo});

  final Vozilo? vozilo;

  @override
  State<VoziloForma> createState() => _VoziloFormaStanje();
}

class _VoziloFormaStanje extends State<VoziloForma> {
  final _forma = GlobalKey<FormState>();

  late final VoziloServis _servis;
  late final SifrarnikServis _sifrarnici;

  late final TextEditingController _registracija;
  late final TextEditingController _godina;
  late final TextEditingController _kilometraza;
  late final TextEditingController _satna;
  late final TextEditingController _dnevna;
  late final TextEditingController _depozit;

  List<StavkaSifrarnika> _modeli = const [];
  List<StavkaSifrarnika> _poslovnice = const [];

  int? _modelId;
  int? _poslovnicaId;
  bool _aktivno = true;

  bool _ucitavanje = true;
  bool _snimanje = false;
  String? _greska;

  bool get _jeIzmjena => widget.vozilo != null;

  @override
  void initState() {
    super.initState();

    final klijent = context.read<ApiKlijent>();
    _servis = VoziloServis(klijent);
    _sifrarnici = SifrarnikServis(klijent);

    final vozilo = widget.vozilo;

    _registracija = TextEditingController(
      text: vozilo?.registarskaOznaka ?? '',
    );
    _godina = TextEditingController(
      text: (vozilo?.godinaProizvodnje ?? DateTime.now().year).toString(),
    );
    _kilometraza = TextEditingController(
      text: (vozilo?.kilometraza ?? 0).toString(),
    );
    _satna = TextEditingController(text: _iznos(vozilo?.satnaTarifa));
    _dnevna = TextEditingController(text: _iznos(vozilo?.dnevnaTarifa));
    _depozit = TextEditingController(text: _iznos(vozilo?.iznosDepozita));

    _modelId = vozilo?.modelVozilaId;
    _poslovnicaId = vozilo?.poslovnicaId;
    _aktivno = vozilo?.aktivno ?? true;

    _ucitajSifrarnike();
  }

  static String _iznos(double? vrijednost) {
    if (vrijednost == null) {
      return '';
    }

    return vrijednost.toStringAsFixed(2);
  }

  @override
  void dispose() {
    _registracija.dispose();
    _godina.dispose();
    _kilometraza.dispose();
    _satna.dispose();
    _dnevna.dispose();
    _depozit.dispose();
    super.dispose();
  }

  Future<void> _ucitajSifrarnike({bool osvjeziPoslovnice = false}) async {
    if (osvjeziPoslovnice) {
      _sifrarnici.zaboravi(SifrarnikServis.poslovnice);
    }

    try {
      final modeli = await _sifrarnici.ucitaj(SifrarnikServis.modeliVozila);
      final poslovnice = await _sifrarnici.ucitaj(SifrarnikServis.poslovnice);

      if (!mounted) {
        return;
      }

      setState(() {
        _modeli = modeli;
        _poslovnice = poslovnice;
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

  /// Nova poslovnica se otvara iz same forme, bez napustanja unosa vozila.
  /// Kad se sacuva, lista se osvjezava i nova poslovnica ostaje odabrana.
  Future<void> _novaPoslovnica() async {
    final id = await showDialog<int>(
      context: context,
      barrierDismissible: false,
      builder: (context) => const PoslovnicaForma(),
    );

    if (id == null) {
      return;
    }

    await _ucitajSifrarnike(osvjeziPoslovnice: true);

    if (!mounted) {
      return;
    }

    setState(() => _poslovnicaId = id);
  }

  Future<void> _sacuvaj() async {
    if (!_forma.currentState!.validate()) {
      return;
    }

    if (_modelId == null || _poslovnicaId == null) {
      setState(() => _greska = 'Odaberite model vozila i poslovnicu.');

      return;
    }

    setState(() {
      _snimanje = true;
      _greska = null;
    });

    final zahtjev = <String, dynamic>{
      'modelVozilaId': _modelId,
      'poslovnicaId': _poslovnicaId,
      'registarskaOznaka': _registracija.text.trim(),
      'godinaProizvodnje': int.tryParse(_godina.text.trim()) ?? 0,
      'kilometraza': int.tryParse(_kilometraza.text.trim()) ?? 0,
      'satnaTarifa': _broj(_satna.text),
      'dnevnaTarifa': _broj(_dnevna.text),
      'iznosDepozita': _broj(_depozit.text),
      if (_jeIzmjena) 'aktivno': _aktivno,
    };

    try {
      if (_jeIzmjena) {
        await _servis.izmijeni(widget.vozilo!.id, zahtjev);
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

  /// Korisnik moze ukucati i zarez. Server ocekuje tacku, pa se pretvara ovdje.
  static double _broj(String tekst) {
    return double.tryParse(tekst.trim().replaceAll(',', '.')) ?? 0;
  }

  @override
  Widget build(BuildContext context) {
    return DijalogForme(
      naslov: _jeIzmjena ? 'Izmjena vozila' : 'Novo vozilo',
      podnaslov: _jeIzmjena
          ? '${widget.vozilo!.puniNaziv} · ${widget.vozilo!.registarskaOznaka}'
          : 'Kategorija dozvole se preuzima iz odabranog modela',
      greska: _greska,
      uToku: _snimanje,
      naSnimanje: _sacuvaj,
      sirina: 720,
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
                    lijevo: DropdownButtonFormField<int>(
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
                          vrijednost == null ? 'Odaberite model vozila.' : null,
                    ),
                    desno: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(
                          child: DropdownButtonFormField<int>(
                            initialValue:
                                _poslovnice.any((x) => x.id == _poslovnicaId)
                                ? _poslovnicaId
                                : null,
                            isExpanded: true,
                            decoration: const InputDecoration(
                              labelText: 'Poslovnica',
                            ),
                            items: [
                              for (final poslovnica in _poslovnice)
                                DropdownMenuItem(
                                  value: poslovnica.id,
                                  child: Text(poslovnica.naziv),
                                ),
                            ],
                            onChanged: (id) =>
                                setState(() => _poslovnicaId = id),
                            validator: (vrijednost) => vrijednost == null
                                ? 'Odaberite poslovnicu.'
                                : null,
                          ),
                        ),
                        const SizedBox(width: Razmaci.s),
                        Padding(
                          padding: const EdgeInsets.only(top: 2),
                          child: IconButton.outlined(
                            tooltip: 'Nova poslovnica',
                            onPressed: _novaPoslovnica,
                            icon: const Icon(Icons.add, size: 18),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: Razmaci.l),
                  RedPolja(
                    lijevo: TextFormField(
                      controller: _registracija,
                      textCapitalization: TextCapitalization.characters,
                      decoration: const InputDecoration(
                        labelText: 'Registarska oznaka',
                        hintText: 'A12-B-345',
                      ),
                      validator: (vrijednost) {
                        final tekst = vrijednost?.trim() ?? '';

                        if (tekst.length < 5 || tekst.length > 20) {
                          return 'Oznaka mora imati između 5 i 20 znakova.';
                        }

                        return null;
                      },
                    ),
                    desno: TextFormField(
                      controller: _godina,
                      keyboardType: TextInputType.number,
                      inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                      decoration: const InputDecoration(
                        labelText: 'Godina proizvodnje',
                      ),
                      validator: (vrijednost) {
                        final godina = int.tryParse(vrijednost?.trim() ?? '');

                        if (godina == null ||
                            godina < 1990 ||
                            godina > DateTime.now().year) {
                          return 'Unesite godinu između 1990. i ${DateTime.now().year}.';
                        }

                        return null;
                      },
                    ),
                  ),
                  const SizedBox(height: Razmaci.l),
                  RedPolja(
                    lijevo: TextFormField(
                      controller: _kilometraza,
                      keyboardType: TextInputType.number,
                      inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                      decoration: const InputDecoration(
                        labelText: 'Kilometraža',
                        suffixText: 'km',
                      ),
                      validator: (vrijednost) {
                        final km = int.tryParse(vrijednost?.trim() ?? '');

                        if (km == null || km < 0 || km > 1000000) {
                          return 'Unesite kilometražu između 0 i 1.000.000.';
                        }

                        return null;
                      },
                    ),
                    desno: TextFormField(
                      controller: _depozit,
                      keyboardType: const TextInputType.numberWithOptions(
                        decimal: true,
                      ),
                      decoration: const InputDecoration(
                        labelText: 'Depozit',
                        suffixText: '€',
                      ),
                      validator: (vrijednost) {
                        final iznos = _broj(vrijednost ?? '');

                        if (iznos < 0 || iznos > 100000) {
                          return 'Depozit mora biti između 0 i 100.000 €.';
                        }

                        return null;
                      },
                    ),
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
                      ),
                      validator: _tarifa,
                    ),
                    desno: TextFormField(
                      controller: _dnevna,
                      keyboardType: const TextInputType.numberWithOptions(
                        decimal: true,
                      ),
                      decoration: const InputDecoration(
                        labelText: 'Dnevna tarifa',
                        suffixText: '€',
                      ),
                      validator: _tarifa,
                    ),
                  ),
                  if (_jeIzmjena) ...[
                    const SizedBox(height: Razmaci.l),
                    SwitchListTile(
                      value: _aktivno,
                      onChanged: (vrijednost) =>
                          setState(() => _aktivno = vrijednost),
                      contentPadding: EdgeInsets.zero,
                      title: const Text('Vozilo je aktivno'),
                      subtitle: const Text(
                        'Deaktivirano vozilo se ne nudi za nove rezervacije, '
                        'a postojeće ostaju netaknute.',
                        style: TextStyle(fontSize: 12),
                      ),
                    ),
                    const SizedBox(height: Razmaci.s),
                    const Divider(),
                    const SizedBox(height: Razmaci.l),
                    GalerijaVozila(
                      voziloId: widget.vozilo!.id,
                      servis: _servis,
                    ),
                  ],
                ],
              ),
            ),
    );
  }

  String? _tarifa(String? vrijednost) {
    final iznos = _broj(vrijednost ?? '');

    if (iznos <= 0 || iznos > 10000) {
      return 'Tarifa mora biti veća od nule.';
    }

    return null;
  }
}
