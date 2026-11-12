import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/kalendar.dart';
import '../../servisi/rezervacija_servis.dart';
import '../../widgeti/obavjestenje.dart';
import '../rezervacije/otkazivanje_dijalog.dart';
import 'zamjena_vozila_dijalog.dart';

/// Kraj blokade kakav će stvarno biti upisan.
///
/// Kad blokada preklapa rezervaciju, a skraćivanje je uključeno, blokada se
/// zaustavlja na početku prve takve rezervacije. Vozilo je tada nedostupno sve do
/// termina koji je već obećan nekom drugom - a šta će sa tim terminom odlučuje
/// uposlenik telefonom, ne forma umjesto njega.
///
/// Isto pravilo važi pri unosu nove blokade i pri izmjeni postojeće.
DateTime krajBlokade({
  required DateTime doDatuma,
  required List<PogodjenaRezervacija> pogodjene,
  required bool skrati,
}) {
  if (!skrati || pogodjene.isEmpty) {
    return doDatuma;
  }

  final prva = pogodjene
      .map((x) => x.datumOd.toLocal())
      .reduce((a, b) => a.isBefore(b) ? a : b);

  return prva.isBefore(doDatuma) ? prva : doDatuma;
}

/// Pogođena rezervacija prelazi na drugo vozilo. Vraća true kad je zamjena urađena.
Future<bool> zamijeniVoziloPogodjene(
  BuildContext context,
  PogodjenaRezervacija rezervacija,
) async {
  final zamijenjeno = await showDialog<bool>(
    context: context,
    barrierDismissible: false,
    builder: (context) => ZamjenaVozilaDijalog(rezervacija: rezervacija),
  );

  if (zamijenjeno != true || !context.mounted) {
    return false;
  }

  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(
      content: Text(
        'Rezervacija ${rezervacija.broj} je prebačena na zamjensko vozilo.',
      ),
    ),
  );

  return true;
}

/// Otkazivanje od strane agencije, uz puni povrat. Ide kroz isti dijalog kao sa
/// ekrana rezervacija, pa uposlenik prije potvrde vidi tačan iznos povrata.
/// Vraća true kad je rezervacija otkazana; grešku sa servera baca dalje.
Future<bool> otkaziPogodjenu(
  BuildContext context,
  PogodjenaRezervacija pogodjena,
) async {
  final rezervacija = await RezervacijaServis(context.read<ApiKlijent>())
      .detalji(pogodjena.id);

  if (!context.mounted) {
    return false;
  }

  final otkazano = await showDialog<bool>(
    context: context,
    barrierDismissible: false,
    builder: (context) => OtkazivanjeDijalog(rezervacija: rezervacija),
  );

  if (otkazano != true || !context.mounted) {
    return false;
  }

  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(content: Text('Rezervacija ${pogodjena.broj} je otkazana.')),
  );

  return true;
}

/// Šta blokada pogađa: rezervacije na tom vozilu u tom periodu, sa izborom da se
/// blokada skrati, a svaka rezervacija prebaci na drugo vozilo ili otkaže.
class PogodjeneRezervacije extends StatelessWidget {
  const PogodjeneRezervacije({
    super.key,
    required this.provjeravam,
    required this.pogodjene,
    required this.skrati,
    required this.krajBlokade,
    required this.trazeniKraj,
    required this.naSkracivanje,
    required this.naZamjenu,
    required this.naOtkazivanje,
  });

  final bool provjeravam;
  final List<PogodjenaRezervacija> pogodjene;
  final bool skrati;

  /// Kraj koji će biti upisan, i kraj koji je uposlenik odabrao.
  final DateTime krajBlokade;
  final DateTime trazeniKraj;

  final ValueChanged<bool> naSkracivanje;
  final ValueChanged<PogodjenaRezervacija> naZamjenu;
  final ValueChanged<PogodjenaRezervacija> naOtkazivanje;

  @override
  Widget build(BuildContext context) {
    if (provjeravam) {
      return const Row(
        children: [
          SizedBox(
            width: 14,
            height: 14,
            child: CircularProgressIndicator(strokeWidth: 2),
          ),
          SizedBox(width: Razmaci.m),
          Text(
            'Provjeravam koje rezervacije ovo pogađa…',
            style: TextStyle(color: Boje.tekstPrigusen, fontSize: 12.5),
          ),
        ],
      );
    }

    if (pogodjene.isEmpty) {
      return Obavjestenje.uspjeh(
        'U tom periodu nema nijedne rezervacije na ovom vozilu.',
      );
    }

    final skraceno = krajBlokade != trazeniKraj;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Obavjestenje.upozorenje(
          'U tom periodu ${pogodjene.length == 1 ? 'postoji rezervacija' : 'postoje ${pogodjene.length} rezervacije'} '
          'na ovom vozilu. Blokada ih ne otkazuje sama: svaku možete prebaciti na '
          'zamjensko vozilo koje klijent smije voziti, ili otkazati uz puni povrat. '
          'Klijent o oba dobija obavještenje i email.',
        ),
        const SizedBox(height: Razmaci.m),
        CheckboxListTile(
          value: skrati,
          onChanged: (novo) => naSkracivanje(novo ?? false),
          contentPadding: EdgeInsets.zero,
          controlAffinity: ListTileControlAffinity.leading,
          title: const Text('Skrati blokadu do prve rezervacije'),
          subtitle: Text(
            skraceno
                ? 'Blokada će trajati do ${Formati.datumIVrijeme(krajBlokade)}, '
                      'kad počinje prva rezervacija.'
                : 'Blokada se završava prije prve rezervacije, pa nema šta da se skrati.',
            style: const TextStyle(fontSize: 12),
          ),
        ),
        const SizedBox(height: Razmaci.m),
        for (final rezervacija in pogodjene)
          PogodjenaKartica(
            rezervacija: rezervacija,
            naZamjenu: () => naZamjenu(rezervacija),
            naOtkazivanje: () => naOtkazivanje(rezervacija),
          ),
      ],
    );
  }
}

/// Polje za datum i vrijeme koje se bira dijalogom, ne kuca.
class PoljeTrenutka extends StatelessWidget {
  const PoljeTrenutka({
    super.key,
    required this.natpis,
    required this.vrijednost,
    required this.naPritisak,
  });

  final String natpis;
  final DateTime vrijednost;
  final VoidCallback naPritisak;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: naPritisak,
      borderRadius: BorderRadius.circular(Zaobljenja.polje),
      child: InputDecorator(
        decoration: InputDecoration(
          labelText: natpis,
          suffixIcon: const Icon(Icons.event_outlined, size: 18),
        ),
        child: Text(Formati.datumIVrijeme(vrijednost)),
      ),
    );
  }
}

/// Jedna pogođena rezervacija sa kontaktom klijenta i dvije moguće radnje.
class PogodjenaKartica extends StatelessWidget {
  const PogodjenaKartica({
    super.key,
    required this.rezervacija,
    required this.naZamjenu,
    required this.naOtkazivanje,
  });

  final PogodjenaRezervacija rezervacija;
  final VoidCallback naZamjenu;
  final VoidCallback naOtkazivanje;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: Razmaci.s),
      padding: const EdgeInsets.all(Razmaci.m),
      decoration: BoxDecoration(
        color: Boje.platno,
        borderRadius: BorderRadius.circular(Zaobljenja.dugme),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Text(
                          rezervacija.broj,
                          style: const TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        const SizedBox(width: Razmaci.s),
                        StatusnaPilula.rezervacija(rezervacija.status),
                      ],
                    ),
                    const SizedBox(height: 2),
                    Text(
                      '${Formati.datumIVrijeme(rezervacija.datumOd)} – '
                      '${Formati.datumIVrijeme(rezervacija.datumDo)}',
                      style: const TextStyle(
                        color: Boje.tekstPrigusen,
                        fontSize: 12,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: Razmaci.m),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text(
                    rezervacija.klijentImePrezime ?? '',
                    style: const TextStyle(fontSize: 12.5),
                  ),
                  Text(
                    rezervacija.klijentTelefon ??
                        rezervacija.klijentEmail ??
                        '',
                    style: const TextStyle(
                      color: Boje.tekstPrigusen,
                      fontSize: 12,
                    ),
                  ),
                ],
              ),
            ],
          ),
          const SizedBox(height: Razmaci.s),
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              TextButton.icon(
                onPressed: naOtkazivanje,
                icon: const Icon(Icons.cancel_outlined, size: 17),
                label: const Text('Otkaži uz puni povrat'),
              ),
              const SizedBox(width: Razmaci.s),
              OutlinedButton.icon(
                onPressed: naZamjenu,
                icon: const Icon(Icons.swap_horiz, size: 17),
                label: const Text('Zamijeni vozilo'),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
