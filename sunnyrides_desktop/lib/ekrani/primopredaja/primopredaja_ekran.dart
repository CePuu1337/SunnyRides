import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/primopredaja.dart';
import '../../modeli/stavka_sifrarnika.dart';
import '../../servisi/primopredaja_servis.dart';
import '../../servisi/sifrarnik_servis.dart';
import '../../widgeti/kartica.dart';
import '../../widgeti/polja.dart';
import '../../widgeti/sadrzaj.dart';
import '../rezervacije/rezervacija_detalji.dart';
import 'izdavanje_dijalog.dart';
import 'povrat_dijalog.dart';

/// Radni dan saltera: ko danas dolazi po vozilo i ko ga vraca.
class PrimopredajaEkran extends StatefulWidget {
  const PrimopredajaEkran({super.key});

  @override
  State<PrimopredajaEkran> createState() => _PrimopredajaEkranStanje();
}

class _PrimopredajaEkranStanje extends State<PrimopredajaEkran> {
  late final PrimopredajaServis _servis;
  late final SifrarnikServis _sifrarnici;

  late DateTime _dan;
  int? _poslovnicaId;

  List<RasporedStavka> _stavke = const [];
  List<StavkaSifrarnika> _poslovnice = const [];

  bool _ucitavanje = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    final klijent = context.read<ApiKlijent>();
    _servis = PrimopredajaServis(klijent);
    _sifrarnici = SifrarnikServis(klijent);

    final sada = DateTime.now();
    _dan = DateTime(sada.year, sada.month, sada.day);

    _ucitajPoslovnice();
    _ucitaj();
  }

  Future<void> _ucitajPoslovnice() async {
    try {
      final poslovnice = await _sifrarnici.ucitaj(SifrarnikServis.poslovnice);

      if (mounted) {
        setState(() => _poslovnice = poslovnice);
      }
    } on ApiGreska {
      // Filter ostaje prazan; raspored se i bez njega prikazuje.
    }
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      final strana = await _servis.raspored(
        od: _dan,
        doDatuma: _dan.add(const Duration(days: 1)),
        poslovnicaId: _poslovnicaId,
      );

      if (!mounted) {
        return;
      }

      setState(() {
        _stavke = strana.stavke;
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

  void _pomjeri(int dana) {
    setState(() => _dan = _dan.add(Duration(days: dana)));
    _ucitaj();
  }

  Future<void> _odaberiDan() async {
    final datum = await showDatePicker(
      context: context,
      initialDate: _dan,
      firstDate: DateTime(2020),
      lastDate: DateTime(DateTime.now().year + 2, 12, 31),
    );

    if (datum == null) {
      return;
    }

    setState(() => _dan = DateTime(datum.year, datum.month, datum.day));
    _ucitaj();
  }

  Future<void> _obradi(RasporedStavka stavka) async {
    // Vracanje bez evidentiranog izdavanja prvo treba izdavanje. Uposlenik tako iz
    // istog reda rijesi ono sto je propusteno, umjesto da trazi gdje da to upise.
    final jeIzdavanje =
        stavka.akcija == TipPrimopredaje.izdavanje || stavka.trebaIzdavanje;

    final gotovo = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (context) => jeIzdavanje
          ? IzdavanjeDijalog(stavka: stavka)
          : PovratDijalog(stavka: stavka),
    );

    if (gotovo == true) {
      _ucitaj();

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              jeIzdavanje
                  ? 'Izdavanje je evidentirano. Sada možete zaprimiti vozilo.'
                  : 'Povrat je evidentiran, a depozit obračunat.',
            ),
          ),
        );
      }
    }
  }

  Future<void> _otvoriRezervaciju(int rezervacijaId) async {
    await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (context) => RezervacijaDetalji(rezervacijaId: rezervacijaId),
      ),
    );

    _ucitaj();
  }

  @override
  Widget build(BuildContext context) {
    final preuzimanja = _stavke
        .where((x) => x.akcija == TipPrimopredaje.izdavanje)
        .toList();
    final vracanja = _stavke
        .where((x) => x.akcija == TipPrimopredaje.povrat)
        .toList();

    return Scaffold(
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(Razmaci.ekranMargina),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _Traka(
              dan: _dan,
              poslovnice: _poslovnice,
              poslovnicaId: _poslovnicaId,
              naPomjeranje: _pomjeri,
              naOdabirDana: _odaberiDan,
              naPoslovnicu: (id) {
                setState(() => _poslovnicaId = id);
                _ucitaj();
              },
            ),
            const SizedBox(height: Razmaci.l),
            SizedBox(
              height: 520,
              child: Sadrzaj(
                ucitavanje: _ucitavanje,
                greska: _greska,
                naPonovniPokusaj: _ucitaj,
                dijete: LayoutBuilder(
                  builder: (context, ogranicenja) {
                    final preuzimanjaKartica = Kartica(
                      naslov: 'Preuzimanja',
                      podnaslov: '${preuzimanja.length} zakazanih za ovaj dan',
                      bezUnutrasnjegRazmaka: true,
                      dijete: _Lista(
                        stavke: preuzimanja,
                        prazno: 'Nema zakazanih preuzimanja.',
                        natpisAkcije: 'Izdaj vozilo',
                        naAkciju: _obradi,
                        naDetalje: _otvoriRezervaciju,
                      ),
                    );

                    final vracanjaKartica = Kartica(
                      naslov: 'Vraćanja',
                      podnaslov: '${vracanja.length} zakazanih za ovaj dan',
                      bezUnutrasnjegRazmaka: true,
                      dijete: _Lista(
                        stavke: vracanja,
                        prazno: 'Nema zakazanih vraćanja.',
                        natpisAkcije: 'Zaprimi vozilo',
                        naAkciju: _obradi,
                        naDetalje: _otvoriRezervaciju,
                      ),
                    );

                    if (ogranicenja.maxWidth < 1000) {
                      return SingleChildScrollView(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            preuzimanjaKartica,
                            const SizedBox(height: Razmaci.l),
                            vracanjaKartica,
                          ],
                        ),
                      );
                    }

                    return Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(child: preuzimanjaKartica),
                        const SizedBox(width: Razmaci.l),
                        Expanded(child: vracanjaKartica),
                      ],
                    );
                  },
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Traka extends StatelessWidget {
  const _Traka({
    required this.dan,
    required this.poslovnice,
    required this.poslovnicaId,
    required this.naPomjeranje,
    required this.naOdabirDana,
    required this.naPoslovnicu,
  });

  final DateTime dan;
  final List<StavkaSifrarnika> poslovnice;
  final int? poslovnicaId;
  final ValueChanged<int> naPomjeranje;
  final VoidCallback naOdabirDana;
  final ValueChanged<int?> naPoslovnicu;

  @override
  Widget build(BuildContext context) {
    final danas = DateTime.now();
    final jeDanas =
        dan.year == danas.year &&
        dan.month == danas.month &&
        dan.day == danas.day;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(Razmaci.m),
        child: Row(
          children: [
            IconButton(
              tooltip: 'Prethodni dan',
              onPressed: () => naPomjeranje(-1),
              icon: const Icon(Icons.chevron_left),
            ),
            TextButton.icon(
              onPressed: naOdabirDana,
              icon: const Icon(Icons.event_outlined, size: 18),
              label: Text(
                jeDanas ? 'Danas, ${Formati.datum(dan)}' : Formati.datum(dan),
                style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
            IconButton(
              tooltip: 'Sljedeći dan',
              onPressed: () => naPomjeranje(1),
              icon: const Icon(Icons.chevron_right),
            ),
            const SizedBox(width: Razmaci.l),
            PadajuciSifrarnik(
              natpis: 'Poslovnica',
              stavke: poslovnice,
              odabrano: poslovnicaId,
              naPromjenu: naPoslovnicu,
            ),
            const Spacer(),
          ],
        ),
      ),
    );
  }
}

class _Lista extends StatelessWidget {
  const _Lista({
    required this.stavke,
    required this.prazno,
    required this.natpisAkcije,
    required this.naAkciju,
    required this.naDetalje,
  });

  final List<RasporedStavka> stavke;
  final String prazno;
  final String natpisAkcije;
  final ValueChanged<RasporedStavka> naAkciju;
  final ValueChanged<int> naDetalje;

  @override
  Widget build(BuildContext context) {
    if (stavke.isEmpty) {
      return PrazanPopis(poruka: prazno, ikona: Icons.event_available_outlined);
    }

    return SizedBox(
      height: 420,
      child: ListView.separated(
        itemCount: stavke.length,
        separatorBuilder: (context, indeks) => const Divider(height: 1),
        itemBuilder: (context, indeks) {
          final stavka = stavke[indeks];

          return _Red(
            stavka: stavka,
            natpisAkcije: natpisAkcije,
            naAkciju: () => naAkciju(stavka),
            naDetalje: () => naDetalje(stavka.rezervacijaId),
          );
        },
      ),
    );
  }
}

class _Red extends StatelessWidget {
  const _Red({
    required this.stavka,
    required this.natpisAkcije,
    required this.naAkciju,
    required this.naDetalje,
  });

  final RasporedStavka stavka;
  final String natpisAkcije;
  final VoidCallback naAkciju;
  final VoidCallback naDetalje;

  @override
  Widget build(BuildContext context) {
    // Otkazana rezervacija ostaje u rasporedu dok se ne prebaci dan, ali se po njoj
    // nema sta raditi.
    final otkazana = stavka.statusRezervacije == StatusRezervacije.otkazana;

    return Padding(
      padding: const EdgeInsets.symmetric(
        horizontal: Razmaci.karticaUnutra,
        vertical: Razmaci.m,
      ),
      child: Row(
        children: [
          SizedBox(
            width: 52,
            child: Text(
              Formati.vrijeme(stavka.vrijeme),
              style: const TextStyle(
                fontWeight: FontWeight.w700,
                fontSize: 13.5,
              ),
            ),
          ),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Flexible(
                      child: Text(
                        stavka.voziloNaziv ?? '',
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          fontSize: 13.5,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                    const SizedBox(width: Razmaci.s),
                    Text(
                      stavka.registarskaOznaka ?? '',
                      style: const TextStyle(
                        color: Boje.tekstPrigusen,
                        fontSize: 12,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 2),
                Text(
                  '${stavka.klijentImePrezime ?? ''} · ${stavka.broj}',
                  style: const TextStyle(
                    color: Boje.tekstPrigusen,
                    fontSize: 12,
                  ),
                ),
              ],
            ),
          ),
          if (stavka.obavljeno)
            const StatusnaPilula(
              tekst: 'Evidentirano',
              pozadina: Boje.uspjehPozadina,
              bojaTeksta: Boje.uspjehTekst,
              ikona: Icons.check,
            )
          else if (otkazana)
            StatusnaPilula.rezervacija(stavka.statusRezervacije)
          else if (stavka.trebaIzdavanje)
            OutlinedButton.icon(
              onPressed: naAkciju,
              icon: const Icon(Icons.assignment_late_outlined, size: 17),
              label: const Text('Evidentiraj izdavanje'),
            )
          else
            ElevatedButton(onPressed: naAkciju, child: Text(natpisAkcije)),
          IconButton(
            tooltip: 'Detalji rezervacije',
            onPressed: naDetalje,
            icon: const Icon(Icons.open_in_new, size: 17),
          ),
        ],
      ),
    );
  }
}
