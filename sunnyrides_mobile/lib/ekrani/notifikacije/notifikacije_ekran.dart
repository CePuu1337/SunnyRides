import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/notifikacija.dart';
import '../../servisi/notifikacija_servis.dart';
import '../../widgeti/obavjestenje.dart';

/// Lista obavjestenja prijavljenog korisnika.
class NotifikacijeEkran extends StatefulWidget {
  const NotifikacijeEkran({super.key});

  @override
  State<NotifikacijeEkran> createState() => _NotifikacijeEkranStanje();
}

class _NotifikacijeEkranStanje extends State<NotifikacijeEkran> {
  late final NotifikacijaServis _servis;
  late final NotifikacijeStanje _stanje;

  List<Notifikacija> _stavke = const [];
  bool _ucitavanje = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = NotifikacijaServis(context.read<ApiKlijent>());
    _stanje = context.read<NotifikacijeStanje>();

    // Dok je lista otvorena, novo obavjestenje sa huba se odmah upisuje u nju.
    _stanje.naNovuNotifikaciju = (_) => _ucitaj();

    _ucitaj();
  }

  @override
  void dispose() {
    _stanje.naNovuNotifikaciju = null;
    super.dispose();
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      final strana = await _servis.lista(
        const OsnovniUpit(velicinaStranice: 50, ukljuciUkupno: false),
      );

      if (!mounted) {
        return;
      }

      setState(() {
        _stavke = strana.stavke;
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

  Future<void> _procitaj(Notifikacija stavka) async {
    if (stavka.procitana) {
      return;
    }

    try {
      final broj = await _servis.oznaciProcitanu(stavka.id);

      if (!mounted) {
        return;
      }

      context.read<NotifikacijeStanje>().postavi(broj);
      await _ucitaj();
    } on ApiGreska {
      // Oznaka procitanosti nije razlog da se korisniku prekine ekran.
    }
  }

  Future<void> _procitajSve() async {
    try {
      final broj = await _servis.oznaciSveProcitane();

      if (!mounted) {
        return;
      }

      context.read<NotifikacijeStanje>().postavi(broj);
      await _ucitaj();
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(greska.poruka)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final imaNeprocitanih = _stavke.any((x) => !x.procitana);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Obavještenja'),
        actions: [
          if (imaNeprocitanih)
            TextButton(
              onPressed: _procitajSve,
              child: const Text('Označi sve'),
            ),
        ],
      ),
      body: Sadrzaj(
        ucitavanje: _ucitavanje,
        greska: _greska,
        naPonovniPokusaj: _ucitaj,
        dijete: _stavke.isEmpty
            ? const PrazanPopis(
                poruka:
                    'Nema obavještenja. Ovdje ćemo javiti kad se nešto\n'
                    'desi sa vašom rezervacijom ili dozvolom.',
                ikona: Icons.notifications_none,
              )
            : RefreshIndicator(
                onRefresh: _ucitaj,
                child: ListView.separated(
                  padding: const EdgeInsets.all(Razmaci.l),
                  itemCount: _stavke.length,
                  separatorBuilder: (_, _) => const SizedBox(height: Razmaci.s),
                  itemBuilder: (context, indeks) => _Red(
                    stavka: _stavke[indeks],
                    naDodir: () => _procitaj(_stavke[indeks]),
                  ),
                ),
              ),
      ),
    );
  }
}

class _Red extends StatelessWidget {
  const _Red({required this.stavka, required this.naDodir});

  final Notifikacija stavka;
  final VoidCallback naDodir;

  @override
  Widget build(BuildContext context) {
    final (ikona, boja) = _izgled(stavka.tip);

    return Material(
      color: stavka.procitana ? Boje.povrsina : Boje.primarnaSvijetla,
      borderRadius: BorderRadius.circular(Zaobljenja.kartica),
      child: InkWell(
        onTap: naDodir,
        borderRadius: BorderRadius.circular(Zaobljenja.kartica),
        child: Container(
          padding: const EdgeInsets.all(Razmaci.m),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(Zaobljenja.kartica),
            border: Border.all(color: Boje.ivica),
          ),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 34,
                height: 34,
                decoration: BoxDecoration(
                  color: boja.withValues(alpha: 0.14),
                  borderRadius: BorderRadius.circular(Zaobljenja.dugme),
                ),
                child: Icon(ikona, size: 18, color: boja),
              ),
              const SizedBox(width: Razmaci.m),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            stavka.naslov,
                            style: TextStyle(
                              fontSize: 13.5,
                              fontWeight: stavka.procitana
                                  ? FontWeight.w600
                                  : FontWeight.w700,
                            ),
                          ),
                        ),
                        if (!stavka.procitana)
                          Container(
                            width: 8,
                            height: 8,
                            decoration: const BoxDecoration(
                              color: Boje.primarnaTamnija,
                              shape: BoxShape.circle,
                            ),
                          ),
                      ],
                    ),
                    const SizedBox(height: 3),
                    Text(
                      stavka.tekst,
                      style: const TextStyle(
                        fontSize: 12.5,
                        height: 1.4,
                        color: Boje.tekstBlazi,
                      ),
                    ),
                    const SizedBox(height: Razmaci.s),
                    Text(
                      Formati.datumIVrijeme(stavka.datumKreiranja),
                      style: const TextStyle(
                        fontSize: 11,
                        color: Boje.tekstPrigusen,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  /// Ikona i boja po tipu, da se vrsta obavjestenja vidi prije nego se procita.
  static (IconData, Color) _izgled(TipNotifikacije? tip) {
    switch (tip) {
      case TipNotifikacije.rezervacijaKreirana:
        return (Icons.event_available_outlined, Boje.info);
      case TipNotifikacije.placanjeUspjesno:
        return (Icons.credit_card, Boje.uspjeh);
      case TipNotifikacije.rezervacijaPotvrdjena:
        return (Icons.check_circle_outline, Boje.uspjeh);
      case TipNotifikacije.rezervacijaOtkazana:
        return (Icons.cancel_outlined, Boje.greska);
      case TipNotifikacije.povratIzvrsen:
        return (Icons.undo, Boje.info);
      case TipNotifikacije.dozvolaOdobrena:
        return (Icons.verified_outlined, Boje.uspjeh);
      case TipNotifikacije.dozvolaOdbijena:
        return (Icons.gpp_bad_outlined, Boje.greska);
      case TipNotifikacije.podsjetnikPreuzimanje:
        return (Icons.alarm, Boje.upozorenje);
      case TipNotifikacije.voziloVraceno:
        return (Icons.task_alt, Boje.uspjeh);
      case TipNotifikacije.resetLozinke:
        return (Icons.lock_reset, Boje.upozorenje);
      case null:
        return (Icons.notifications_none, Boje.tekstPrigusen);
    }
  }
}
