import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../stanje/sesija.dart';
import '../../widgeti/dijalog_forme.dart';

/// Promjena vlastite lozinke za osoblje.
///
/// Trazi staru lozinku, za razliku od administratorskog reseta u upravljanju
/// korisnicima: racunar ostavljen otkljucan ne smije biti dovoljan da neko preuzme
/// nalog.
class PromjenaLozinkeDijalog extends StatefulWidget {
  const PromjenaLozinkeDijalog({super.key});

  @override
  State<PromjenaLozinkeDijalog> createState() =>
      _PromjenaLozinkeDijalogStanje();
}

class _PromjenaLozinkeDijalogStanje extends State<PromjenaLozinkeDijalog> {
  final _forma = GlobalKey<FormState>();
  final _stara = TextEditingController();
  final _nova = TextEditingController();
  final _potvrda = TextEditingController();

  bool _snimanje = false;
  String? _greska;

  @override
  void dispose() {
    _stara.dispose();
    _nova.dispose();
    _potvrda.dispose();
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

    try {
      await context.read<Sesija>().promijeniLozinku(
        stara: _stara.text,
        nova: _nova.text,
        potvrda: _potvrda.text,
      );

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
      naslov: 'Promjena lozinke',
      podnaslov: 'Za vaš nalog',
      greska: _greska,
      uToku: _snimanje,
      natpisPotvrde: 'Promijeni lozinku',
      naSnimanje: _sacuvaj,
      sirina: 520,
      dijete: Form(
        key: _forma,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'Na ovom računaru ostajete prijavljeni. Svi ostali uređaji na kojima '
              'ste prijavljeni tražiće ponovnu prijavu novom lozinkom.',
              style: TextStyle(
                color: Boje.tekstPrigusen,
                fontSize: 12.5,
                height: 1.4,
              ),
            ),
            const SizedBox(height: Razmaci.l),
            TextFormField(
              controller: _stara,
              obscureText: true,
              autofocus: true,
              decoration: const InputDecoration(labelText: 'Trenutna lozinka'),
              validator: (vrijednost) => (vrijednost ?? '').isEmpty
                  ? 'Unesite trenutnu lozinku.'
                  : null,
            ),
            const SizedBox(height: Razmaci.l),
            TextFormField(
              controller: _nova,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'Nova lozinka'),
              validator: Validatori.lozinka,
            ),
            const SizedBox(height: Razmaci.l),
            TextFormField(
              controller: _potvrda,
              obscureText: true,
              onFieldSubmitted: (_) {
                if (!_snimanje) {
                  _sacuvaj();
                }
              },
              decoration: const InputDecoration(
                labelText: 'Potvrda nove lozinke',
              ),
              validator: (vrijednost) =>
                  Validatori.potvrdaLozinke(vrijednost, _nova.text),
            ),
          ],
        ),
      ),
    );
  }
}
