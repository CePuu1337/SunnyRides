import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../widgeti/obavjestenje.dart';

/// Reset zaboravljene lozinke kodom poslanim na email.
///
/// Dva koraka na istom ekranu: prvo email, pa kod iz maila i nova lozinka. Server na
/// prvi korak odgovara isto postojao nalog ili ne, pa ni ekran to ne razlikuje - u
/// suprotnom bi se kroz njega moglo provjeravati ko je registrovan.
class ZaboravljenaLozinkaEkran extends StatefulWidget {
  const ZaboravljenaLozinkaEkran({super.key});

  @override
  State<ZaboravljenaLozinkaEkran> createState() =>
      _ZaboravljenaLozinkaEkranStanje();
}

class _ZaboravljenaLozinkaEkranStanje extends State<ZaboravljenaLozinkaEkran> {
  final _formaEmail = GlobalKey<FormState>();
  final _formaKod = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _kod = TextEditingController();
  final _nova = TextEditingController();
  final _potvrda = TextEditingController();

  bool _kodPoslan = false;
  bool _slanje = false;
  String? _greska;
  String? _info;

  @override
  void dispose() {
    _email.dispose();
    _kod.dispose();
    _nova.dispose();
    _potvrda.dispose();
    super.dispose();
  }

  Future<void> _posaljiKod() async {
    if (!(_formaEmail.currentState?.validate() ?? false) || _slanje) {
      return;
    }

    setState(() {
      _slanje = true;
      _greska = null;
      _info = null;
    });

    try {
      await context.read<AuthServis>().zaboravljenaLozinka(_email.text.trim());

      if (!mounted) {
        return;
      }

      setState(() {
        _slanje = false;
        _kodPoslan = true;
        _info =
            'Ako za ${_email.text.trim()} postoji nalog, na tu adresu je '
            'poslan kod od 8 znakova. Kod vrijedi 15 minuta i može se '
            'iskoristiti samo jednom.';
      });
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

  Future<void> _promijeni() async {
    if (!(_formaKod.currentState?.validate() ?? false) || _slanje) {
      return;
    }

    setState(() {
      _slanje = true;
      _greska = null;
    });

    try {
      await context.read<AuthServis>().resetLozinke(
        email: _email.text.trim(),
        kod: _kod.text.trim(),
        nova: _nova.text,
        potvrda: _potvrda.text,
      );

      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text(
            'Lozinka je promijenjena. Prijavite se novom lozinkom.',
          ),
        ),
      );

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

  void _drugaAdresa() {
    setState(() {
      _kodPoslan = false;
      _greska = null;
      _info = null;
      _kod.clear();
      _nova.clear();
      _potvrda.clear();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Boje.povrsina,
      appBar: AppBar(title: const Text('Zaboravljena lozinka')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(Razmaci.xl),
          children: [
            if (_greska != null) ...[
              Obavjestenje.greska(_greska!),
              const SizedBox(height: Razmaci.l),
            ],
            if (_info != null) ...[
              Obavjestenje.info(_info!),
              const SizedBox(height: Razmaci.l),
            ],
            _kodPoslan ? _korakKod() : _korakEmail(),
          ],
        ),
      ),
    );
  }

  Widget _korakEmail() {
    return Form(
      key: _formaEmail,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Text(
            'Upišite email adresu naloga. Na nju ćemo poslati kod kojim '
            'postavljate novu lozinku.',
            style: TextStyle(color: Boje.tekstPrigusen, fontSize: 14),
          ),
          const SizedBox(height: Razmaci.l),
          TextFormField(
            controller: _email,
            enabled: !_slanje,
            keyboardType: TextInputType.emailAddress,
            textInputAction: TextInputAction.done,
            onFieldSubmitted: (_) => _posaljiKod(),
            decoration: const InputDecoration(
              labelText: 'Email',
              prefixIcon: Icon(Icons.mail_outline),
            ),
            validator: ValidacijaResetaLozinke.email,
          ),
          const SizedBox(height: Razmaci.xl),
          SizedBox(
            height: 50,
            child: ElevatedButton(
              onPressed: _slanje ? null : _posaljiKod,
              child: Text(_slanje ? 'Slanje...' : 'Pošalji kod'),
            ),
          ),
        ],
      ),
    );
  }

  Widget _korakKod() {
    return Form(
      key: _formaKod,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TextFormField(
            controller: _kod,
            enabled: !_slanje,
            textCapitalization: TextCapitalization.characters,
            decoration: const InputDecoration(
              labelText: 'Kod iz emaila',
              hintText: 'npr. K7M2Q9XA',
              prefixIcon: Icon(Icons.pin_outlined),
            ),
            validator: ValidacijaResetaLozinke.kod,
          ),
          const SizedBox(height: Razmaci.l),
          TextFormField(
            controller: _nova,
            enabled: !_slanje,
            obscureText: true,
            decoration: const InputDecoration(
              labelText: 'Nova lozinka',
              prefixIcon: Icon(Icons.lock_outline),
            ),
            validator: ValidacijaResetaLozinke.lozinka,
          ),
          const SizedBox(height: Razmaci.l),
          TextFormField(
            controller: _potvrda,
            enabled: !_slanje,
            obscureText: true,
            onFieldSubmitted: (_) => _promijeni(),
            decoration: const InputDecoration(
              labelText: 'Potvrda nove lozinke',
              prefixIcon: Icon(Icons.lock_outline),
            ),
            validator: (vrijednost) =>
                vrijednost == _nova.text ? null : 'Lozinke se ne poklapaju.',
          ),
          const SizedBox(height: Razmaci.xl),
          SizedBox(
            height: 50,
            child: ElevatedButton(
              onPressed: _slanje ? null : _promijeni,
              child: Text(_slanje ? 'Čuvanje...' : 'Postavi novu lozinku'),
            ),
          ),
          const SizedBox(height: Razmaci.m),
          TextButton(
            onPressed: _slanje ? null : _posaljiKod,
            child: const Text('Pošalji novi kod'),
          ),
          TextButton(
            onPressed: _slanje ? null : _drugaAdresa,
            child: const Text('Upiši drugu email adresu'),
          ),
        ],
      ),
    );
  }
}

/// Provjere za reset lozinke, iste kao na serveru, da korisnik gresku vidi ispod
/// polja prije nego zahtjev ode.
class ValidacijaResetaLozinke {
  const ValidacijaResetaLozinke._();

  static final _oblikEmaila = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');

  /// Znakovi koje server koristi za kod: bez I, O, 0 i 1, koji se lako zamijene.
  static final _oblikKoda = RegExp(r'^[A-HJ-NP-Z2-9]{8}$');

  static String? email(String? vrijednost) {
    final tekst = vrijednost?.trim() ?? '';

    if (tekst.isEmpty) {
      return 'Unesite email adresu.';
    }

    return _oblikEmaila.hasMatch(tekst)
        ? null
        : 'Unesite email u obliku ime@primjer.com.';
  }

  /// Razmaci i crtica se zanemaruju, a mala slova prihvataju - kod se prepisuje
  /// rukom, a server ga svodi na isti oblik.
  static String? kod(String? vrijednost) {
    final tekst = (vrijednost ?? '')
        .replaceAll(RegExp(r'[\s-]'), '')
        .toUpperCase();

    if (tekst.isEmpty) {
      return 'Unesite kod iz emaila.';
    }

    return _oblikKoda.hasMatch(tekst)
        ? null
        : 'Kod ima tačno 8 znakova, slova i brojeve, npr. K7M2Q9XA.';
  }

  static String? lozinka(String? vrijednost) {
    return (vrijednost ?? '').length < 6
        ? 'Lozinka mora imati najmanje 6 znakova.'
        : null;
  }
}
