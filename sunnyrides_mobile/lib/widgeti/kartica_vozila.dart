import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../modeli/vozilo.dart';
import 'slika.dart';

/// Kartica vozila u listi ili u vodoravnom nizu preporuka.
///
/// Kad je zadato [obrazlozenje], ispod podataka stoji zuti okvir sa recenicom koju
/// je poslao server - korisnik tako vidi zasto mu je bas ovo vozilo predlozeno, a ne
/// samo da jeste.
class KarticaVozila extends StatelessWidget {
  const KarticaVozila({
    super.key,
    required this.vozilo,
    required this.naDodir,
    this.obrazlozenje,
    this.sirina,
    this.ocjena,
  });

  final Vozilo vozilo;
  final VoidCallback naDodir;
  final String? obrazlozenje;
  final double? sirina;
  final double? ocjena;

  @override
  Widget build(BuildContext context) {
    final kartica = Container(
      width: sirina,
      decoration: BoxDecoration(
        color: Boje.povrsina,
        borderRadius: BorderRadius.circular(Zaobljenja.kartica),
        border: Border.all(color: Boje.ivica),
      ),
      clipBehavior: Clip.antiAlias,
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: naDodir,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Slika(putanja: vozilo.thumbnailUrl, visina: 124, zaobljenje: 0),
              Padding(
                padding: const EdgeInsets.all(Razmaci.m),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      vozilo.naziv,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        fontSize: 14.5,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      [
                        vozilo.tipVozilaNaziv,
                        vozilo.pogon,
                      ].where((x) => x != null && x.isNotEmpty).join(' · '),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        fontSize: 12,
                        color: Boje.tekstPrigusen,
                      ),
                    ),
                    const SizedBox(height: Razmaci.s),
                    Row(
                      children: [
                        Expanded(
                          child: RichText(
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            text: TextSpan(
                              style: const TextStyle(color: Boje.tekst),
                              children: [
                                TextSpan(
                                  text: Formati.novac(vozilo.dnevnaTarifa),
                                  style: const TextStyle(
                                    fontSize: 14.5,
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                                const TextSpan(
                                  text: ' / dan',
                                  style: TextStyle(
                                    fontSize: 11.5,
                                    color: Boje.tekstPrigusen,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                        if (ocjena != null) ...[
                          const Icon(
                            Icons.star_rounded,
                            size: 15,
                            color: Boje.primarnaTamnija,
                          ),
                          const SizedBox(width: 2),
                          Text(
                            ocjena!.toStringAsFixed(1),
                            style: const TextStyle(
                              fontSize: 12.5,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ],
                    ),
                    if (vozilo.lokacija.isNotEmpty) ...[
                      const SizedBox(height: Razmaci.xs),
                      Row(
                        children: [
                          const Icon(
                            Icons.place_outlined,
                            size: 13,
                            color: Boje.tekstPrigusen,
                          ),
                          const SizedBox(width: 3),
                          Expanded(
                            child: Text(
                              vozilo.lokacija,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: const TextStyle(
                                fontSize: 11.5,
                                color: Boje.tekstPrigusen,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ],
                    if (obrazlozenje != null && obrazlozenje!.isNotEmpty) ...[
                      const SizedBox(height: Razmaci.m),
                      Container(
                        width: double.infinity,
                        padding: const EdgeInsets.all(Razmaci.s),
                        decoration: BoxDecoration(
                          color: Boje.primarnaSvijetla,
                          borderRadius: BorderRadius.circular(Zaobljenja.dugme),
                        ),
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Icon(
                              Icons.auto_awesome_outlined,
                              size: 13,
                              color: Boje.upozorenjeTekst,
                            ),
                            const SizedBox(width: Razmaci.xs),
                            Expanded(
                              child: Text(
                                obrazlozenje!,
                                style: const TextStyle(
                                  fontSize: 11,
                                  height: 1.35,
                                  color: Boje.upozorenjeTekst,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );

    return kartica;
  }
}
