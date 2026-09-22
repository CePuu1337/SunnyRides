import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/rezervacija.dart';
import '../../modeli/stavka_sifrarnika.dart';
import '../../servisi/rezervacija_servis.dart';
import '../../servisi/sifrarnik_servis.dart';
import '../../widgeti/dijalog_forme.dart';
import '../../widgeti/obavjestenje.dart';

/// Otkazivanje rezervacije sa prikazom obracuna povrata.
///
/// Obracun stize sa servera prije nego se bilo sta otkaze, pa uposlenik vidi tacno
/// koliko se vraca i moze to reci klijentu prije nego potvrdi.
class OtkazivanjeDijalog extends StatefulWidget {
  const OtkazivanjeDijalog({super.key, required this.rezervacija});

  final Rezervacija rezervacija;

  @override
  State<OtkazivanjeDijalog> createState() => _OtkazivanjeDijalogStanje();
}

class _OtkazivanjeDijalogStanje extends State<OtkazivanjeDijalog> {
  late final RezervacijaServis _servis;
  late final SifrarnikServis _sifrarnici;

  final _napomena = TextEditingController();

  ObracunOtkazivanja? _obracun;
  List<StavkaSifrarnika> _razlozi = const [];
  int? _razlogId;

  bool _ucitavanje = true;
  bool _snimanje = false;
  String? _greska;

  @override
  void initState() {
    super.initState();

    final klijent = context.read<ApiKlijent>();
    _servis = RezervacijaServis(klijent);
    _sifrarnici = SifrarnikServis(klijent);

    _ucitaj();
  }

  @override
  void dispose() {
    _napomena.dispose();
    super.dispose();
  }

  Future<void> _ucitaj() async {
    try {
      final obracun = await _servis.obracunOtkazivanja(widget.rezervacija.id);
      final razlozi = await _sifrarnici.ucitaj(
        SifrarnikServis.razloziOtkazivanja,
      );

      if (!mounted) {
        return;
      }

      setState(() {
        _obracun = obracun;
        _razlozi = razlozi;
        _ucitavanje = false;
      });
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

  Future<void> _otkazi() async {
    if (_razlogId == null) {
      setState(() => _greska = 'Odaberite razlog otkazivanja.');

      return;
    }

    setState(() {
      _snimanje = true;
      _greska = null;
    });

    try {
      await _servis.otkazi(
        widget.rezervacija.id,
        razlogId: _razlogId!,
        napomena: _napomena.text.trim().isEmpty ? null : _napomena.text.trim(),
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
    final obracun = _obracun;
    final mozeOtkazati = obracun?.mozeSeOtkazati ?? false;

    return DijalogForme(
      naslov: 'Otkazivanje rezervacije',
      podnaslov: widget.rezervacija.broj,
      greska: _greska,
      uToku: _snimanje,
      natpisPotvrde: 'Otkaži rezervaciju',
      potvrdaOmogucena: mozeOtkazati,
      naSnimanje: _otkazi,
      sirina: 580,
      dijete: _ucitavanje
          ? const Padding(
              padding: EdgeInsets.all(Razmaci.xxl),
              child: Center(child: CircularProgressIndicator()),
            )
          : obracun == null
          ? const Text('Obračun nije dostupan.')
          : Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                if (!mozeOtkazati)
                  Obavjestenje.upozorenje(
                    obracun.razlogNemogucnosti ??
                        'Ova rezervacija se više ne može otkazati.',
                  )
                else ...[
                  Obavjestenje.upozorenje(
                    'Otkazivanje se ne može poništiti. Termin se odmah oslobađa '
                    'za druge klijente.',
                  ),
                  const SizedBox(height: Razmaci.l),
                  _Obracun(obracun: obracun),
                  const SizedBox(height: Razmaci.l),
                  DropdownButtonFormField<int>(
                    initialValue: _razlogId,
                    isExpanded: true,
                    decoration: const InputDecoration(
                      labelText: 'Razlog otkazivanja',
                    ),
                    items: [
                      for (final razlog in _razlozi)
                        DropdownMenuItem(
                          value: razlog.id,
                          child: Text(razlog.naziv),
                        ),
                    ],
                    onChanged: (id) => setState(() => _razlogId = id),
                  ),
                  const SizedBox(height: Razmaci.l),
                  TextField(
                    controller: _napomena,
                    maxLines: 3,
                    decoration: const InputDecoration(
                      labelText: 'Napomena (nije obavezna)',
                      alignLabelWithHint: true,
                    ),
                  ),
                ],
              ],
            ),
    );
  }
}

class _Obracun extends StatelessWidget {
  const _Obracun({required this.obracun});

  final ObracunOtkazivanja obracun;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(Razmaci.l),
      decoration: BoxDecoration(
        color: Boje.platno,
        borderRadius: BorderRadius.circular(Zaobljenja.dugme),
      ),
      child: Column(
        children: [
          _Red('Naplaćeno', Formati.novac(obracun.naplaceno)),
          if (obracun.vecVraceno > 0)
            _Red('Već vraćeno', '− ${Formati.novac(obracun.vecVraceno)}'),
          _Red(
            'Povrat najma (${Formati.postotak(obracun.procenatPovrataNajma)})',
            Formati.novac(obracun.povratNajma),
          ),
          _Red('Povrat depozita', Formati.novac(obracun.povratDepozita)),
          const Divider(height: Razmaci.xl),
          _Red(
            'Ukupan povrat',
            Formati.novac(obracun.ukupanPovrat),
            istaknuto: true,
          ),
          _Red('Zadržava agencija', Formati.novac(obracun.zadrzanoAgenciji)),
          const SizedBox(height: Razmaci.m),
          Align(
            alignment: Alignment.centerLeft,
            child: Text(
              obracun.obrazlozenje,
              style: const TextStyle(
                color: Boje.tekstPrigusen,
                fontSize: 12,
                height: 1.4,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _Red extends StatelessWidget {
  const _Red(this.natpis, this.vrijednost, {this.istaknuto = false});

  final String natpis;
  final String vrijednost;
  final bool istaknuto;

  @override
  Widget build(BuildContext context) {
    final stil = TextStyle(
      fontSize: istaknuto ? 14 : 13,
      fontWeight: istaknuto ? FontWeight.w700 : FontWeight.w400,
      color: istaknuto ? Boje.tekst : Boje.tekstBlazi,
    );

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        children: [
          Expanded(child: Text(natpis, style: stil)),
          Text(vrijednost, style: stil),
        ],
      ),
    );
  }
}
