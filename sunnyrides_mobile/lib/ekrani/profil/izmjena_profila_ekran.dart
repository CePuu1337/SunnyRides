import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/vozilo.dart';
import '../../servisi/katalog_servis.dart';
import '../../servisi/profil_servis.dart';
import '../../stanje/sesija.dart';
import '../../widgeti/obavjestenje.dart';

/// Izmjena vlastitih podataka. Lozinka nije dio forme - ona ide zasebno, uz staru.
class IzmjenaProfilaEkran extends StatefulWidget {
  const IzmjenaProfilaEkran({super.key, required this.korisnik});

  final Korisnik korisnik;

  @override
  State<IzmjenaProfilaEkran> createState() => _IzmjenaProfilaEkranStanje();
}

class _IzmjenaProfilaEkranStanje extends State<IzmjenaProfilaEkran> {
  final _forma = GlobalKey<FormState>();

  late final ProfilServis _servis;
  late final TextEditingController _ime;
  late final TextEditingController _prezime;
  late final TextEditingController _email;
  late final TextEditingController _telefon;

  late DateTime _datumRodjenja;

  List<Stavka> _gradovi = const [];
  int? _gradId;

  bool _slanje = false;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = ProfilServis(context.read<ApiKlijent>());

    _ime = TextEditingController(text: widget.korisnik.ime);
    _prezime = TextEditingController(text: widget.korisnik.prezime);
    _email = TextEditingController(text: widget.korisnik.email);
    _telefon = TextEditingController(text: widget.korisnik.telefon ?? '');
    _datumRodjenja = widget.korisnik.datumRodjenja.toLocal();
    _gradId = widget.korisnik.gradId;

    _ucitajGradove();
  }

  Future<void> _ucitajGradove() async {
    try {
      final gradovi = await KatalogServis(context.read<ApiKlijent>()).gradovi();

      if (mounted) {
        setState(() => _gradovi = gradovi);
      }
    } on ApiGreska catch (greska) {
      if (mounted) {
        setState(() => _greska = greska.poruka);
      }
    }
  }

  @override
  void dispose() {
    _ime.dispose();
    _prezime.dispose();
    _email.dispose();
    _telefon.dispose();
    super.dispose();
  }

  Future<void> _odaberiDatum() async {
    final danas = DateTime.now();

    final datum = await showDatePicker(
      context: context,
      initialDate: _datumRodjenja,
      firstDate: DateTime(danas.year - 100),
      lastDate: danas,
      helpText: 'Datum rođenja',
    );

    if (datum == null) {
      return;
    }

    setState(() => _datumRodjenja = datum);
  }

  Future<void> _sacuvaj() async {
    if (!(_forma.currentState?.validate() ?? false)) {
      return;
    }

    final gradId = _gradId;

    if (gradId == null) {
      return;
    }

    setState(() {
      _slanje = true;
      _greska = null;
    });

    try {
      final korisnik = await _servis.azuriraj(
        ime: _ime.text.trim(),
        prezime: _prezime.text.trim(),
        email: _email.text.trim(),
        telefon: _telefon.text.trim(),
        datumRodjenja: _datumRodjenja,
        gradId: gradId,
      );

      if (!mounted) {
        return;
      }

      context.read<Sesija>().osvjeziKorisnika(korisnik);

      ScaffoldMessenger.of(context)
          .showSnackBar(const SnackBar(content: Text('Podaci su sačuvani.')));

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
      appBar: AppBar(title: const Text('Moji podaci')),
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
              controller: _ime,
              decoration: const InputDecoration(labelText: 'Ime'),
              validator: (vrijednost) => _obavezno(vrijednost, 'Ime'),
            ),
            const SizedBox(height: Razmaci.m),
            TextFormField(
              controller: _prezime,
              decoration: const InputDecoration(labelText: 'Prezime'),
              validator: (vrijednost) => _obavezno(vrijednost, 'Prezime'),
            ),
            const SizedBox(height: Razmaci.m),
            TextFormField(
              controller: _email,
              keyboardType: TextInputType.emailAddress,
              decoration: const InputDecoration(labelText: 'Email'),
              validator: Validatori.email,
            ),
            const SizedBox(height: Razmaci.m),
            TextFormField(
              controller: _telefon,
              keyboardType: TextInputType.phone,
              decoration: const InputDecoration(
                labelText: 'Telefon',
                hintText: '+387 6X XXX XXX',
              ),
              validator: (vrijednost) =>
                  Validatori.telefon(vrijednost, obavezan: false),
            ),
            const SizedBox(height: Razmaci.m),
            InkWell(
              onTap: _odaberiDatum,
              borderRadius: BorderRadius.circular(Zaobljenja.polje),
              child: InputDecorator(
                decoration: const InputDecoration(labelText: 'Datum rođenja'),
                child: Text(Formati.datum(_datumRodjenja)),
              ),
            ),
            const SizedBox(height: Razmaci.m),
            DropdownButtonFormField<int>(
              // Kljuc prati ucitane gradove: lista stize poslije prvog crtanja, a
              // polje pocetnu vrijednost cita samo jednom.
              key: ValueKey('grad-${_gradovi.length}'),
              initialValue: _gradovi.any((x) => x.id == _gradId)
                  ? _gradId
                  : null,
              isExpanded: true,
              decoration: const InputDecoration(
                labelText: 'Grad',
                helperText: 'Po gradu vam prvo nudimo najbliže poslovnice.',
              ),
              items: [
                for (final grad in _gradovi)
                  DropdownMenuItem(value: grad.id, child: Text(grad.naziv)),
              ],
              onChanged: (vrijednost) => setState(() => _gradId = vrijednost),
              validator: (vrijednost) =>
                  vrijednost == null ? 'Odaberite grad u kojem ste.' : null,
            ),
            const SizedBox(height: Razmaci.xl),
            FilledButton(
              onPressed: _slanje ? null : _sacuvaj,
              child: Text(_slanje ? 'Čuvanje...' : 'Sačuvaj'),
            ),
          ],
        ),
      ),
    );
  }

  static String? _obavezno(String? vrijednost, String naziv) {
    final tekst = vrijednost?.trim() ?? '';

    if (tekst.isEmpty) {
      return '$naziv je obavezno.';
    }

    if (tekst.length < 2) {
      return '$naziv mora imati najmanje 2 znaka.';
    }

    return null;
  }
}
