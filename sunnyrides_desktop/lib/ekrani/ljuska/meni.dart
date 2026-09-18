import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../widgeti/u_izradi.dart';
import '../pregled/pregled_ekran.dart';
import '../rezervacije/rezervacije_ekran.dart';
import '../vozila/vozila_ekran.dart';

/// Jedna stavka bocne trake.
class StavkaMenija {
  const StavkaMenija({
    required this.id,
    required this.naziv,
    required this.podnaslov,
    required this.ikona,
    required this.gradi,
    this.samoAdministrator = false,
  });

  final String id;

  /// Natpis u meniju i naslov ekrana.
  final String naziv;

  /// Siva linija ispod naslova - kaze cemu ekran sluzi.
  final String podnaslov;

  final IconData ikona;
  final WidgetBuilder gradi;

  /// Stavke koje uposlenik ne vidi. Ne oslanja se samo na ovo - server svaku od
  /// tih ruta ionako odbija bez administratorske uloge. Ovdje je da uposlenik ne
  /// gleda dugmad koja mu ne rade.
  final bool samoAdministrator;
}

class GrupaMenija {
  const GrupaMenija({this.naslov, required this.stavke});

  /// Naslov grupe. Prva grupa ga nema - pocetni ekran stoji sam, iznad podjele.
  final String? naslov;

  final List<StavkaMenija> stavke;
}

class Meni {
  const Meni._();

  /// Meni prilagodjen ulozi. Uposlenik ne vidi administratorske stavke.
  static List<GrupaMenija> zaKorisnika(Korisnik korisnik) {
    final grupe = <GrupaMenija>[];

    for (final grupa in _sveGrupe) {
      final vidljive = grupa.stavke
          .where((x) => !x.samoAdministrator || korisnik.jeAdministrator)
          .toList();

      if (vidljive.isNotEmpty) {
        grupe.add(GrupaMenija(naslov: grupa.naslov, stavke: vidljive));
      }
    }

    return grupe;
  }

  static StavkaMenija prva(Korisnik korisnik) =>
      zaKorisnika(korisnik).first.stavke.first;

  static const _sveGrupe = <GrupaMenija>[
    GrupaMenija(
      stavke: [
        StavkaMenija(
          id: 'pregled',
          naziv: 'Pregled poslovanja',
          podnaslov: 'Stanje flote i posla za danas',
          ikona: Icons.dashboard_outlined,
          gradi: _pregled,
        ),
      ],
    ),
    GrupaMenija(
      naslov: 'FLOTA',
      stavke: [
        StavkaMenija(
          id: 'vozila',
          naziv: 'Vozila',
          podnaslov: 'Skuteri, motocikli i kvadovi u floti',
          ikona: Icons.two_wheeler_outlined,
          gradi: _vozila,
        ),
        StavkaMenija(
          id: 'kalendar',
          naziv: 'Kalendar flote',
          podnaslov: 'Zauzeće vozila po danima',
          ikona: Icons.calendar_month_outlined,
          gradi: _uIzradi,
        ),
        StavkaMenija(
          id: 'rezervacije',
          naziv: 'Rezervacije',
          podnaslov: 'Sve rezervacije i njihov status',
          ikona: Icons.receipt_long_outlined,
          gradi: _rezervacije,
        ),
        StavkaMenija(
          id: 'primopredaja',
          naziv: 'Primopredaja',
          podnaslov: 'Izdavanje i povrat vozila',
          ikona: Icons.compare_arrows_outlined,
          gradi: _uIzradi,
        ),
      ],
    ),
    GrupaMenija(
      naslov: 'KORISNICI',
      stavke: [
        StavkaMenija(
          id: 'korisnici',
          naziv: 'Korisnici',
          podnaslov: 'Nalozi klijenata i osoblja',
          ikona: Icons.people_outline,
          gradi: _uIzradi,
          samoAdministrator: true,
        ),
        StavkaMenija(
          id: 'dozvole',
          naziv: 'Vozačke dozvole',
          podnaslov: 'Verifikacija predanih dozvola',
          ikona: Icons.badge_outlined,
          gradi: _uIzradi,
        ),
        StavkaMenija(
          id: 'recenzije',
          naziv: 'Recenzije',
          podnaslov: 'Ocjene klijenata i moderacija',
          ikona: Icons.star_outline,
          gradi: _uIzradi,
        ),
      ],
    ),
    GrupaMenija(
      naslov: 'SISTEM',
      stavke: [
        StavkaMenija(
          id: 'obavijesti',
          naziv: 'Obavijesti',
          podnaslov: 'Objave koje klijenti vide u aplikaciji',
          ikona: Icons.campaign_outlined,
          gradi: _uIzradi,
          samoAdministrator: true,
        ),
        StavkaMenija(
          id: 'cjenovnik',
          naziv: 'Cjenovnik',
          podnaslov: 'Tarife, popusti i paketi osiguranja',
          ikona: Icons.sell_outlined,
          gradi: _uIzradi,
          samoAdministrator: true,
        ),
        StavkaMenija(
          id: 'sifarnici',
          naziv: 'Šifarnici',
          podnaslov: 'Gradovi, marke, tipovi i poslovnice',
          ikona: Icons.list_alt_outlined,
          gradi: _uIzradi,
          samoAdministrator: true,
        ),
        StavkaMenija(
          id: 'izvjestaji',
          naziv: 'Izvještaji',
          podnaslov: 'Iskorištenost flote i finansijski pregled',
          ikona: Icons.insert_chart_outlined,
          gradi: _uIzradi,
        ),
      ],
    ),
  ];
}

Widget _uIzradi(BuildContext context) => const UIzradi();

Widget _pregled(BuildContext context) => const PregledEkran();

Widget _vozila(BuildContext context) => const VozilaEkran();

Widget _rezervacije(BuildContext context) => const RezervacijeEkran();
