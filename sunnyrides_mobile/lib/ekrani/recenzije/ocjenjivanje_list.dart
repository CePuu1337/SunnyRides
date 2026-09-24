import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/recenzija.dart';
import '../../servisi/recenzija_servis.dart';
import '../../widgeti/obavjestenje.dart';
import '../../widgeti/ocjena.dart';

/// Ostavljanje ocjene za zavrseni najam.
class OcjenjivanjeList extends StatefulWidget {
  const OcjenjivanjeList({super.key, required this.najam});

  final RezervacijaZaRecenziju najam;

  @override
  State<OcjenjivanjeList> createState() => _OcjenjivanjeListStanje();
}

class _OcjenjivanjeListStanje extends State<OcjenjivanjeList> {
  late final RecenzijaServis _servis;
  final _komentar = TextEditingController();

  int _ocjena = 0;
  bool _slanje = false;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = RecenzijaServis(context.read<ApiKlijent>());
  }

  @override
  void dispose() {
    _komentar.dispose();
    super.dispose();
  }

  Future<void> _posalji() async {
    setState(() {
      _slanje = true;
      _greska = null;
    });

    try {
      await _servis.ostavi(
        rezervacijaId: widget.najam.rezervacijaId,
        ocjena: _ocjena,
        komentar: _komentar.text.trim().isEmpty ? null : _komentar.text.trim(),
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
        _slanje = false;
        _greska = greska.poruka;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(
        left: Razmaci.l,
        right: Razmaci.l,
        top: Razmaci.s,
        bottom: MediaQuery.of(context).viewInsets.bottom + Razmaci.l,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            widget.najam.voziloOpis ?? 'Ocjena najma',
            style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
          ),
          Text(
            '${widget.najam.broj} · '
            '${Formati.datum(widget.najam.datumOd)} - '
            '${Formati.datum(widget.najam.datumDo)}',
            style: const TextStyle(fontSize: 11.5, color: Boje.tekstPrigusen),
          ),
          const SizedBox(height: Razmaci.l),
          if (_greska != null) ...[
            Obavjestenje.greska(_greska!),
            const SizedBox(height: Razmaci.m),
          ],
          OdabirOcjene(
            ocjena: _ocjena,
            naPromjenu: (vrijednost) => setState(() => _ocjena = vrijednost),
          ),
          const SizedBox(height: Razmaci.m),
          TextField(
            controller: _komentar,
            maxLines: 4,
            maxLength: 1000,
            decoration: const InputDecoration(
              labelText: 'Komentar (nije obavezan)',
              hintText: 'Kako je vozilo izgledalo i vozilo se?',
            ),
          ),
          const SizedBox(height: Razmaci.s),
          FilledButton(
            onPressed: _ocjena == 0 || _slanje ? null : _posalji,
            child: Text(_slanje ? 'Slanje...' : 'Pošalji ocjenu'),
          ),
        ],
      ),
    );
  }
}
