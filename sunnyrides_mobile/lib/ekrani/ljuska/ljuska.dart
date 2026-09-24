import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../stanje/navigacija.dart';
import '../pocetna/pocetna_ekran.dart';
import '../pretraga/pretraga_ekran.dart';
import '../profil/profil_ekran.dart';
import '../rezervacije/moje_rezervacije_ekran.dart';

/// Okvir mobilne aplikacije: cetiri kartice u donjoj traci.
///
/// Svaka kartica ima svoj Navigator, pa detalji otvoreni iz pretrage ostaju u
/// pretrazi - povratak na karticu vraca korisnika tamo gdje je stao, umjesto na
/// pocetak.
class Ljuska extends StatefulWidget {
  const Ljuska({super.key});

  @override
  State<Ljuska> createState() => _LjuskaStanje();
}

class _LjuskaStanje extends State<Ljuska> {
  static const _brojKartica = 4;

  final _kljucevi = List.generate(
    _brojKartica,
    (_) => GlobalKey<NavigatorState>(),
  );

  late final NotifikacijeStanje _notifikacije;
  late final Navigacija _navigacija;

  int _aktivna = 0;

  @override
  void initState() {
    super.initState();

    _notifikacije = NotifikacijeStanje(
      klijent: context.read<ApiKlijent>(),
      okruzenje: context.read<Okruzenje>(),
      pohrana: context.read<PohranaTokena>(),
    );
    _notifikacije.pokreni();

    _navigacija = Navigacija()..addListener(_naZahtjevZaKarticom);
  }

  @override
  void dispose() {
    _navigacija.removeListener(_naZahtjevZaKarticom);
    _navigacija.dispose();
    _notifikacije.dispose();
    super.dispose();
  }

  void _naZahtjevZaKarticom() {
    if (_navigacija.kartica == _aktivna) {
      return;
    }

    setState(() => _aktivna = _navigacija.kartica);
  }

  void _odaberi(int indeks) {
    if (indeks == _aktivna) {
      // Ponovni dodir iste kartice vraca na njen pocetni ekran - uobicajeno
      // ponasanje, i jedini nacin da se korisnik vrati iz dubine bez tipke nazad.
      _kljucevi[indeks].currentState?.popUntil((ruta) => ruta.isFirst);

      return;
    }

    _navigacija.otvoriKarticu(indeks);
    setState(() => _aktivna = indeks);
  }

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<NotifikacijeStanje>.value(value: _notifikacije),
        ChangeNotifierProvider<Navigacija>.value(value: _navigacija),
      ],
      child: PopScope(
        canPop: false,
        onPopInvokedWithResult: (jePopnuto, rezultat) {
          if (jePopnuto) {
            return;
          }

          final navigator = _kljucevi[_aktivna].currentState;

          if (navigator != null && navigator.canPop()) {
            navigator.pop();
          } else if (_aktivna != 0) {
            _odaberi(0);
          }
        },
        child: Scaffold(
          body: IndexedStack(
            index: _aktivna,
            children: [
              _kartica(0, const PocetnaEkran()),
              _kartica(1, const PretragaEkran()),
              _kartica(2, const MojeRezervacijeEkran()),
              _kartica(3, const ProfilEkran()),
            ],
          ),
          bottomNavigationBar: NavigationBar(
            selectedIndex: _aktivna,
            onDestinationSelected: _odaberi,
            height: 68,
            labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
            destinations: const [
              NavigationDestination(
                icon: Icon(Icons.home_outlined),
                selectedIcon: Icon(Icons.home),
                label: 'Početna',
              ),
              NavigationDestination(
                icon: Icon(Icons.search),
                selectedIcon: Icon(Icons.search),
                label: 'Pretraga',
              ),
              NavigationDestination(
                icon: Icon(Icons.receipt_long_outlined),
                selectedIcon: Icon(Icons.receipt_long),
                label: 'Rezervacije',
              ),
              NavigationDestination(
                icon: Icon(Icons.person_outline),
                selectedIcon: Icon(Icons.person),
                label: 'Profil',
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _kartica(int indeks, Widget pocetni) {
    return Navigator(
      key: _kljucevi[indeks],
      onGenerateRoute: (postavke) => MaterialPageRoute<void>(
        settings: postavke,
        builder: (context) => pocetni,
      ),
    );
  }
}
