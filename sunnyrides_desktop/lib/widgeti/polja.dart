import 'package:flutter/material.dart';

import '../modeli/stavka_sifrarnika.dart';

/// Polje za pretragu koje ne salje zahtjev na svako slovo.
///
/// Ceka da korisnik prestane kucati. Bez toga bi "Honda" poslala pet zahtjeva, a
/// odgovori bi se mogli vratiti izmijesanim redom i ostaviti pogresan rezultat.
class PoljePretrage extends StatefulWidget {
  const PoljePretrage({
    super.key,
    required this.naPromjenu,
    this.natpis = 'Pretraga',
    this.sirina = 260,
  });

  final ValueChanged<String> naPromjenu;
  final String natpis;
  final double sirina;

  @override
  State<PoljePretrage> createState() => _PoljePretrageStanje();
}

class _PoljePretrageStanje extends State<PoljePretrage> {
  final _kontroler = TextEditingController();
  int _zadnjaPromjena = 0;

  @override
  void dispose() {
    _kontroler.dispose();
    super.dispose();
  }

  Future<void> _odgodi(String tekst) async {
    final trenutak = DateTime.now().millisecondsSinceEpoch;
    _zadnjaPromjena = trenutak;

    await Future<void>.delayed(const Duration(milliseconds: 400));

    // Ako je u medjuvremenu stigao novi znak, ovaj poziv se odbacuje.
    if (!mounted || _zadnjaPromjena != trenutak) {
      return;
    }

    widget.naPromjenu(tekst.trim());
  }

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: widget.sirina,
      child: TextField(
        controller: _kontroler,
        onChanged: _odgodi,
        decoration: InputDecoration(
          hintText: widget.natpis,
          prefixIcon: const Icon(Icons.search, size: 19),
          suffixIcon: _kontroler.text.isEmpty
              ? null
              : IconButton(
                  tooltip: 'Očisti',
                  icon: const Icon(Icons.close, size: 17),
                  onPressed: () {
                    _kontroler.clear();
                    widget.naPromjenu('');
                    setState(() {});
                  },
                ),
        ),
      ),
    );
  }
}

/// Padajuca lista sifrarnika, sa stavkom koja znaci "bez filtera".
class PadajuciSifrarnik extends StatelessWidget {
  const PadajuciSifrarnik({
    super.key,
    required this.natpis,
    required this.stavke,
    required this.odabrano,
    required this.naPromjenu,
    this.svePoljeNatpis = 'Sve',
    this.sirina = 190,
  });

  final String natpis;
  final List<StavkaSifrarnika> stavke;
  final int? odabrano;
  final ValueChanged<int?> naPromjenu;
  final String svePoljeNatpis;
  final double sirina;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: sirina,
      child: DropdownButtonFormField<int?>(
        key: ValueKey(odabrano),
        initialValue: stavke.any((x) => x.id == odabrano) ? odabrano : null,
        isExpanded: true,
        decoration: InputDecoration(labelText: natpis),
        items: [
          DropdownMenuItem<int?>(value: null, child: Text(svePoljeNatpis)),
          for (final stavka in stavke)
            DropdownMenuItem<int?>(value: stavka.id, child: Text(stavka.naziv)),
        ],
        onChanged: naPromjenu,
      ),
    );
  }
}
