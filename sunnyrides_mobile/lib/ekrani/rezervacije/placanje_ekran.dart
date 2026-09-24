import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_stripe/flutter_stripe.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/placanje.dart';
import '../../servisi/rezervacija_servis.dart';
import '../../widgeti/obavjestenje.dart';
import 'detalji_rezervacije_ekran.dart';

/// Placanje rezervacije kroz Stripe PaymentSheet.
///
/// Aplikacija ne zna iznos naplate - server ga je upisao u intent. Kad PaymentSheet
/// javi uspjeh, to se ne uzima kao dokaz: rezervacija se oznacava placenom samo
/// nakon sto server provjeri stanje kod Stripe-a.
class PlacanjeEkran extends StatefulWidget {
  const PlacanjeEkran({super.key, required this.rezervacija});

  final Rezervacija rezervacija;

  @override
  State<PlacanjeEkran> createState() => _PlacanjeEkranStanje();
}

class _PlacanjeEkranStanje extends State<PlacanjeEkran> {
  late final RezervacijaServis _servis;

  PlatniIntent? _intent;
  Timer? _otkucaj;
  int? _preostalo;

  bool _ucitavanje = true;
  bool _placanje = false;
  bool _placeno = false;
  String? _greska;
  String? _porukaNaplate;

  @override
  void initState() {
    super.initState();

    _servis = RezervacijaServis(context.read<ApiKlijent>());
    _pripremi();
  }

  @override
  void dispose() {
    _otkucaj?.cancel();
    super.dispose();
  }

  Future<void> _pripremi() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      final intent = await _servis.platniIntent(widget.rezervacija.id);

      if (!mounted) {
        return;
      }

      setState(() {
        _intent = intent;
        _placeno = intent.isPaid;
        _ucitavanje = false;
      });

      _pokreniOdbrojavanje(intent.preostaloSekundiDrzanja);
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

  /// Odbrojavanje polazi od broja sekundi koji je poslao server, ne od sata na
  /// uredjaju - pogresno postavljen sat inace pomjeri rok.
  void _pokreniOdbrojavanje(int? sekundi) {
    _otkucaj?.cancel();

    if (sekundi == null || sekundi <= 0 || _placeno) {
      setState(() => _preostalo = sekundi);

      return;
    }

    setState(() => _preostalo = sekundi);

    _otkucaj = Timer.periodic(const Duration(seconds: 1), (otkucaj) {
      if (!mounted) {
        otkucaj.cancel();

        return;
      }

      final ostalo = (_preostalo ?? 0) - 1;

      setState(() => _preostalo = ostalo);

      if (ostalo <= 0) {
        otkucaj.cancel();
      }
    });
  }

  Future<void> _plati() async {
    final intent = _intent;

    if (intent == null ||
        intent.clientSecret == null ||
        intent.publishableKey == null) {
      return;
    }

    setState(() {
      _placanje = true;
      _porukaNaplate = null;
    });

    try {
      // Javni kljuc dolazi sa servera, pa se postavlja prije svakog otvaranja
      // PaymentSheet-a umjesto da stoji upisan u aplikaciji.
      Stripe.publishableKey = intent.publishableKey!;
      await Stripe.instance.applySettings();

      await Stripe.instance.initPaymentSheet(
        paymentSheetParameters: SetupPaymentSheetParameters(
          paymentIntentClientSecret: intent.clientSecret!,
          merchantDisplayName: 'SunnyRides',
        ),
      );

      await Stripe.instance.presentPaymentSheet();

      // Potvrda ide serveru. On pita Stripe i tek onda mijenja status rezervacije.
      final placeno = await _servis.potvrdiPlacanje(intent.placanjeId);

      if (!mounted) {
        return;
      }

      _otkucaj?.cancel();

      setState(() {
        _placanje = false;
        _placeno = placeno;
        _porukaNaplate = placeno
            ? null
            : 'Naplata je poslana, ali je još nije potvrđena. Provjerite '
                  'rezervaciju za nekoliko trenutaka.';
      });
    } on StripeException catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() {
        _placanje = false;
        _porukaNaplate =
            greska.error.localizedMessage ??
            'Plaćanje nije završeno. Možete pokušati ponovo.';
      });
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() {
        _placanje = false;
        _porukaNaplate = greska.poruka;
      });
    }
  }

  /// Preostalo vrijeme u obliku "12:05". Formati.trajanje racuna u satima i
  /// danima, pa za rok od petnaest minuta ne bi pokazao nista korisno.
  static String _odbrojavanje(int sekundi) {
    final minute = sekundi ~/ 60;
    final ostatak = sekundi % 60;

    return '$minute:${ostatak.toString().padLeft(2, '0')}';
  }

  void _otvoriRezervaciju() {
    Navigator.of(context).pushReplacement(
      MaterialPageRoute<void>(
        builder: (_) =>
            DetaljiRezervacijeEkran(rezervacijaId: widget.rezervacija.id),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final intent = _intent;
    final rezervacija = widget.rezervacija;
    final ostalo = _preostalo;

    return Scaffold(
      backgroundColor: Boje.platno,
      appBar: AppBar(title: const Text('Plaćanje')),
      body: Sadrzaj(
        ucitavanje: _ucitavanje,
        greska: _greska,
        naPonovniPokusaj: _pripremi,
        dijete: ListView(
          padding: const EdgeInsets.all(Razmaci.l),
          children: [
            if (_placeno)
              Obavjestenje.uspjeh(
                'Plaćanje je potvrđeno. Rezervacija ${rezervacija.broj} je '
                'aktivna i vozilo vas čeka u zakazanom terminu.',
              )
            else if (ostalo != null && ostalo <= 0)
              Obavjestenje.greska(
                'Vrijeme za plaćanje je isteklo, pa je vozilo pušteno u ponudu. '
                'Rezervaciju možete napraviti ponovo.',
              )
            else if (ostalo != null)
              Obavjestenje.upozorenje(
                'Vozilo je rezervisano za vas još ${_odbrojavanje(ostalo)}. '
                'Nakon toga se termin pušta u ponudu.',
                naslov: 'Rok za plaćanje',
              ),
            const SizedBox(height: Razmaci.l),
            Container(
              padding: const EdgeInsets.all(Razmaci.l),
              decoration: BoxDecoration(
                color: Boje.povrsina,
                borderRadius: BorderRadius.circular(Zaobljenja.kartica),
                border: Border.all(color: Boje.ivica),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Rezervacija ${rezervacija.broj}',
                    style: const TextStyle(
                      fontSize: 14.5,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: Razmaci.s),
                  Text(
                    '${rezervacija.vozilo}\n'
                    '${Formati.datumIVrijeme(rezervacija.datumOd)} - '
                    '${Formati.datumIVrijeme(rezervacija.datumDo)}',
                    style: const TextStyle(
                      fontSize: 12.5,
                      height: 1.5,
                      color: Boje.tekstBlazi,
                    ),
                  ),
                  const Divider(height: Razmaci.xl),
                  Row(
                    children: [
                      const Expanded(
                        child: Text(
                          'Za naplatu',
                          style: TextStyle(
                            fontSize: 14,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                      Text(
                        Formati.novac(intent?.iznos ?? rezervacija.ukupanIznos),
                        style: const TextStyle(
                          fontSize: 17,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: Razmaci.xs),
                  Text(
                    'Iznos uključuje depozit od '
                    '${Formati.novac(rezervacija.iznosDepozita)}, koji se vraća '
                    'nakon povrata vozila.',
                    style: const TextStyle(
                      fontSize: 11.5,
                      color: Boje.tekstPrigusen,
                    ),
                  ),
                ],
              ),
            ),
            if (_porukaNaplate != null) ...[
              const SizedBox(height: Razmaci.l),
              Obavjestenje.upozorenje(_porukaNaplate!),
            ],
            if (!_placeno) ...[
              const SizedBox(height: Razmaci.l),
              Obavjestenje.info(
                'Naplata ide preko Stripe-a, u testnom režimu. Za probu se koristi '
                'kartica 4242 4242 4242 4242, bilo koji budući datum i CVC 123.',
              ),
            ],
            const SizedBox(height: Razmaci.xl),
            if (_placeno)
              FilledButton(
                onPressed: _otvoriRezervaciju,
                child: const Text('Pogledaj rezervaciju'),
              )
            else
              FilledButton.icon(
                onPressed: _placanje || (ostalo != null && ostalo <= 0)
                    ? null
                    : _plati,
                icon: _placanje
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Icon(Icons.credit_card, size: 18),
                label: Text(_placanje ? 'U toku...' : 'Plati karticom'),
              ),
            const SizedBox(height: Razmaci.s),
            if (!_placeno)
              TextButton(
                onPressed: _otvoriRezervaciju,
                child: const Text('Plati kasnije'),
              ),
          ],
        ),
      ),
    );
  }
}
