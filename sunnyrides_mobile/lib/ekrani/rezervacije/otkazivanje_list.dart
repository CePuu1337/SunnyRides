import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../servisi/rezervacija_servis.dart';
import '../../widgeti/obavjestenje.dart';

/// Otkazivanje rezervacije, sa obracunom povrata prije nego korisnik potvrdi.
///
/// Iznos povrata racuna server iz stvarno naplacenog. Ovaj ekran ga samo prikazuje -
/// i prikazuje ga *prije* potvrde, da korisnik zna sta gubi.
class OtkazivanjeList extends StatefulWidget {
  const OtkazivanjeList({super.key, required this.rezervacija});

  final Rezervacija rezervacija;

  @override
  State<OtkazivanjeList> createState() => _OtkazivanjeListStanje();
}

class _OtkazivanjeListStanje extends State<OtkazivanjeList> {
  late final RezervacijaServis _servis;

  final _napomena = TextEditingController();

  ObracunOtkazivanja? _obracun;
  List<RazlogOtkazivanja> _razlozi = const [];
  int? _razlogId;

  bool _ucitavanje = true;
  bool _slanje = false;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = RezervacijaServis(context.read<ApiKlijent>());
    _ucitaj();
  }

  @override
  void dispose() {
    _napomena.dispose();
    super.dispose();
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      final rezultati = await Future.wait([
        _servis.obracunOtkazivanja(widget.rezervacija.id),
        _servis.razloziOtkazivanja(),
      ]);

      if (!mounted) {
        return;
      }

      setState(() {
        _obracun = rezultati[0] as ObracunOtkazivanja;
        _razlozi = rezultati[1] as List<RazlogOtkazivanja>;
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

  RazlogOtkazivanja? get _razlog {
    for (final razlog in _razlozi) {
      if (razlog.id == _razlogId) {
        return razlog;
      }
    }

    return null;
  }

  bool get _mozePotvrditi {
    final razlog = _razlog;

    if (razlog == null || _slanje) {
      return false;
    }

    if (razlog.traziNapomenu && _napomena.text.trim().isEmpty) {
      return false;
    }

    return _obracun?.mozeSeOtkazati ?? false;
  }

  Future<void> _potvrdi() async {
    final razlog = _razlog;

    if (razlog == null) {
      return;
    }

    setState(() => _slanje = true);

    try {
      await _servis.otkazi(
        widget.rezervacija.id,
        razlogId: razlog.id,
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

      setState(() => _slanje = false);

      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(greska.poruka)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final obracun = _obracun;

    return Padding(
      padding: EdgeInsets.only(
        left: Razmaci.l,
        right: Razmaci.l,
        top: Razmaci.s,
        bottom: MediaQuery.of(context).viewInsets.bottom + Razmaci.l,
      ),
      child: _ucitavanje
          ? const Padding(
              padding: EdgeInsets.all(Razmaci.xl),
              child: Center(child: CircularProgressIndicator()),
            )
          : _greska != null
          ? Obavjestenje.greska(_greska!)
          : SingleChildScrollView(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Otkazivanje rezervacije',
                    style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
                  ),
                  const SizedBox(height: Razmaci.m),
                  if (obracun != null && !obracun.mozeSeOtkazati)
                    Obavjestenje.greska(
                      obracun.razlogNemogucnosti ??
                          'Ova rezervacija se više ne može otkazati.',
                    )
                  else if (obracun != null) ...[
                    Obavjestenje.greska(
                      'Otkazivanje je konačno. Rezervacija se ne može vratiti, '
                      'a termin se pušta drugim klijentima.',
                    ),
                    const SizedBox(height: Razmaci.m),
                    _Obracun(obracun: obracun),
                    const SizedBox(height: Razmaci.l),
                    DropdownButtonFormField<int?>(
                      initialValue: _razlogId,
                      isExpanded: true,
                      decoration: const InputDecoration(
                        labelText: 'Razlog otkazivanja',
                      ),
                      items: [
                        for (final razlog in _razlozi)
                          DropdownMenuItem<int?>(
                            value: razlog.id,
                            child: Text(razlog.naziv),
                          ),
                      ],
                      onChanged: (vrijednost) =>
                          setState(() => _razlogId = vrijednost),
                    ),
                    if (_razlog?.traziNapomenu ?? false) ...[
                      const SizedBox(height: Razmaci.m),
                      TextField(
                        controller: _napomena,
                        maxLines: 3,
                        maxLength: 500,
                        onChanged: (_) => setState(() {}),
                        decoration: const InputDecoration(
                          labelText: 'Napomena',
                          hintText: 'Kratko objašnjenje',
                        ),
                      ),
                    ],
                    const SizedBox(height: Razmaci.m),
                    Row(
                      children: [
                        Expanded(
                          child: OutlinedButton(
                            onPressed: () => Navigator.of(context).pop(),
                            child: const Text('Odustani'),
                          ),
                        ),
                        const SizedBox(width: Razmaci.m),
                        Expanded(
                          child: FilledButton(
                            onPressed: _mozePotvrditi ? _potvrdi : null,
                            style: FilledButton.styleFrom(
                              backgroundColor: Boje.greska,
                              foregroundColor: Colors.white,
                            ),
                            child: Text(
                              _slanje ? 'U toku...' : 'Otkaži rezervaciju',
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
                ],
              ),
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
      padding: const EdgeInsets.all(Razmaci.m),
      decoration: BoxDecoration(
        color: Boje.platno,
        borderRadius: BorderRadius.circular(Zaobljenja.kartica),
      ),
      child: Column(
        children: [
          _Red(oznaka: 'Naplaćeno', iznos: obracun.naplaceno),
          if (obracun.vecVraceno > 0)
            _Red(oznaka: 'Već vraćeno', iznos: obracun.vecVraceno),
          _Red(
            oznaka:
                'Povrat najma '
                '(${Formati.postotak(obracun.procenatPovrataNajma)})',
            iznos: obracun.povratNajma,
          ),
          _Red(oznaka: 'Povrat depozita', iznos: obracun.povratDepozita),
          if (obracun.zadrzanoAgenciji > 0)
            _Red(oznaka: 'Zadržava agencija', iznos: obracun.zadrzanoAgenciji),
          const Divider(height: Razmaci.l),
          _Red(
            oznaka: 'Vraća se vama',
            iznos: obracun.ukupanPovrat,
            podebljano: true,
          ),
          const SizedBox(height: Razmaci.s),
          Text(
            obracun.obrazlozenje,
            style: const TextStyle(
              fontSize: 11.5,
              height: 1.4,
              color: Boje.tekstBlazi,
            ),
          ),
        ],
      ),
    );
  }
}

class _Red extends StatelessWidget {
  const _Red({
    required this.oznaka,
    required this.iznos,
    this.podebljano = false,
  });

  final String oznaka;
  final double iznos;
  final bool podebljano;

  @override
  Widget build(BuildContext context) {
    final stil = TextStyle(
      fontSize: podebljano ? 14 : 12.5,
      fontWeight: podebljano ? FontWeight.w700 : FontWeight.w400,
    );

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        children: [
          Expanded(child: Text(oznaka, style: stil)),
          Text(Formati.novac(iznos), style: stil),
        ],
      ),
    );
  }
}
