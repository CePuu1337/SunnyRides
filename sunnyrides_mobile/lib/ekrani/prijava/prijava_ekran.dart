import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../stanje/sesija.dart';
import '../../widgeti/obavjestenje.dart';
import 'registracija_ekran.dart';

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
  bool _sakrivena = true;
  String? _greska;

  @override
  void initState() {
    super.initState();
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
      backgroundColor: Boje.povrsina,
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(Razmaci.xl),
          child: Form(
            key: _forma,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SizedBox(height: Razmaci.xxl),
                Center(
                  child: Container(
                    width: 64,
                    height: 64,
                    decoration: BoxDecoration(
                      color: Boje.primarna,
                      borderRadius: BorderRadius.circular(18),
                    ),
                    child: const Icon(
                      Icons.two_wheeler,
                      color: Boje.naPrimarnoj,
                      size: 34,
                    ),
                  ),
                ),
                const SizedBox(height: Razmaci.l),
                const Text(
                  'SunnyRides',
                  textAlign: TextAlign.center,
                  style: TextStyle(fontSize: 26, fontWeight: FontWeight.w700),
                ),
                const SizedBox(height: Razmaci.xs),
                const Text(
                  'Iznajmi skuter, motocikl ili kvad',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: Boje.tekstPrigusen, fontSize: 14),
                ),
                const SizedBox(height: Razmaci.xxl),
                if (_greska != null) ...[
                  Obavjestenje.greska(_greska!),
                  const SizedBox(height: Razmaci.l),
                ],
                TextFormField(
                  controller: _korisnickoIme,
                  enabled: !_uToku,
                  textInputAction: TextInputAction.next,
                  decoration: const InputDecoration(
                    labelText: 'Korisničko ime',
                    prefixIcon: Icon(Icons.person_outline),
                  ),
                  validator: (vrijednost) =>
                      (vrijednost?.trim().isEmpty ?? true)
                      ? 'Unesite korisničko ime.'
                      : null,
                ),
                const SizedBox(height: Razmaci.l),
                TextFormField(
                  controller: _lozinka,
                  enabled: !_uToku,
                  obscureText: _sakrivena,
                  onFieldSubmitted: (_) => _prijavi(),
                  decoration: InputDecoration(
                    labelText: 'Lozinka',
                    prefixIcon: const Icon(Icons.lock_outline),
                    suffixIcon: IconButton(
                      icon: Icon(
                        _sakrivena
                            ? Icons.visibility_outlined
                            : Icons.visibility_off_outlined,
                      ),
                      onPressed: () => setState(() => _sakrivena = !_sakrivena),
                    ),
                  ),
                  validator: (vrijednost) =>
                      (vrijednost?.isEmpty ?? true) ? 'Unesite lozinku.' : null,
                ),
                const SizedBox(height: Razmaci.xl),
                SizedBox(
                  height: 50,
                  child: ElevatedButton(
                    onPressed: _uToku ? null : _prijavi,
                    child: _uToku
                        ? const SizedBox(
                            width: 20,
                            height: 20,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Boje.naPrimarnoj,
                            ),
                          )
                        : const Text('Prijavi se'),
                  ),
                ),
                const SizedBox(height: Razmaci.l),
                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Text(
                      'Nemate nalog?',
                      style: TextStyle(
                        color: Boje.tekstPrigusen,
                        fontSize: 13.5,
                      ),
                    ),
                    TextButton(
                      onPressed: _uToku
                          ? null
                          : () => Navigator.of(context).push(
                              MaterialPageRoute<void>(
                                builder: (context) => const RegistracijaEkran(),
                              ),
                            ),
                      child: const Text('Registrujte se'),
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
