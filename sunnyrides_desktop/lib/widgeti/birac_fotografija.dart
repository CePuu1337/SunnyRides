import 'dart:typed_data';

import 'package:file_selector/file_selector.dart';
import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Odabir fotografija koje idu uz izdavanje ili povrat vozila.
///
/// Slike se drze u memoriji dok se forma ne posalje, jer podaci i fotografije idu
/// serveru u istom zahtjevu. Dok se ne posalju, nista nije zapisano - pa se i mogu
/// ukloniti bez ikakvog traga.
class BiracFotografija extends StatefulWidget {
  const BiracFotografija({
    super.key,
    required this.fotografije,
    required this.naPromjenu,
    this.najvise = 6,
  });

  final List<FajlZaSlanje> fotografije;
  final ValueChanged<List<FajlZaSlanje>> naPromjenu;
  final int najvise;

  @override
  State<BiracFotografija> createState() => _BiracFotografijaStanje();
}

class _BiracFotografijaStanje extends State<BiracFotografija> {
  final _pregledi = <String, Uint8List>{};
  String? _greska;

  Future<void> _dodaj() async {
    const vrste = XTypeGroup(
      label: 'Slike',
      extensions: ['jpg', 'jpeg', 'png', 'webp'],
    );

    final odabrani = await openFiles(acceptedTypeGroups: [vrste]);

    if (odabrani.isEmpty) {
      return;
    }

    final slobodno = widget.najvise - widget.fotografije.length;

    if (slobodno <= 0) {
      setState(() => _greska = 'Najviše ${widget.najvise} fotografija.');

      return;
    }

    final nove = <FajlZaSlanje>[];

    for (final fajl in odabrani.take(slobodno)) {
      final bajtovi = await fajl.readAsBytes();
      final kljuc = '${fajl.name}-${bajtovi.length}';

      _pregledi[kljuc] = bajtovi;
      nove.add(FajlZaSlanje(ime: fajl.name, sadrzaj: bajtovi));
    }

    if (!mounted) {
      return;
    }

    setState(() {
      _greska = odabrani.length > slobodno
          ? 'Dodano je prvih $slobodno, ostalo prelazi ograničenje.'
          : null;
    });

    widget.naPromjenu([...widget.fotografije, ...nove]);
  }

  void _ukloni(int indeks) {
    final preostale = [...widget.fotografije]..removeAt(indeks);

    setState(() => _greska = null);
    widget.naPromjenu(preostale);
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                widget.fotografije.isEmpty
                    ? 'Nijedna fotografija'
                    : '${widget.fotografije.length} od ${widget.najvise}',
                style: const TextStyle(
                  color: Boje.tekstPrigusen,
                  fontSize: 12.5,
                ),
              ),
            ),
            OutlinedButton.icon(
              onPressed: widget.fotografije.length >= widget.najvise
                  ? null
                  : _dodaj,
              icon: const Icon(Icons.add_a_photo_outlined, size: 17),
              label: const Text('Dodaj fotografije'),
            ),
          ],
        ),
        if (_greska != null) ...[
          const SizedBox(height: Razmaci.s),
          Text(
            _greska!,
            style: const TextStyle(color: Boje.upozorenjeTekst, fontSize: 12),
          ),
        ],
        if (widget.fotografije.isNotEmpty) ...[
          const SizedBox(height: Razmaci.m),
          Wrap(
            spacing: Razmaci.s,
            runSpacing: Razmaci.s,
            children: [
              for (var i = 0; i < widget.fotografije.length; i++)
                _Pregled(
                  bajtovi: Uint8List.fromList(widget.fotografije[i].sadrzaj),
                  naUklanjanje: () => _ukloni(i),
                ),
            ],
          ),
        ],
      ],
    );
  }
}

class _Pregled extends StatelessWidget {
  const _Pregled({required this.bajtovi, required this.naUklanjanje});

  final Uint8List bajtovi;
  final VoidCallback naUklanjanje;

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(Zaobljenja.dugme),
          child: Image.memory(
            bajtovi,
            width: 96,
            height: 72,
            fit: BoxFit.cover,
          ),
        ),
        Positioned(
          right: 2,
          top: 2,
          child: InkWell(
            onTap: naUklanjanje,
            child: Container(
              padding: const EdgeInsets.all(2),
              decoration: BoxDecoration(
                color: Colors.black.withValues(alpha: 0.55),
                shape: BoxShape.circle,
              ),
              child: const Icon(Icons.close, size: 14, color: Colors.white),
            ),
          ),
        ),
      ],
    );
  }
}

/// Klizac za nivo goriva ili napunjenost baterije, u procentima.
///
/// Server u oba slucaja cuva isti broj, jer je to ista mjera. Razlikuje se samo ono
/// sto uposlenik cita: skuter na struju nema rezervoar, pa bi mu natpis "nivo goriva"
/// bio pogresan.
class KlizacGoriva extends StatelessWidget {
  const KlizacGoriva({
    super.key,
    required this.vrijednost,
    required this.naPromjenu,
    this.jeElektricno = false,
  });

  final int vrijednost;
  final ValueChanged<int> naPromjenu;
  final bool jeElektricno;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Icon(
              jeElektricno
                  ? Icons.battery_charging_full_outlined
                  : Icons.local_gas_station_outlined,
              size: 17,
              color: Boje.tekstPrigusen,
            ),
            const SizedBox(width: Razmaci.s),
            Text(
              jeElektricno ? 'Napunjenost baterije' : 'Nivo goriva',
              style: const TextStyle(fontSize: 13),
            ),
            const Spacer(),
            Text(
              '$vrijednost %',
              style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700),
            ),
          ],
        ),
        Slider(
          value: vrijednost.toDouble(),
          max: 100,
          divisions: 20,
          label: '$vrijednost %',
          onChanged: (novo) => naPromjenu(novo.round()),
        ),
      ],
    );
  }
}
