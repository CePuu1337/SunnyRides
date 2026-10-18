import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../stanje/sesija.dart';
import 'meni.dart';
import 'promjena_lozinke_dijalog.dart';

/// Gornja traka: naslov ekrana lijevo, nalog desno.
///
/// Zvona za obavjestenja ovdje nema. Obavjestenja dobijaju klijenti, u mobilnoj
/// aplikaciji; osoblju je ono sto ceka obradu na Pregledu poslovanja.
class Zaglavlje extends StatelessWidget {
  const Zaglavlje({super.key, required this.stavka});

  final StavkaMenija stavka;

  static const visina = 76.0;

  @override
  Widget build(BuildContext context) {
    final korisnik = context.watch<Sesija>().korisnik;

    return Container(
      height: visina,
      padding: const EdgeInsets.symmetric(horizontal: Razmaci.ekranMargina),
      decoration: const BoxDecoration(
        color: Boje.povrsina,
        border: Border(bottom: BorderSide(color: Boje.ivica)),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  stavka.naziv,
                  style: const TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.w600,
                    letterSpacing: -0.2,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  stavka.podnaslov,
                  style: const TextStyle(
                    color: Boje.tekstPrigusen,
                    fontSize: 13,
                  ),
                ),
              ],
            ),
          ),
          if (korisnik != null) _Nalog(korisnik: korisnik),
        ],
      ),
    );
  }
}

class _Nalog extends StatelessWidget {
  const _Nalog({required this.korisnik});

  final Korisnik korisnik;

  Future<void> _odjava(BuildContext context) async {
    final potvrda = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Odjava'),
        content: const Text('Želite li se odjaviti iz aplikacije?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Odustani'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Odjavi se'),
          ),
        ],
      ),
    );

    if (potvrda != true || !context.mounted) {
      return;
    }

    await context.read<Sesija>().odjava();
  }

  Future<void> _promjenaLozinke(BuildContext context) async {
    final promijenjena = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (context) => const PromjenaLozinkeDijalog(),
    );

    if (promijenjena != true || !context.mounted) {
      return;
    }

    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text(
          'Lozinka je promijenjena. Na drugim uređajima ćete se morati prijaviti '
          'ponovo.',
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final okruzenje = context.read<Okruzenje>();
    final slika = okruzenje.apsolutnaSlika(
      korisnik.thumbnailUrl ?? korisnik.putanjaSlike,
    );

    return PopupMenuButton<String>(
      tooltip: '',
      offset: const Offset(0, 48),
      position: PopupMenuPosition.under,
      onSelected: (izbor) {
        switch (izbor) {
          case 'lozinka':
            _promjenaLozinke(context);
          case 'odjava':
            _odjava(context);
        }
      },
      itemBuilder: (context) => const [
        PopupMenuItem(
          value: 'lozinka',
          child: Row(
            children: [
              Icon(Icons.lock_outline, size: 18),
              SizedBox(width: Razmaci.m),
              Text('Promijeni lozinku'),
            ],
          ),
        ),
        PopupMenuDivider(),
        PopupMenuItem(
          value: 'odjava',
          child: Row(
            children: [
              Icon(Icons.logout, size: 18, color: Boje.greskaTekst),
              SizedBox(width: Razmaci.m),
              Text('Odjava', style: TextStyle(color: Boje.greskaTekst)),
            ],
          ),
        ),
      ],
      child: Row(
        children: [
          CircleAvatar(
            radius: 17,
            backgroundColor: Boje.primarnaSvijetla,
            foregroundImage: slika == null ? null : NetworkImage(slika),
            child: Text(
              korisnik.inicijali,
              style: const TextStyle(
                color: Boje.primarnaTamnija,
                fontSize: 12,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
          const SizedBox(width: Razmaci.m),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text(
                korisnik.punoIme,
                style: const TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                ),
              ),
              Text(
                // Uposlenik uz ulogu vidi i poslovnicu, jer mu je cijela
                // aplikacija svedena na nju.
                korisnik.vezanZaPoslovnicu && korisnik.poslovnicaNaziv != null
                    ? '${korisnik.glavnaUloga} · ${korisnik.poslovnicaNaziv}'
                    : korisnik.glavnaUloga,
                style: const TextStyle(
                  color: Boje.tekstPrigusen,
                  fontSize: 11.5,
                ),
              ),
            ],
          ),
          const SizedBox(width: Razmaci.xs),
          const Icon(Icons.expand_more, size: 18, color: Boje.tekstPrigusen),
        ],
      ),
    );
  }
}
