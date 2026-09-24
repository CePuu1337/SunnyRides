import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/recenzija.dart';
import '../../servisi/recenzija_servis.dart';
import '../../widgeti/obavjestenje.dart';
import '../../widgeti/ocjena.dart';
import '../../widgeti/slika.dart';
import 'ocjenjivanje_list.dart';

/// Recenzije klijenta: najmovi koje treba ocijeniti i ocjene koje je dao.
class MojeRecenzijeEkran extends StatefulWidget {
  const MojeRecenzijeEkran({super.key});

  @override
  State<MojeRecenzijeEkran> createState() => _MojeRecenzijeEkranStanje();
}

class _MojeRecenzijeEkranStanje extends State<MojeRecenzijeEkran> {
  late final RecenzijaServis _servis;

  List<RezervacijaZaRecenziju> _zaOcjenjivanje = const [];
  List<Recenzija> _moje = const [];

  bool _ucitavanje = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = RecenzijaServis(context.read<ApiKlijent>());
    _ucitaj();
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      final rezultati = await Future.wait([
        _servis.zaOcjenjivanje(),
        _servis.moje(),
      ]);

      if (!mounted) {
        return;
      }

      setState(() {
        _zaOcjenjivanje = rezultati[0] as List<RezervacijaZaRecenziju>;
        _moje = (rezultati[1] as Strana<Recenzija>).stavke;
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

  Future<void> _ocijeni(RezervacijaZaRecenziju najam) async {
    final ostavljena = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => OcjenjivanjeList(najam: najam),
    );

    if (!mounted) {
      return;
    }

    if (ostavljena == true) {
      ScaffoldMessenger.of(context)
          .showSnackBar(const SnackBar(content: Text('Hvala na ocjeni.')));
    }

    await _ucitaj();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Boje.platno,
      appBar: AppBar(title: const Text('Moje recenzije')),
      body: Sadrzaj(
        ucitavanje: _ucitavanje,
        greska: _greska,
        naPonovniPokusaj: _ucitaj,
        dijete: _zaOcjenjivanje.isEmpty && _moje.isEmpty
            ? const PrazanPopis(
                poruka: 'Nakon završenog najma ovdje možete ocijeniti vozilo.',
                ikona: Icons.star_outline,
              )
            : RefreshIndicator(
                onRefresh: _ucitaj,
                child: ListView(
                  padding: const EdgeInsets.all(Razmaci.l),
                  children: [
                    if (_zaOcjenjivanje.isNotEmpty) ...[
                      const Text(
                        'Za ocjenjivanje',
                        style: TextStyle(
                          fontSize: 15,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(height: Razmaci.s),
                      Obavjestenje.info(
                        'Vaša ocjena ulazi u preporuke - i vama i drugim klijentima.',
                      ),
                      const SizedBox(height: Razmaci.m),
                      for (final najam in _zaOcjenjivanje)
                        Padding(
                          padding: const EdgeInsets.only(bottom: Razmaci.m),
                          child: _ZaOcjenu(
                            najam: najam,
                            naDodir: () => _ocijeni(najam),
                          ),
                        ),
                      const SizedBox(height: Razmaci.l),
                    ],
                    if (_moje.isNotEmpty) ...[
                      const Text(
                        'Moje ocjene',
                        style: TextStyle(
                          fontSize: 15,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(height: Razmaci.m),
                      for (final recenzija in _moje)
                        Padding(
                          padding: const EdgeInsets.only(bottom: Razmaci.m),
                          child: _Moja(recenzija: recenzija),
                        ),
                    ],
                  ],
                ),
              ),
      ),
    );
  }
}

class _ZaOcjenu extends StatelessWidget {
  const _ZaOcjenu({required this.najam, required this.naDodir});

  final RezervacijaZaRecenziju najam;
  final VoidCallback naDodir;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(Razmaci.m),
      decoration: BoxDecoration(
        color: Boje.povrsina,
        borderRadius: BorderRadius.circular(Zaobljenja.kartica),
        border: Border.all(color: Boje.ivica),
      ),
      child: Row(
        children: [
          Slika(putanja: najam.thumbnailUrl, sirina: 64, visina: 48),
          const SizedBox(width: Razmaci.m),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  najam.voziloOpis ?? 'Vozilo',
                  style: const TextStyle(
                    fontSize: 13.5,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                Text(
                  '${najam.broj} · ${Formati.datum(najam.datumDo)}',
                  style: const TextStyle(
                    fontSize: 11.5,
                    color: Boje.tekstPrigusen,
                  ),
                ),
              ],
            ),
          ),
          FilledButton(onPressed: naDodir, child: const Text('Ocijeni')),
        ],
      ),
    );
  }
}

class _Moja extends StatelessWidget {
  const _Moja({required this.recenzija});

  final Recenzija recenzija;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(Razmaci.m),
      decoration: BoxDecoration(
        color: Boje.povrsina,
        borderRadius: BorderRadius.circular(Zaobljenja.kartica),
        border: Border.all(color: Boje.ivica),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Zvjezdice(ocjena: recenzija.ocjena.toDouble(), velicina: 15),
              const SizedBox(width: Razmaci.s),
              Expanded(
                child: Text(
                  recenzija.voziloOpis ?? '',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    fontSize: 12.5,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
              Text(
                Formati.datum(recenzija.datumKreiranja),
                style: const TextStyle(fontSize: 11, color: Boje.tekstPrigusen),
              ),
            ],
          ),
          if (recenzija.komentar != null && recenzija.komentar!.isNotEmpty) ...[
            const SizedBox(height: Razmaci.xs),
            Text(
              recenzija.komentar!,
              style: const TextStyle(
                fontSize: 12.5,
                height: 1.45,
                color: Boje.tekstBlazi,
              ),
            ),
          ],
          if (recenzija.skrivena) ...[
            const SizedBox(height: Razmaci.s),
            Obavjestenje.upozorenje(
              'Ova recenzija je skrivena od ostalih korisnika i ne ulazi u '
              'prosječnu ocjenu.',
            ),
          ],
        ],
      ),
    );
  }
}
