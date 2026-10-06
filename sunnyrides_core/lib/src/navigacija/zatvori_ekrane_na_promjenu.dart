import 'package:flutter/widgets.dart';

/// Kad se promijeni [kljuc], zatvara sve sto je otvoreno preko pocetnog ekrana.
///
/// Korijen aplikacije bira izmedju prijave i glavnog dijela prema stanju sesije,
/// ali to mijenja samo pocetni ekran. Sve sto je otvoreno preko njega - ekran za
/// registraciju, dijalog, potvrda - ostaje na vrhu. Bez ovoga bi nakon registracije
/// korisnik i dalje gledao formu sa kruzicem, a nakon isteka sesije dijalog bi
/// visio preko ekrana za prijavu.
class ZatvoriEkraneNaPromjenu extends StatefulWidget {
  const ZatvoriEkraneNaPromjenu({
    super.key,
    required this.kljuc,
    required this.child,
  });

  final Object kljuc;
  final Widget child;

  @override
  State<ZatvoriEkraneNaPromjenu> createState() =>
      _ZatvoriEkraneNaPromjenuStanje();
}

class _ZatvoriEkraneNaPromjenuStanje extends State<ZatvoriEkraneNaPromjenu> {
  @override
  void didUpdateWidget(ZatvoriEkraneNaPromjenu stari) {
    super.didUpdateWidget(stari);

    if (stari.kljuc == widget.kljuc) {
      return;
    }

    // Navigator se ne smije mijenjati usred iscrtavanja, pa tek u sljedecem okviru.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) {
        return;
      }

      Navigator.of(context).popUntil((ruta) => ruta.isFirst);
    });
  }

  @override
  Widget build(BuildContext context) => widget.child;
}
