import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../widgeti/obavjestenje.dart';

/// Promjena lozinke. Trazi staru - bez nje bi tudji telefon u rukama bio dovoljan.
class PromjenaLozinkeEkran extends StatefulWidget {
  const PromjenaLozinkeEkran({super.key});

  @override
  State<PromjenaLozinkeEkran> createState() => _PromjenaLozinkeEkranStanje();
}

class _PromjenaLozinkeEkranStanje extends State<PromjenaLozinkeEkran> {
  final _forma = GlobalKey<FormState>();
  final _stara = TextEditingController();
  final _nova = TextEditingController();
  final _potvrda = TextEditingController();

  bool _slanje = false;
  String? _greska;

  @override
  void dispose() {
    _stara.dispose();
    _nova.dispose();
    _potvrda.dispose();
    super.dispose();
  }

  Future<void> _sacuvaj() async {
    if (!(_forma.currentState?.validate() ?? false)) {
      return;
    }

    setState(() {
      _slanje = true;
      _greska = null;
    });

    try {
      await context.read<AuthServis>().promjenaLozinke(
        stara: _stara.text,
        nova: _nova.text,
        potvrda: _potvrda.text,
      );

      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Lozinka je promijenjena.')));

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
      appBar: AppBar(title: const Text('Promjena lozinke')),
      body: Form(
        key: _forma,
        child: ListView(
          padding: const EdgeInsets.all(Razmaci.l),
          children: [
            if (_greska != null) ...[
              Obavjestenje.greska(_greska!),
              const SizedBox(height: Razmaci.l),
            ],
            TextFormField(
              controller: _stara,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'Trenutna lozinka'),
              validator: (vrijednost) => (vrijednost ?? '').isEmpty
                  ? 'Unesite trenutnu lozinku.'
                  : null,
            ),
            const SizedBox(height: Razmaci.m),
            TextFormField(
              controller: _nova,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'Nova lozinka'),
              validator: (vrijednost) => (vrijednost ?? '').length < 8
                  ? 'Lozinka mora imati najmanje 8 znakova.'
                  : null,
            ),
            const SizedBox(height: Razmaci.m),
            TextFormField(
              controller: _potvrda,
              obscureText: true,
              decoration: const InputDecoration(
                labelText: 'Potvrda nove lozinke',
              ),
              validator: (vrijednost) =>
                  vrijednost == _nova.text ? null : 'Lozinke se ne poklapaju.',
            ),
            const SizedBox(height: Razmaci.l),
            Obavjestenje.info(
              'Nakon promjene lozinke ostajete prijavljeni na ovom uređaju.',
            ),
            const SizedBox(height: Razmaci.xl),
            FilledButton(
              onPressed: _slanje ? null : _sacuvaj,
              child: Text(_slanje ? 'Čuvanje...' : 'Promijeni lozinku'),
            ),
          ],
        ),
      ),
    );
  }
}
