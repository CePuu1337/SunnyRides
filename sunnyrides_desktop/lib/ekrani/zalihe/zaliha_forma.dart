import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/stavka_sifrarnika.dart';
import '../../modeli/zaliha_opreme.dart';
import '../../servisi/zalihe_opreme_servis.dart';
import '../../widgeti/dijalog_forme.dart';

/// Unos ili izmjena zaliha jedne vrste opreme u jednoj poslovnici.
///
/// Pri izmjeni se mijenja samo količina. Oprema i poslovnica određuju o kojem se
/// zapisu radi, pa su prikazane, ali se ne biraju ponovo.
class ZalihaForma extends StatefulWidget {
  const ZalihaForma({
    super.key,
    this.zaliha,
    required this.vrsteOpreme,
    required this.poslovnice,
  });

  final ZalihaOpreme? zaliha;
  final List<StavkaSifrarnika> vrsteOpreme;

  /// Poslovnice koje korisnik smije birati - uposleniku samo njegova.
  final List<StavkaSifrarnika> poslovnice;

  @override
  State<ZalihaForma> createState() => _ZalihaFormaStanje();
}

class _ZalihaFormaStanje extends State<ZalihaForma> {
  final _forma = GlobalKey<FormState>();

  late final ZaliheOpremeServis _servis;
  late final TextEditingController _kolicina;

  int? _vrstaId;
  int? _poslovnicaId;

  bool _snimanje = false;
  String? _greska;

  bool get _jeIzmjena => widget.zaliha != null;

  @override
  void initState() {
    super.initState();

    _servis = ZaliheOpremeServis(context.read<ApiKlijent>());
    _kolicina = TextEditingController(
      text: (widget.zaliha?.kolicina ?? 0).toString(),
    );

    // Kad je ponuđena samo jedna poslovnica (uposlenik), nema šta birati.
    _poslovnicaId =
        widget.zaliha?.poslovnicaId ??
        (widget.poslovnice.length == 1 ? widget.poslovnice.first.id : null);
    _vrstaId = widget.zaliha?.vrstaOpremeId;
  }

  @override
  void dispose() {
    _kolicina.dispose();
    super.dispose();
  }

  Future<void> _sacuvaj() async {
    if (!_forma.currentState!.validate()) {
      return;
    }

    setState(() {
      _snimanje = true;
      _greska = null;
    });

    final kolicina = int.parse(_kolicina.text.trim());

    try {
      if (_jeIzmjena) {
        await _servis.izmijeni(widget.zaliha!.id, kolicina);
      } else {
        await _servis.dodaj(
          vrstaOpremeId: _vrstaId!,
          poslovnicaId: _poslovnicaId!,
          kolicina: kolicina,
        );
      }

      if (!mounted) {
        return;
      }

      Navigator.of(context).pop(true);
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      // Npr. kad je količina manja od one koju već drže rezervacije - server
      // kaže koliko je obećano i od kada.
      setState(() {
        _snimanje = false;
        _greska = greska.poruka;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final zaliha = widget.zaliha;

    return DijalogForme(
      naslov: _jeIzmjena ? 'Izmjena zaliha' : 'Nove zalihe opreme',
      podnaslov: _jeIzmjena
          ? '${zaliha!.vrstaOpremeNaziv} · ${zaliha.poslovnicaNaziv}'
          : 'Koliko komada opreme poslovnica ima',
      greska: _greska,
      uToku: _snimanje,
      sirina: 560,
      naSnimanje: _sacuvaj,
      dijete: Form(
        key: _forma,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (!_jeIzmjena) ...[
              RedPolja(
                lijevo: DropdownButtonFormField<int>(
                  initialValue: _vrstaId,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Oprema'),
                  items: [
                    for (final vrsta in widget.vrsteOpreme)
                      DropdownMenuItem(
                        value: vrsta.id,
                        child: Text(vrsta.naziv),
                      ),
                  ],
                  onChanged: (id) => setState(() => _vrstaId = id),
                  validator: (vrijednost) =>
                      vrijednost == null ? 'Odaberite opremu.' : null,
                ),
                desno: DropdownButtonFormField<int>(
                  initialValue: _poslovnicaId,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Poslovnica'),
                  items: [
                    for (final poslovnica in widget.poslovnice)
                      DropdownMenuItem(
                        value: poslovnica.id,
                        child: Text(poslovnica.naziv),
                      ),
                  ],
                  onChanged: widget.poslovnice.length == 1
                      ? null
                      : (id) => setState(() => _poslovnicaId = id),
                  validator: (vrijednost) =>
                      vrijednost == null ? 'Odaberite poslovnicu.' : null,
                ),
              ),
              const SizedBox(height: Razmaci.l),
            ],
            RedPolja(
              lijevo: TextFormField(
                controller: _kolicina,
                keyboardType: TextInputType.number,
                inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                decoration: const InputDecoration(
                  labelText: 'Količina',
                  suffixText: 'kom.',
                  helperText:
                      'Ne može biti manja od opreme koju drže rezervacije.',
                  helperMaxLines: 2,
                ),
                validator: (vrijednost) {
                  final broj = int.tryParse(vrijednost?.trim() ?? '');

                  if (broj == null || broj < 0 || broj > 1000) {
                    return 'Unesite cijeli broj od 0 do 1000.';
                  }

                  return null;
                },
              ),
              desno: const SizedBox.shrink(),
            ),
          ],
        ),
      ),
    );
  }
}
