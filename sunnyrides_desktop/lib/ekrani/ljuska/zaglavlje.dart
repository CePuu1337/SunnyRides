import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../stanje/sesija.dart';
import 'meni.dart';

/// Gornja traka: naslov ekrana lijevo, zvono i nalog desno.
class Zaglavlje extends StatelessWidget {
  const Zaglavlje({
    super.key,
    required this.stavka,
    required this.naNotifikacije,
  });

  final StavkaMenija stavka;
  final VoidCallback naNotifikacije;

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
          _Zvono(naPritisak: naNotifikacije),
          const SizedBox(width: Razmaci.l),
          Container(width: 1, height: 32, color: Boje.ivica),
          const SizedBox(width: Razmaci.l),
          if (korisnik != null) _Nalog(korisnik: korisnik),
        ],
      ),
    );
  }
}

class _Zvono extends StatelessWidget {
  const _Zvono({required this.naPritisak});

  final VoidCallback naPritisak;

  @override
  Widget build(BuildContext context) {
    final neprocitanih = context.watch<NotifikacijeStanje>().neprocitanih;

    return Tooltip(
      message: 'Obavještenja',
      child: InkWell(
        onTap: naPritisak,
        borderRadius: BorderRadius.circular(Zaobljenja.dugme),
        child: Padding(
          padding: const EdgeInsets.all(Razmaci.s),
          child: Stack(
            clipBehavior: Clip.none,
            children: [
              const Icon(
                Icons.notifications_none,
                size: 22,
                color: Boje.tekstBlazi,
              ),
              if (neprocitanih > 0)
                Positioned(
                  right: -6,
                  top: -4,
                  child: Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 5,
                      vertical: 1,
                    ),
                    constraints: const BoxConstraints(minWidth: 17),
                    decoration: BoxDecoration(
                      color: Boje.greska,
                      borderRadius: BorderRadius.circular(Zaobljenja.pilula),
                    ),
                    child: Text(
                      neprocitanih > 99 ? '99+' : '$neprocitanih',
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 10,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                ),
            ],
          ),
        ),
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
        if (izbor == 'odjava') {
          _odjava(context);
        }
      },
      itemBuilder: (context) => const [
        PopupMenuItem(
          value: 'profil',
          child: Row(
            children: [
              Icon(Icons.person_outline, size: 18),
              SizedBox(width: Razmaci.m),
              Text('Moj profil'),
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
                korisnik.glavnaUloga,
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
