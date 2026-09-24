import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../servisi/rezervacija_servis.dart';
import '../../stanje/navigacija.dart';
import '../../widgeti/obavjestenje.dart';
import '../../widgeti/slika.dart';
import 'detalji_rezervacije_ekran.dart';

/// Rezervacije prijavljenog klijenta, razdvojene na aktivne i historiju.
class MojeRezervacijeEkran extends StatefulWidget {
  const MojeRezervacijeEkran({super.key});

  @override
  State<MojeRezervacijeEkran> createState() => _MojeRezervacijeEkranStanje();
}

class _MojeRezervacijeEkranStanje extends State<MojeRezervacijeEkran>
    with SingleTickerProviderStateMixin {
  late final TabController _kartice = TabController(length: 2, vsync: this);

  @override
  void dispose() {
    _kartice.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Boje.platno,
      appBar: AppBar(
        title: const Text('Moje rezervacije'),
        bottom: TabBar(
          controller: _kartice,
          tabs: const [
            Tab(text: 'Aktivne'),
            Tab(text: 'Historija'),
          ],
        ),
      ),
      body: TabBarView(
        controller: _kartice,
        children: const [_Popis(aktivne: true), _Popis(aktivne: false)],
      ),
    );
  }
}

class _Popis extends StatefulWidget {
  const _Popis({required this.aktivne});

  final bool aktivne;

  @override
  State<_Popis> createState() => _PopisStanje();
}

class _PopisStanje extends State<_Popis> {
  static const _velicinaStranice = 10;

  late final RezervacijaServis _servis;

  final _skrol = ScrollController();
  final List<Rezervacija> _stavke = [];

  int _stranica = 0;
  int? _ukupno;
  bool _ucitavanje = true;
  bool _dopunjavanje = false;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = RezervacijaServis(context.read<ApiKlijent>());
    _skrol.addListener(_naSkrol);
    _ucitaj();
  }

  @override
  void dispose() {
    _skrol.dispose();
    super.dispose();
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
      _stranica = 0;
    });

    try {
      final strana = await _servis.moje(
        stranica: 0,
        velicinaStranice: _velicinaStranice,
        aktivne: widget.aktivne,
      );

      if (!mounted) {
        return;
      }

      setState(() {
        _stavke
          ..clear()
          ..addAll(strana.stavke);
        _ukupno = strana.ukupno;
        _ucitavanje = false;
      });
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() {
        _greska = greska.poruka;
        _ucitavanje = false;
      });
    }
  }

  void _naSkrol() {
    if (_skrol.position.pixels < _skrol.position.maxScrollExtent - 200) {
      return;
    }

    _dopuni();
  }

  Future<void> _dopuni() async {
    final ukupno = _ukupno;

    if (_dopunjavanje || _ucitavanje) {
      return;
    }

    if (ukupno != null && _stavke.length >= ukupno) {
      return;
    }

    setState(() => _dopunjavanje = true);

    try {
      final strana = await _servis.moje(
        stranica: _stranica + 1,
        velicinaStranice: _velicinaStranice,
        aktivne: widget.aktivne,
      );

      if (!mounted) {
        return;
      }

      setState(() {
        _stranica += 1;
        _stavke.addAll(strana.stavke);
        _ukupno = strana.ukupno ?? _ukupno;
        _dopunjavanje = false;
      });
    } on ApiGreska {
      if (!mounted) {
        return;
      }

      setState(() => _dopunjavanje = false);
    }
  }

  Future<void> _otvori(Rezervacija rezervacija) async {
    await Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => DetaljiRezervacijeEkran(rezervacijaId: rezervacija.id),
      ),
    );

    if (!mounted) {
      return;
    }

    // Detalji su mogli otkazati ili platiti rezervaciju, pa se lista osvjezava.
    await _ucitaj();
  }

  @override
  Widget build(BuildContext context) {
    return Sadrzaj(
      ucitavanje: _ucitavanje,
      greska: _greska,
      naPonovniPokusaj: _ucitaj,
      dijete: _stavke.isEmpty
          ? PrazanPopis(
              poruka: widget.aktivne ? 'Nemate aktivnih rezervacija.' : 'Ovdje će stajati rezervacije koje su završene ili otkazane.',
              ikona: Icons.receipt_long_outlined,
              akcija: !widget.aktivne
                  ? null
                  : FilledButton(
                      onPressed: () =>
                          context.read<Navigacija>().otvoriPretragu(),
                      child: const Text('Pronađi vozilo'),
                    ),
            )
          : RefreshIndicator(
              onRefresh: _ucitaj,
              child: ListView.separated(
                controller: _skrol,
                padding: const EdgeInsets.all(Razmaci.l),
                itemCount: _stavke.length + (_dopunjavanje ? 1 : 0),
                separatorBuilder: (_, _) => const SizedBox(height: Razmaci.m),
                itemBuilder: (context, indeks) {
                  if (indeks >= _stavke.length) {
                    return const Padding(
                      padding: EdgeInsets.all(Razmaci.l),
                      child: Center(child: CircularProgressIndicator()),
                    );
                  }

                  final rezervacija = _stavke[indeks];

                  return _Kartica(
                    rezervacija: rezervacija,
                    naDodir: () => _otvori(rezervacija),
                  );
                },
              ),
            ),
    );
  }
}

class _Kartica extends StatelessWidget {
  const _Kartica({required this.rezervacija, required this.naDodir});

  final Rezervacija rezervacija;
  final VoidCallback naDodir;

  @override
  Widget build(BuildContext context) {
    final trebaPlatiti =
        !rezervacija.isPaid &&
        rezervacija.status == StatusRezervacije.naCekanju;

    return Material(
      color: Boje.povrsina,
      borderRadius: BorderRadius.circular(Zaobljenja.kartica),
      child: InkWell(
        onTap: naDodir,
        borderRadius: BorderRadius.circular(Zaobljenja.kartica),
        child: Container(
          padding: const EdgeInsets.all(Razmaci.m),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(Zaobljenja.kartica),
            border: Border.all(color: Boje.ivica),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Slika(
                    putanja: rezervacija.thumbnailUrl,
                    sirina: 72,
                    visina: 54,
                  ),
                  const SizedBox(width: Razmaci.m),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          rezervacija.vozilo.isEmpty
                              ? rezervacija.registarskaOznaka ?? '-'
                              : rezervacija.vozilo,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            fontSize: 14,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          rezervacija.broj,
                          style: const TextStyle(
                            fontSize: 11.5,
                            color: Boje.tekstPrigusen,
                          ),
                        ),
                      ],
                    ),
                  ),
                  StatusnaPilula.rezervacija(rezervacija.status),
                ],
              ),
              const SizedBox(height: Razmaci.m),
              Row(
                children: [
                  const Icon(
                    Icons.schedule,
                    size: 14,
                    color: Boje.tekstPrigusen,
                  ),
                  const SizedBox(width: Razmaci.xs),
                  Expanded(
                    child: Text(
                      '${Formati.datumIVrijeme(rezervacija.datumOd)} - '
                      '${Formati.datumIVrijeme(rezervacija.datumDo)}',
                      style: const TextStyle(
                        fontSize: 11.5,
                        color: Boje.tekstBlazi,
                      ),
                    ),
                  ),
                  Text(
                    Formati.novac(rezervacija.ukupanIznos),
                    style: const TextStyle(
                      fontSize: 13.5,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
              ),
              if (trebaPlatiti) ...[
                const SizedBox(height: Razmaci.m),
                Obavjestenje.upozorenje(
                  'Rezervacija čeka plaćanje. Vozilo se drži samo do isteka roka.',
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
