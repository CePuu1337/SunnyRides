import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../widgeti/dijalog_forme.dart';
import '../../widgeti/obavjestenje.dart';

/// Odbijanje dozvole. Razlog je obavezan i ide klijentu.
///
/// Nije slobodan tekst iz pristojnosti nego zato sto klijent iz njega treba znati
/// sta da ispravi. "Nije u redu" ne pomaze nikome, pa ponudjeni razlozi pokrivaju
/// ono sto se stvarno desava, a polje ostaje izmjenjivo.
class OdbijanjeDijalog extends StatefulWidget {
  const OdbijanjeDijalog({super.key, required this.klijent});

  final String klijent;

  @override
  State<OdbijanjeDijalog> createState() => _OdbijanjeDijalogStanje();
}

class _OdbijanjeDijalogStanje extends State<OdbijanjeDijalog> {
  final _razlog = TextEditingController();
  String? _greska;

  static const _ponudjeni = <String>[
    'Fotografija je nečitka ili zamućena.',
    'Na fotografiji se ne vide svi podaci sa dozvole.',
    'Broj dozvole ne odgovara onome što piše na fotografiji.',
    'Dozvola je istekla.',
    'Podaci na dozvoli ne odgovaraju podacima na nalogu.',
  ];

  @override
  void dispose() {
    _razlog.dispose();
    super.dispose();
  }

  void _potvrdi() {
    final razlog = _razlog.text.trim();

    if (razlog.length < 5) {
      setState(() => _greska = 'Napišite razlog, bar pet znakova.');

      return;
    }

    Navigator.of(context).pop(razlog);
  }

  @override
  Widget build(BuildContext context) {
    return DijalogForme(
      naslov: 'Odbijanje dozvole',
      podnaslov: widget.klijent,
      greska: _greska,
      natpisPotvrde: 'Odbij dozvolu',
      naSnimanje: _potvrdi,
      sirina: 560,
      dijete: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Obavjestenje.info(
            'Razlog ide klijentu uz obavještenje. Napišite ga tako da zna šta da '
            'ispravi i pošalje ponovo.',
          ),
          const SizedBox(height: Razmaci.l),
          const Text(
            'Česti razlozi',
            style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: Razmaci.s),
          Wrap(
            spacing: Razmaci.s,
            runSpacing: Razmaci.s,
            children: [
              for (final ponudjen in _ponudjeni)
                ActionChip(
                  label: Text(ponudjen),
                  onPressed: () => setState(() {
                    _razlog.text = ponudjen;
                    _greska = null;
                  }),
                ),
            ],
          ),
          const SizedBox(height: Razmaci.l),
          TextField(
            controller: _razlog,
            maxLines: 3,
            maxLength: 500,
            autofocus: true,
            onChanged: (_) {
              if (_greska != null) {
                setState(() => _greska = null);
              }
            },
            decoration: const InputDecoration(
              labelText: 'Razlog odbijanja',
              alignLabelWithHint: true,
            ),
          ),
        ],
      ),
    );
  }
}
