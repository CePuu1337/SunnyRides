import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../ekrani/notifikacije/notifikacije_ekran.dart';

/// Zvono sa brojem neprocitanih obavjestenja.
class Zvono extends StatelessWidget {
  const Zvono({super.key, this.bijelo = false});

  /// Na tamnoj pozadini zaglavlja ikona je bijela.
  final bool bijelo;

  @override
  Widget build(BuildContext context) {
    final neprocitanih = context.watch<NotifikacijeStanje>().neprocitanih;
    final stanje = context.read<NotifikacijeStanje>();

    return Stack(
      clipBehavior: Clip.none,
      children: [
        IconButton(
          onPressed: () async {
            await Navigator.of(context).push(
              MaterialPageRoute<void>(
                builder: (_) => const NotifikacijeEkran(),
              ),
            );

            await stanje.osvjezi();
          },
          icon: Icon(
            Icons.notifications_none,
            color: bijelo ? Colors.white : Boje.tekst,
          ),
          tooltip: 'Obavještenja',
        ),
        if (neprocitanih > 0)
          Positioned(
            right: 6,
            top: 6,
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1),
              constraints: const BoxConstraints(minWidth: 17),
              decoration: BoxDecoration(
                color: Boje.greska,
                borderRadius: BorderRadius.circular(Zaobljenja.pilula),
                border: Border.all(
                  color: bijelo ? Boje.navy : Colors.white,
                  width: 1.5,
                ),
              ),
              child: Text(
                neprocitanih > 9 ? '9+' : '$neprocitanih',
                textAlign: TextAlign.center,
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 10,
                  fontWeight: FontWeight.w700,
                  height: 1.3,
                ),
              ),
            ),
          ),
      ],
    );
  }
}
