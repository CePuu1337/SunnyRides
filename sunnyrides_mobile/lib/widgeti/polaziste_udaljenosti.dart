import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../stanje/lokacija.dart';
import '../stanje/sesija.dart';

/// Red "Mjereno od: ..." sa dugmetom za prelaz izmedju grada iz profila i trenutne
/// lokacije telefona. Stoji na pocetnom ekranu i iznad rezultata pretrage, da je
/// uvijek jasno od cega se racuna udaljenost.
class PolazisteUdaljenosti extends StatelessWidget {
  const PolazisteUdaljenosti({super.key});

  Future<void> _ukljuci(BuildContext context) async {
    final poruka = await context.read<Lokacija>().ukljuci();

    if (poruka == null || !context.mounted) {
      return;
    }

    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(poruka)));
  }

  @override
  Widget build(BuildContext context) {
    final lokacija = context.watch<Lokacija>();
    final grad = context.watch<Sesija>().korisnik?.gradNaziv;

    final opis = lokacija.ukljucena
        ? 'vaša trenutna lokacija'
        : (grad == null ? 'grad iz profila' : 'grad $grad');

    return Row(
      children: [
        Icon(
          lokacija.ukljucena ? Icons.my_location : Icons.location_city_outlined,
          size: 15,
          color: Boje.tekstPrigusen,
        ),
        const SizedBox(width: Razmaci.xs),
        Expanded(
          child: Text(
            'Mjereno od: $opis',
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontSize: 12, color: Boje.tekstPrigusen),
          ),
        ),
        if (lokacija.trazenje)
          const Padding(
            padding: EdgeInsets.symmetric(horizontal: Razmaci.m),
            child: SizedBox(
              width: 14,
              height: 14,
              child: CircularProgressIndicator(strokeWidth: 2),
            ),
          )
        else
          TextButton(
            onPressed: lokacija.ukljucena
                ? lokacija.iskljuci
                : () => _ukljuci(context),
            style: TextButton.styleFrom(
              visualDensity: VisualDensity.compact,
              textStyle: const TextStyle(fontSize: 12.5),
            ),
            child: Text(lokacija.ukljucena ? 'Koristi grad' : 'Moja lokacija'),
          ),
      ],
    );
  }
}
