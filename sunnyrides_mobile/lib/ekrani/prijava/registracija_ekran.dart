import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../stanje/sesija.dart';
import '../../widgeti/obavjestenje.dart';

/// Otvaranje klijentskog naloga.
///
/// Uloga se ne salje - server novom nalogu dodjeljuje klijentsku ulogu sam. Da
/// aplikacija moze reci koja je uloga, svako bi se mogao registrovati kao administrator.
class RegistracijaEkran extends StatefulWidget {
  const RegistracijaEkran({super.key});

  @override
  State<RegistracijaEkran> createState() => _RegistracijaEkranStanje();
}

class _RegistracijaEkranStanje extends State<RegistracijaEkran> {
  final _forma = GlobalKey<FormState>();

  final _korisnickoIme = TextEditingController();
  final _ime = TextEditingController();
  final _prezime = TextEditingController();
  final _email = TextEditingController();
  final _telefon = TextEditingController(text: '+387 6');
  final _lozinka = TextEditingController();
  final _potvrda = TextEditingController();

  DateTime? _datumRodjenja;
  bool _uToku = false;
  String? _greska;

  @override
  void dispose() {
    _korisnickoIme.dispose();
    _ime.dispose();
    _prezime.dispose();
    _email.dispose();
    _telefon.dispose();
    _lozinka.dispose();
    _potvrda.dispose();
    super.dispose();
  }

  Future<void> _odaberiDatum() async {
    final danas = DateTime.now();

    final datum = await showDatePicker(
      context: context,
      initialDate:
          _datumRodjenja ?? DateTime(danas.year - 25, danas.month, danas.day),
      firstDate: DateTime(1930),
      lastDate: danas,
      helpText: 'Datum rođenja',
    );

    if (datum != null) {
      setState(() => _datumRodjenja = datum);
    }
  }

  Future<void> _registruj() async {
    if (!_forma.currentState!.validate() || _uToku) {
      return;
    }

    if (_datumRodjenja == null) {
      setState(() => _greska = 'Odaberite datum rođenja.');

      return;
    }

    setState(() {
      _uToku = true;
      _greska = null;
    });

    try {
      await context.read<Sesija>().registracija(
        korisnickoIme: _korisnickoIme.text.trim(),
        ime: _ime.text.trim(),
        prezime: _prezime.text.trim(),
        email: _email.text.trim(),
        telefon: _telefon.text.trim(),
        datumRodjenja: _datumRodjenja!,
        lozinka: _lozinka.text,
        potvrdaLozinke: _potvrda.text,
      );

      // Uspjesna registracija odmah prijavljuje, pa ekran prijave nestaje sam.
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
      appBar: AppBar(title: const Text('Novi nalog')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(Razmaci.xl),
          child: Form(
            key: _forma,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                if (_greska != null) ...[
                  Obavjestenje.greska(_greska!),
                  const SizedBox(height: Razmaci.l),
                ],
                TextFormField(
                  controller: _korisnickoIme,
                  decoration: const InputDecoration(
                    labelText: 'Korisničko ime',
                  ),
                  validator: (vrijednost) {
                    final tekst = vrijednost?.trim() ?? '';

                    if (tekst.length < 3 || tekst.length > 50) {
                      return 'Između 3 i 50 znakova.';
                    }

                    if (!RegExp(r'^[a-zA-Z0-9._-]+$').hasMatch(tekst)) {
                      return 'Slova, brojevi, tačka, donja crta i crtica.';
                    }

                    return null;
                  },
                ),
                const SizedBox(height: Razmaci.l),
                TextFormField(
                  controller: _ime,
                  decoration: const InputDecoration(labelText: 'Ime'),
                  validator: (vrijednost) => _duzina(vrijednost),
                ),
                const SizedBox(height: Razmaci.l),
                TextFormField(
                  controller: _prezime,
                  decoration: const InputDecoration(labelText: 'Prezime'),
                  validator: (vrijednost) => _duzina(vrijednost),
                ),
                const SizedBox(height: Razmaci.l),
                TextFormField(
                  controller: _email,
                  keyboardType: TextInputType.emailAddress,
                  decoration: const InputDecoration(labelText: 'Email'),
                  validator: (vrijednost) {
                    final tekst = vrijednost?.trim() ?? '';

                    if (!RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$')
                        .hasMatch(tekst)) {
                      return 'Unesite ispravnu email adresu.';
                    }

                    return null;
                  },
                ),
                const SizedBox(height: Razmaci.l),
                TextFormField(
                  controller: _telefon,
                  keyboardType: TextInputType.phone,
                  decoration: const InputDecoration(
                    labelText: 'Telefon',
                    hintText: '+387 61 234 567',
                  ),
                  validator: (vrijednost) {
                    final tekst = vrijednost?.trim() ?? '';

                    if (!RegExp(r'^\+387 6\d{1} \d{3} \d{3}$')
                        .hasMatch(tekst)) {
                      return 'Oblik: +387 61 234 567';
                    }

                    return null;
                  },
                ),
                const SizedBox(height: Razmaci.l),
                InkWell(
                  onTap: _odaberiDatum,
                  borderRadius: BorderRadius.circular(Zaobljenja.polje),
                  child: InputDecorator(
                    decoration: const InputDecoration(
                      labelText: 'Datum rođenja',
                      suffixIcon: Icon(Icons.event_outlined),
                    ),
                    child: Text(
                      _datumRodjenja == null
                          ? 'Odaberite datum'
                          : Formati.datum(_datumRodjenja!),
                      style: TextStyle(
                        color: _datumRodjenja == null
                            ? Boje.tekstPrigusen
                            : Boje.tekst,
                      ),
                    ),
                  ),
                ),
                const SizedBox(height: Razmaci.l),
                TextFormField(
                  controller: _lozinka,
                  obscureText: true,
                  decoration: const InputDecoration(labelText: 'Lozinka'),
                  validator: (vrijednost) => (vrijednost?.length ?? 0) < 6
                      ? 'Lozinka mora imati bar šest znakova.'
                      : null,
                ),
                const SizedBox(height: Razmaci.l),
                TextFormField(
                  controller: _potvrda,
                  obscureText: true,
                  decoration: const InputDecoration(
                    labelText: 'Potvrda lozinke',
                  ),
                  validator: (vrijednost) => vrijednost != _lozinka.text
                      ? 'Lozinke se ne poklapaju.'
                      : null,
                ),
                const SizedBox(height: Razmaci.xl),
                Obavjestenje.info(
                  'Za rezervaciju vozila trebaće vam i verifikovana vozačka dozvola. '
                  'Nju dodajete u profilu nakon registracije.',
                ),
                const SizedBox(height: Razmaci.xl),
                SizedBox(
                  height: 50,
                  child: ElevatedButton(
                    onPressed: _uToku ? null : _registruj,
                    child: _uToku
                        ? const SizedBox(
                            width: 20,
                            height: 20,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Boje.naPrimarnoj,
                            ),
                          )
                        : const Text('Otvori nalog'),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  String? _duzina(String? vrijednost) {
    final tekst = vrijednost?.trim() ?? '';

    return tekst.length < 2 || tekst.length > 50
        ? 'Između 2 i 50 znakova.'
        : null;
  }
}
