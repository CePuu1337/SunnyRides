import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/placanje.dart';
import '../../servisi/placanje_servis.dart';
import '../../widgeti/potvrda.dart';

/// Ponovno slanje odbijenog povrata, uz potvrdu.
///
/// Vraća true kad je zahtjev otišao serveru, bez obzira na ishod. Lista se tada
/// osvježava i kad je server javio grešku: novi pokušaj se upisuje prije nego Stripe
/// odgovori, pa i neuspjeh mijenja ono što treba prikazati.
Future<bool> ponoviPovratUzPotvrdu(
  BuildContext context,
  PlacanjeServis servis,
  Placanje placanje,
  Povrat povrat,
) async {
  final potvrdjeno = await potvrdiRadnju(
    context,
    naslov: 'Ponoviti povrat?',
    tekst:
        'Klijentu ${placanje.klijentImePrezime} se ponovo šalje povrat od '
        '${Formati.novac(povrat.iznos)} za rezervaciju ${placanje.rezervacijaBroj}. '
        'Odbijeni povrat ostaje u historiji, a novi se šalje Stripe-u.',
    potvrda: 'Da, pošalji ponovo',
  );

  if (!potvrdjeno || !context.mounted) {
    return false;
  }

  final poruke = ScaffoldMessenger.of(context);

  try {
    await servis.ponoviPovrat(povrat.id);
    poruke.showSnackBar(
      const SnackBar(content: Text('Povrat je ponovo poslan Stripe-u.')),
    );
  } on ApiGreska catch (greska) {
    poruke.showSnackBar(SnackBar(content: Text(greska.poruka)));
  }

  return true;
}

/// Jedno plaćanje i njegovi povrati. Isti prikaz koriste ekran plaćanja i detalji
/// rezervacije, pa osoblje na oba mjesta vidi isto.
class PrikazPlacanja extends StatelessWidget {
  const PrikazPlacanja({
    super.key,
    required this.placanje,
    required this.naPonavljanje,
    this.saRezervacijom = true,
  });

  final Placanje placanje;
  final ValueChanged<Povrat> naPonavljanje;

  /// U detaljima rezervacije broj i klijent su već u zaglavlju, pa se ne ponavljaju.
  final bool saRezervacijom;

  @override
  Widget build(BuildContext context) {
    final naplaceno = placanje.naplaceniIznos;

    return Container(
      padding: const EdgeInsets.all(Razmaci.m),
      decoration: BoxDecoration(
        border: Border.all(
          color: placanje.imaNeuspjeliPovrat ? Boje.greskaPozadina : Boje.ivica,
        ),
        borderRadius: BorderRadius.circular(Zaobljenja.kartica),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      saRezervacijom
                          ? placanje.rezervacijaBroj
                          : 'Plaćanje od ${Formati.datumIVrijeme(placanje.datumKreiranja)}',
                      style: const TextStyle(fontWeight: FontWeight.w600),
                    ),
                    if (saRezervacijom) ...[
                      const SizedBox(height: 2),
                      Text(
                        [
                          placanje.klijentImePrezime,
                          if (placanje.poslovnicaNaziv.isNotEmpty)
                            placanje.poslovnicaNaziv,
                          Formati.datumIVrijeme(placanje.datumKreiranja),
                        ].join(' · '),
                        style: const TextStyle(
                          fontSize: 12,
                          color: Boje.tekstPrigusen,
                        ),
                      ),
                    ],
                  ],
                ),
              ),
              const SizedBox(width: Razmaci.s),
              StatusnaPilula.placanje(placanje.status),
            ],
          ),
          const SizedBox(height: Razmaci.s),
          // Iznosi se prelamaju u novi red kad je prostor uzak, npr. u desnoj koloni
          // detalja rezervacije.
          Wrap(
            spacing: Razmaci.xl,
            runSpacing: Razmaci.xs,
            children: [
              _Iznos(natpis: 'Iznos', vrijednost: placanje.iznos),
              _Iznos(natpis: 'Naplaćeno', vrijednost: naplaceno),
              _Iznos(natpis: 'Vraćeno', vrijednost: placanje.ukupnoVraceno),
            ],
          ),
          if (placanje.povrati.isNotEmpty) ...[
            const SizedBox(height: Razmaci.s),
            const Divider(height: 1),
            for (final povrat in placanje.povrati)
              _RedPovrata(
                povrat: povrat,
                naPonavljanje: () => naPonavljanje(povrat),
              ),
          ],
        ],
      ),
    );
  }
}

class _Iznos extends StatelessWidget {
  const _Iznos({required this.natpis, required this.vrijednost});

  final String natpis;
  final double? vrijednost;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          natpis,
          style: const TextStyle(fontSize: 11, color: Boje.tekstPrigusen),
        ),
        Text(
          vrijednost == null ? '—' : Formati.novac(vrijednost!),
          style: const TextStyle(fontWeight: FontWeight.w600),
        ),
      ],
    );
  }
}

class _RedPovrata extends StatelessWidget {
  const _RedPovrata({required this.povrat, required this.naPonavljanje});

  final Povrat povrat;
  final VoidCallback naPonavljanje;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: Razmaci.s),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Padding(
                padding: EdgeInsets.only(top: 2),
                child: Icon(Icons.undo, size: 16, color: Boje.tekstPrigusen),
              ),
              const SizedBox(width: Razmaci.s),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Povrat ${Formati.novac(povrat.iznos)}',
                      style: const TextStyle(
                        fontWeight: FontWeight.w600,
                        fontSize: 13,
                      ),
                    ),
                    Text(
                      povrat.razlog,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontSize: 12.5),
                    ),
                    Text(
                      [
                        Formati.datumIVrijeme(povrat.datumKreiranja),
                        povrat.kreiraoKorisnikIme ?? 'sistem',
                      ].join(' · '),
                      style: const TextStyle(
                        fontSize: 11.5,
                        color: Boje.tekstPrigusen,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: Razmaci.s),
              StatusnaPilula.placanje(povrat.status),
            ],
          ),
          if (povrat.mozeSePonoviti)
            Align(
              alignment: Alignment.centerRight,
              child: Padding(
                padding: const EdgeInsets.only(top: Razmaci.xs),
                child: OutlinedButton.icon(
                  onPressed: naPonavljanje,
                  icon: const Icon(Icons.refresh, size: 16),
                  label: const Text('Ponovi povrat'),
                ),
              ),
            ),
        ],
      ),
    );
  }
}
