import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/primopredaja.dart';
import '../../servisi/primopredaja_servis.dart';

/// Izdavanje i povrat jedne rezervacije, onako kako su evidentirani.
///
/// Služi da se poslije može provjeriti stanje vozila: kilometraža, gorivo, šteta i
/// fotografije, i na osnovu čega je zadržan dio depozita. Sam povrat depozita je u
/// sekciji plaćanja, jer je to povrat novca kao i svaki drugi.
class PrikazPrimopredaje extends StatelessWidget {
  const PrikazPrimopredaje({
    super.key,
    required this.primopredaje,
    required this.servis,
  });

  final List<Primopredaja> primopredaje;
  final PrimopredajaServis servis;

  Primopredaja? _tipa(TipPrimopredaje tip) =>
      primopredaje.where((x) => x.tip == tip).firstOrNull;

  @override
  Widget build(BuildContext context) {
    final izdavanje = _tipa(TipPrimopredaje.izdavanje);
    final povrat = _tipa(TipPrimopredaje.povrat);

    if (izdavanje == null) {
      return const Text(
        'Vozilo još nije izdato.',
        style: TextStyle(color: Boje.tekstPrigusen, fontSize: 13),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _Zapis(naslov: 'Izdavanje', zapis: izdavanje, servis: servis),
        const SizedBox(height: Razmaci.m),
        const Divider(height: 1),
        const SizedBox(height: Razmaci.m),
        if (povrat == null)
          const Text(
            'Vozilo još nije vraćeno.',
            style: TextStyle(color: Boje.tekstPrigusen, fontSize: 13),
          )
        else
          _Zapis(
            naslov: 'Povrat',
            zapis: povrat,
            servis: servis,
            izdavanje: izdavanje,
          ),
      ],
    );
  }
}

class _Zapis extends StatelessWidget {
  const _Zapis({
    required this.naslov,
    required this.zapis,
    required this.servis,
    this.izdavanje,
  });

  final String naslov;
  final Primopredaja zapis;
  final PrimopredajaServis servis;

  /// Kod povrata: zapis sa izdavanja, radi pređenih kilometara i potrošenog goriva.
  final Primopredaja? izdavanje;

  @override
  Widget build(BuildContext context) {
    final izdavanje = this.izdavanje;
    final predjeno = izdavanje == null
        ? null
        : zapis.kilometraza - izdavanje.kilometraza;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            Text(
              naslov,
              style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
            ),
            const SizedBox(width: Razmaci.s),
            if (zapis.unesenoNaknadno)
              const Tooltip(
                message:
                    'Zapis je unesen poslije stvarne primopredaje. Vrijeme ispod je '
                    'vrijeme primopredaje, ne unosa.',
                child: StatusnaPilula(
                  tekst: 'Uneseno naknadno',
                  pozadina: Boje.upozorenjePozadina,
                  bojaTeksta: Boje.upozorenjeTekst,
                ),
              ),
          ],
        ),
        const SizedBox(height: Razmaci.s),
        _Red('Vrijeme', Formati.datumIVrijeme(zapis.datumVrijeme)),
        _Red('Uposlenik', zapis.izvrsioKorisnikIme ?? '—'),
        _Red(
          'Kilometraža',
          predjeno == null
              ? '${Formati.broj(zapis.kilometraza)} km'
              : '${Formati.broj(zapis.kilometraza)} km (pređeno ${Formati.broj(predjeno)} km)',
        ),
        _Red(
          'Gorivo',
          izdavanje == null
              ? '${zapis.nivoGoriva} %'
              : '${zapis.nivoGoriva} % (pri izdavanju ${izdavanje.nivoGoriva} %)',
        ),
        if (zapis.napomena?.isNotEmpty == true)
          _Red('Napomena', zapis.napomena!),
        if (zapis.imaOstecenje) ...[
          const SizedBox(height: Razmaci.s),
          Container(
            padding: const EdgeInsets.all(Razmaci.m),
            decoration: BoxDecoration(
              color: Boje.greskaPozadina,
              borderRadius: BorderRadius.circular(Zaobljenja.dugme),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Šteta: ${zapis.iznosStete == null ? '—' : Formati.novac(zapis.iznosStete!)}',
                  style: const TextStyle(
                    fontWeight: FontWeight.w600,
                    color: Boje.greskaTekst,
                  ),
                ),
                if (zapis.opisStete?.isNotEmpty == true) ...[
                  const SizedBox(height: 2),
                  Text(zapis.opisStete!, style: const TextStyle(fontSize: 13)),
                ],
              ],
            ),
          ),
        ],
        if (zapis.fotografijaIds.isNotEmpty) ...[
          const SizedBox(height: Razmaci.s),
          GalerijaPrimopredaje(ids: zapis.fotografijaIds, servis: servis),
        ],
      ],
    );
  }
}

class _Red extends StatelessWidget {
  const _Red(this.natpis, this.vrijednost);

  final String natpis;
  final String vrijednost;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 100,
            child: Text(
              natpis,
              style: const TextStyle(color: Boje.tekstPrigusen, fontSize: 13),
            ),
          ),
          Expanded(
            child: Text(vrijednost, style: const TextStyle(fontSize: 13)),
          ),
        ],
      ),
    );
  }
}

/// Sličice fotografija primopredaje. Svaka se preuzima jednom, kad se galerija
/// otvori, a ne pri svakom iscrtavanju; klik otvara uvećanu fotografiju.
class GalerijaPrimopredaje extends StatefulWidget {
  const GalerijaPrimopredaje({
    super.key,
    required this.ids,
    required this.servis,
  });

  final List<int> ids;
  final PrimopredajaServis servis;

  @override
  State<GalerijaPrimopredaje> createState() => _GalerijaPrimopredajeStanje();
}

class _GalerijaPrimopredajeStanje extends State<GalerijaPrimopredaje> {
  late final Map<int, Future<Uint8List>> _fotografije;

  @override
  void initState() {
    super.initState();

    _fotografije = {
      for (final id in widget.ids) id: widget.servis.fotografija(id),
    };
  }

  void _uvecaj(Uint8List bajtovi) {
    showDialog<void>(
      context: context,
      builder: (context) => Dialog(
        child: Stack(
          children: [
            ConstrainedBox(
              constraints: BoxConstraints(
                maxWidth: MediaQuery.of(context).size.width * 0.8,
                maxHeight: MediaQuery.of(context).size.height * 0.85,
              ),
              child: InteractiveViewer(
                child: Image.memory(bajtovi, fit: BoxFit.contain),
              ),
            ),
            Positioned(
              top: Razmaci.s,
              right: Razmaci.s,
              child: IconButton.filledTonal(
                tooltip: 'Zatvori',
                onPressed: () => Navigator.of(context).pop(),
                icon: const Icon(Icons.close),
              ),
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: Razmaci.s,
      runSpacing: Razmaci.s,
      children: [
        for (final id in widget.ids)
          FutureBuilder<Uint8List>(
            future: _fotografije[id],
            builder: (context, stanje) {
              final bajtovi = stanje.data;

              return SizedBox(
                width: 84,
                height: 84,
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(Zaobljenja.dugme),
                  child: bajtovi != null
                      ? InkWell(
                          onTap: () => _uvecaj(bajtovi),
                          child: Image.memory(bajtovi, fit: BoxFit.cover),
                        )
                      : Container(
                          color: Boje.platno,
                          alignment: Alignment.center,
                          child: stanje.hasError
                              ? const Tooltip(
                                  message: 'Fotografija se nije mogla učitati.',
                                  child: Icon(
                                    Icons.broken_image_outlined,
                                    color: Boje.tekstPrigusen,
                                  ),
                                )
                              : const SizedBox(
                                  width: 18,
                                  height: 18,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                  ),
                                ),
                        ),
                ),
              );
            },
          ),
      ],
    );
  }
}
