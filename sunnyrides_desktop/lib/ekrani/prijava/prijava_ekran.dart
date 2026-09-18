import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../stanje/sesija.dart';
import '../../widgeti/obavjestenje.dart';

class PrijavaEkran extends StatefulWidget {
  const PrijavaEkran({super.key});

  @override
  State<PrijavaEkran> createState() => _PrijavaEkranStanje();
}

class _PrijavaEkranStanje extends State<PrijavaEkran> {
  final _forma = GlobalKey<FormState>();
  final _korisnickoIme = TextEditingController();
  final _lozinka = TextEditingController();

  bool _uToku = false;
  bool _sakrivenaLozinka = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    // Poruka o istekloj sesiji ili o klijentskom nalogu, ako je do nje doslo.
    _greska = context.read<Sesija>().preuzmiPorukuOdjave();
  }

  @override
  void dispose() {
    _korisnickoIme.dispose();
    _lozinka.dispose();
    super.dispose();
  }

  Future<void> _prijavi() async {
    if (!_forma.currentState!.validate() || _uToku) {
      return;
    }

    setState(() {
      _uToku = true;
      _greska = null;
    });

    try {
      await context.read<Sesija>().prijava(
        _korisnickoIme.text.trim(),
        _lozinka.text,
      );
      // Ekran nestaje sam kad se promijeni stanje sesije, pa ovdje nema navigacije.
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() {
        _uToku = false;
        _greska = greska.poruka;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Boje.platno,
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(Razmaci.xxl),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const _Znak(),
                const SizedBox(height: Razmaci.xl),
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(Razmaci.xxl),
                    child: Form(
                      key: _forma,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          const Text(
                            'Prijava',
                            style: TextStyle(
                              fontSize: 20,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          const SizedBox(height: Razmaci.xs),
                          const Text(
                            'Pristup je dozvoljen osoblju agencije.',
                            style: TextStyle(
                              color: Boje.tekstPrigusen,
                              fontSize: 13,
                            ),
                          ),
                          const SizedBox(height: Razmaci.xl),
                          if (_greska != null) ...[
                            Obavjestenje.greska(_greska!),
                            const SizedBox(height: Razmaci.l),
                          ],
                          TextFormField(
                            controller: _korisnickoIme,
                            autofocus: true,
                            enabled: !_uToku,
                            textInputAction: TextInputAction.next,
                            decoration: const InputDecoration(
                              labelText: 'Korisničko ime',
                              prefixIcon: Icon(Icons.person_outline, size: 20),
                            ),
                            validator: (vrijednost) {
                              if (vrijednost == null ||
                                  vrijednost.trim().isEmpty) {
                                return 'Unesite korisničko ime.';
                              }

                              return null;
                            },
                          ),
                          const SizedBox(height: Razmaci.l),
                          TextFormField(
                            controller: _lozinka,
                            enabled: !_uToku,
                            obscureText: _sakrivenaLozinka,
                            onFieldSubmitted: (_) => _prijavi(),
                            decoration: InputDecoration(
                              labelText: 'Lozinka',
                              prefixIcon: const Icon(
                                Icons.lock_outline,
                                size: 20,
                              ),
                              suffixIcon: IconButton(
                                tooltip: _sakrivenaLozinka
                                    ? 'Prikaži lozinku'
                                    : 'Sakrij lozinku',
                                icon: Icon(
                                  _sakrivenaLozinka
                                      ? Icons.visibility_outlined
                                      : Icons.visibility_off_outlined,
                                  size: 20,
                                ),
                                onPressed: () {
                                  setState(
                                    () =>
                                        _sakrivenaLozinka = !_sakrivenaLozinka,
                                  );
                                },
                              ),
                            ),
                            validator: (vrijednost) {
                              if (vrijednost == null || vrijednost.isEmpty) {
                                return 'Unesite lozinku.';
                              }

                              return null;
                            },
                          ),
                          const SizedBox(height: Razmaci.xl),
                          SizedBox(
                            height: 44,
                            child: ElevatedButton(
                              onPressed: _uToku ? null : _prijavi,
                              child: _uToku
                                  ? const SizedBox(
                                      width: 18,
                                      height: 18,
                                      child: CircularProgressIndicator(
                                        strokeWidth: 2,
                                        color: Boje.naPrimarnoj,
                                      ),
                                    )
                                  : const Text('Prijavi se'),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _Znak extends StatelessWidget {
  const _Znak();

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Container(
          width: 56,
          height: 56,
          decoration: BoxDecoration(
            color: Boje.primarna,
            borderRadius: BorderRadius.circular(Zaobljenja.kartica),
          ),
          child: const Icon(
            Icons.two_wheeler,
            color: Boje.naPrimarnoj,
            size: 30,
          ),
        ),
        const SizedBox(height: Razmaci.m),
        const Text(
          'SunnyRides',
          style: TextStyle(
            fontSize: 22,
            fontWeight: FontWeight.w700,
            letterSpacing: -0.3,
          ),
        ),
        const SizedBox(height: Razmaci.xs),
        const Text(
          'Upravljanje flotom i najmovima',
          style: TextStyle(color: Boje.tekstPrigusen, fontSize: 13),
        ),
      ],
    );
  }
}
