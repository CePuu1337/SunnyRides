import 'package:flutter/material.dart';
import 'package:latlong2/latlong.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/stavka_sifrarnika.dart';
import '../../servisi/sifrarnik_servis.dart';
import '../../widgeti/dijalog_forme.dart';
import 'birac_lokacije.dart';

/// Unos poslovnice. Vraca identifikator nove poslovnice kad se sacuva.
///
/// Otvara se i samostalno, iz sifrarnika, i iz forme za vozilo - zato vraca
/// identifikator umjesto obicnog "sacuvano", da ga forma za vozilo odmah odabere.
class PoslovnicaForma extends StatefulWidget {
  const PoslovnicaForma({super.key});

  @override
  State<PoslovnicaForma> createState() => _PoslovnicaFormaStanje();
}

class _PoslovnicaFormaStanje extends State<PoslovnicaForma> {
  final _forma = GlobalKey<FormState>();
  final _naziv = TextEditingController();
  final _adresa = TextEditingController();
  final _radnoVrijeme = TextEditingController();

  late final ApiKlijent _klijent;
  late final SifrarnikServis _sifrarnici;

  List<StavkaSifrarnika> _gradovi = const [];
  int? _gradId;
  LatLng? _lokacija;

  bool _ucitavanje = true;
  bool _snimanje = false;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _klijent = context.read<ApiKlijent>();
    _sifrarnici = SifrarnikServis(_klijent);

    _ucitajGradove();
  }

  @override
  void dispose() {
    _naziv.dispose();
    _adresa.dispose();
    _radnoVrijeme.dispose();
    super.dispose();
  }

  Future<void> _ucitajGradove() async {
    try {
      final gradovi = await _sifrarnici.ucitaj(SifrarnikServis.gradovi);

      if (!mounted) {
        return;
      }

      setState(() {
        _gradovi = gradovi;
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

  Future<void> _sacuvaj() async {
    if (!_forma.currentState!.validate()) {
      return;
    }

    if (_gradId == null) {
      setState(() => _greska = 'Odaberite grad.');

      return;
    }

    setState(() {
      _snimanje = true;
      _greska = null;
    });

    try {
      final odgovor = await _klijent.post(
        '/api/poslovnice',
        tijelo: {
          'gradId': _gradId,
          'naziv': _naziv.text.trim(),
          'adresa': _adresa.text.trim(),
          'latituda': _lokacija?.latitude,
          'longituda': _lokacija?.longitude,
          'radnoVrijeme': _radnoVrijeme.text.trim().isEmpty
              ? null
              : _radnoVrijeme.text.trim(),
        },
      );

      if (!mounted) {
        return;
      }

      final id = citajInt((odgovor as Map<String, dynamic>)['id']);

      Navigator.of(context).pop(id);
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
      naslov: 'Nova poslovnica',
      podnaslov: 'Grad se bira iz šifarnika, a lokacija klikom na kartu',
      greska: _greska,
      uToku: _snimanje,
      naSnimanje: _sacuvaj,
      sirina: 620,
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
                        labelText: 'Naziv poslovnice',
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
                      initialValue: _gradId,
                      isExpanded: true,
                      decoration: const InputDecoration(labelText: 'Grad'),
                      items: [
                        for (final grad in _gradovi)
                          DropdownMenuItem(
                            value: grad.id,
                            child: Text(grad.naziv),
                          ),
                      ],
                      onChanged: (id) => setState(() => _gradId = id),
                      validator: (vrijednost) =>
                          vrijednost == null ? 'Odaberite grad.' : null,
                    ),
                  ),
                  const SizedBox(height: Razmaci.l),
                  TextFormField(
                    controller: _adresa,
                    decoration: const InputDecoration(labelText: 'Adresa'),
                    validator: (vrijednost) {
                      final tekst = vrijednost?.trim() ?? '';

                      if (tekst.length < 3 || tekst.length > 200) {
                        return 'Adresa mora imati između 3 i 200 znakova.';
                      }

                      return null;
                    },
                  ),
                  const SizedBox(height: Razmaci.l),
                  TextFormField(
                    controller: _radnoVrijeme,
                    decoration: const InputDecoration(
                      labelText: 'Radno vrijeme',
                      hintText: 'Pon–Pet 08:00–18:00, Sub 09:00–14:00',
                    ),
                  ),
                  const SizedBox(height: Razmaci.xl),
                  const Text(
                    'Lokacija na karti',
                    style: TextStyle(
                      fontSize: 13.5,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: Razmaci.s),
                  BiracLokacije(
                    pocetna: _lokacija,
                    naOdabir: (tacka) => _lokacija = tacka,
                  ),
                ],
              ),
            ),
    );
  }
}
