import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../servisi/korisnik_servis.dart';
import '../../widgeti/dijalog_forme.dart';

/// Otvaranje naloga i izmjena postojeceg.
///
/// Lozinka se unosi samo pri otvaranju naloga. Izmjena je ne dira - za nju postoji
/// zaseban reset, koji ne trazi staru lozinku jer je administrator ni ne zna.
class KorisnikForma extends StatefulWidget {
  const KorisnikForma({super.key, this.korisnik, required this.uloge});

  final Korisnik? korisnik;
  final List<Uloga> uloge;

  @override
  State<KorisnikForma> createState() => _KorisnikFormaStanje();
}

class _KorisnikFormaStanje extends State<KorisnikForma> {
  final _forma = GlobalKey<FormState>();

  late final TextEditingController _korisnickoIme;
  late final TextEditingController _ime;
  late final TextEditingController _prezime;
  late final TextEditingController _email;
  late final TextEditingController _telefon;
  late final TextEditingController _lozinka;
  late final TextEditingController _potvrda;

  late final KorisnikServis _servis;

  late DateTime _datumRodjenja;
  late Set<int> _odabraneUloge;
  late bool _aktivan;

  bool _snimanje = false;
  String? _greska;

  bool get _jeIzmjena => widget.korisnik != null;

  @override
  void initState() {
    super.initState();

    _servis = KorisnikServis(context.read<ApiKlijent>());

    final korisnik = widget.korisnik;

    _korisnickoIme = TextEditingController(text: korisnik?.korisnickoIme ?? '');
    _ime = TextEditingController(text: korisnik?.ime ?? '');
    _prezime = TextEditingController(text: korisnik?.prezime ?? '');
    _email = TextEditingController(text: korisnik?.email ?? '');
    _telefon = TextEditingController(text: korisnik?.telefon ?? '');
    _lozinka = TextEditingController();
    _potvrda = TextEditingController();

    _datumRodjenja =
        korisnik?.datumRodjenja.toLocal() ?? DateTime(DateTime.now().year - 25);
    _aktivan = korisnik?.aktivan ?? true;

    _odabraneUloge = widget.uloge
        .where((u) => korisnik?.uloge.contains(u.naziv) ?? false)
        .map((u) => u.id)
        .toSet();
  }

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
    final datum = await showDatePicker(
      context: context,
      initialDate: _datumRodjenja,
      firstDate: DateTime(1930),
      lastDate: DateTime.now(),
      helpText: 'Datum rođenja',
    );

    if (datum != null) {
      setState(() => _datumRodjenja = datum);
    }
  }

  Future<void> _sacuvaj() async {
    if (!_forma.currentState!.validate()) {
      return;
    }

    if (_odabraneUloge.isEmpty) {
      setState(() => _greska = 'Odaberite bar jednu ulogu.');

      return;
    }

    setState(() {
      _snimanje = true;
      _greska = null;
    });

    try {
      if (_jeIzmjena) {
        await _servis.izmijeni(widget.korisnik!.id, {
          'ime': _ime.text.trim(),
          'prezime': _prezime.text.trim(),
          'email': _email.text.trim(),
          'telefon': _telefon.text.trim().isEmpty ? null : _telefon.text.trim(),
          'datumRodjenja': _datumRodjenja.toUtc().toIso8601String(),
          'aktivan': _aktivan,
        });

        // Uloge se postavljaju zasebnim pozivom, jer ih izmjena naloga ne nosi -
        // promjena uloge je druga radnja i server je tako i razdvaja.
        await _servis.postaviUloge(
          widget.korisnik!.id,
          _odabraneUloge.toList(),
        );
      } else {
        await _servis.dodaj({
          'korisnickoIme': _korisnickoIme.text.trim(),
          'ime': _ime.text.trim(),
          'prezime': _prezime.text.trim(),
          'email': _email.text.trim(),
          'telefon': _telefon.text.trim().isEmpty ? null : _telefon.text.trim(),
          'datumRodjenja': _datumRodjenja.toUtc().toIso8601String(),
          'lozinka': _lozinka.text,
          'potvrdaLozinke': _potvrda.text,
          'ulogeIds': _odabraneUloge.toList(),
        });
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
      naslov: _jeIzmjena ? 'Izmjena naloga' : 'Novi nalog',
      podnaslov: _jeIzmjena
          ? widget.korisnik!.korisnickoIme
          : 'Korisničko ime se kasnije ne mijenja',
      greska: _greska,
      uToku: _snimanje,
      naSnimanje: _sacuvaj,
      sirina: 700,
      dijete: Form(
        key: _forma,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (!_jeIzmjena) ...[
              TextFormField(
                controller: _korisnickoIme,
                decoration: const InputDecoration(labelText: 'Korisničko ime'),
                validator: (vrijednost) {
                  final tekst = vrijednost?.trim() ?? '';

                  if (tekst.length < 3 || tekst.length > 50) {
                    return 'Korisničko ime mora imati između 3 i 50 znakova.';
                  }

                  if (!RegExp(r'^[a-zA-Z0-9._-]+$').hasMatch(tekst)) {
                    return 'Dozvoljena su slova, brojevi, tačka, donja crta i crtica.';
                  }

                  return null;
                },
              ),
              const SizedBox(height: Razmaci.l),
            ],
            RedPolja(
              lijevo: TextFormField(
                controller: _ime,
                decoration: const InputDecoration(labelText: 'Ime'),
                validator: (vrijednost) => _duzina(vrijednost, 'Ime'),
              ),
              desno: TextFormField(
                controller: _prezime,
                decoration: const InputDecoration(labelText: 'Prezime'),
                validator: (vrijednost) => _duzina(vrijednost, 'Prezime'),
              ),
            ),
            const SizedBox(height: Razmaci.l),
            RedPolja(
              lijevo: TextFormField(
                controller: _email,
                decoration: const InputDecoration(labelText: 'Email'),
                validator: (vrijednost) {
                  final tekst = vrijednost?.trim() ?? '';

                  if (!RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(tekst)) {
                    return 'Unesite ispravnu email adresu.';
                  }

                  return null;
                },
              ),
              desno: TextFormField(
                controller: _telefon,
                decoration: const InputDecoration(
                  labelText: 'Telefon',
                  hintText: '+387 61 234 567',
                ),
                validator: (vrijednost) {
                  final tekst = vrijednost?.trim() ?? '';

                  if (tekst.isEmpty) {
                    return null;
                  }

                  if (!RegExp(r'^\+387 6\d{1} \d{3} \d{3}$').hasMatch(tekst)) {
                    return 'Oblik: +387 61 234 567';
                  }

                  return null;
                },
              ),
            ),
            const SizedBox(height: Razmaci.l),
            RedPolja(
              lijevo: InkWell(
                onTap: _odaberiDatum,
                borderRadius: BorderRadius.circular(Zaobljenja.polje),
                child: InputDecorator(
                  decoration: const InputDecoration(
                    labelText: 'Datum rođenja',
                    suffixIcon: Icon(Icons.event_outlined, size: 18),
                  ),
                  child: Text(Formati.datum(_datumRodjenja)),
                ),
              ),
              desno: _jeIzmjena
                  ? SwitchListTile(
                      value: _aktivan,
                      onChanged: (vrijednost) =>
                          setState(() => _aktivan = vrijednost),
                      contentPadding: EdgeInsets.zero,
                      title: const Text('Nalog je aktivan'),
                      subtitle: const Text(
                        'Neaktivan se ne može prijaviti.',
                        style: TextStyle(fontSize: 11.5),
                      ),
                    )
                  : const SizedBox.shrink(),
            ),
            if (!_jeIzmjena) ...[
              const SizedBox(height: Razmaci.l),
              RedPolja(
                lijevo: TextFormField(
                  controller: _lozinka,
                  obscureText: true,
                  decoration: const InputDecoration(labelText: 'Lozinka'),
                  validator: (vrijednost) {
                    if ((vrijednost?.length ?? 0) < 6) {
                      return 'Lozinka mora imati bar šest znakova.';
                    }

                    return null;
                  },
                ),
                desno: TextFormField(
                  controller: _potvrda,
                  obscureText: true,
                  decoration: const InputDecoration(
                    labelText: 'Potvrda lozinke',
                  ),
                  validator: (vrijednost) {
                    if (vrijednost != _lozinka.text) {
                      return 'Lozinke se ne poklapaju.';
                    }

                    return null;
                  },
                ),
              ),
            ],
            const SizedBox(height: Razmaci.xl),
            const Text(
              'Uloge',
              style: TextStyle(fontSize: 13.5, fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: Razmaci.s),
            Wrap(
              spacing: Razmaci.s,
              runSpacing: Razmaci.s,
              children: [
                for (final uloga in widget.uloge)
                  FilterChip(
                    label: Text(uloga.naziv),
                    selected: _odabraneUloge.contains(uloga.id),
                    onSelected: (odabrano) => setState(() {
                      if (odabrano) {
                        _odabraneUloge.add(uloga.id);
                      } else {
                        _odabraneUloge.remove(uloga.id);
                      }

                      _greska = null;
                    }),
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  String? _duzina(String? vrijednost, String polje) {
    final tekst = vrijednost?.trim() ?? '';

    if (tekst.length < 2 || tekst.length > 50) {
      return '$polje mora imati između 2 i 50 znakova.';
    }

    return null;
  }
}

/// Postavljanje nove lozinke bez trazenja stare.
class ResetLozinkeDijalog extends StatefulWidget {
  const ResetLozinkeDijalog({super.key, required this.korisnik});

  final Korisnik korisnik;

  @override
  State<ResetLozinkeDijalog> createState() => _ResetLozinkeDijalogStanje();
}

class _ResetLozinkeDijalogStanje extends State<ResetLozinkeDijalog> {
  final _forma = GlobalKey<FormState>();
  final _nova = TextEditingController();
  final _potvrda = TextEditingController();

  late final KorisnikServis _servis;

  bool _snimanje = false;
  String? _greska;

  @override
  void initState() {
    super.initState();
    _servis = KorisnikServis(context.read<ApiKlijent>());
  }

  @override
  void dispose() {
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
      await _servis.resetujLozinku(
        widget.korisnik.id,
        _nova.text,
        _potvrda.text,
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
      naslov: 'Nova lozinka',
      podnaslov:
          '${widget.korisnik.punoIme} · ${widget.korisnik.korisnickoIme}',
      greska: _greska,
      uToku: _snimanje,
      natpisPotvrde: 'Postavi lozinku',
      naSnimanje: _sacuvaj,
      sirina: 520,
      dijete: Form(
        key: _forma,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'Stara lozinka se ne traži — administrator je ne zna. Novu lozinku '
              'javite korisniku, jer je sistem ne šalje sam.',
              style: TextStyle(
                color: Boje.tekstPrigusen,
                fontSize: 12.5,
                height: 1.4,
              ),
            ),
            const SizedBox(height: Razmaci.l),
            TextFormField(
              controller: _nova,
              obscureText: true,
              autofocus: true,
              decoration: const InputDecoration(labelText: 'Nova lozinka'),
              validator: (vrijednost) {
                if ((vrijednost?.length ?? 0) < 6) {
                  return 'Lozinka mora imati bar šest znakova.';
                }

                return null;
              },
            ),
            const SizedBox(height: Razmaci.l),
            TextFormField(
              controller: _potvrda,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'Potvrda lozinke'),
              validator: (vrijednost) {
                if (vrijednost != _nova.text) {
                  return 'Lozinke se ne poklapaju.';
                }

                return null;
              },
            ),
          ],
        ),
      ),
    );
  }
}
