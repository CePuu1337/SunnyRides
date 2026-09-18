import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/pregled_poslovanja.dart';
import '../../servisi/pregled_servis.dart';
import '../../widgeti/kartica.dart';
import '../../widgeti/sadrzaj.dart';
import 'metrika_kartica.dart';
import 'presjek_trake.dart';

class PregledEkran extends StatefulWidget {
  const PregledEkran({super.key});

  @override
  State<PregledEkran> createState() => _PregledEkranStanje();
}

class _PregledEkranStanje extends State<PregledEkran> {
  late final PregledServis _servis;

  PregledPoslovanja? _pregled;
  bool _ucitavanje = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = PregledServis(context.read<ApiKlijent>());
    _ucitaj();
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      final pregled = await _servis.pregled();

      if (!mounted) {
        return;
      }

      setState(() {
        _pregled = pregled;
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Sadrzaj(
        ucitavanje: _ucitavanje,
        greska: _greska,
        naPonovniPokusaj: _ucitaj,
        dijete: _pregled == null
            ? const SizedBox.shrink()
            : _Prikaz(pregled: _pregled!, naOsvjezavanje: _ucitaj),
      ),
    );
  }
}

class _Prikaz extends StatelessWidget {
  const _Prikaz({required this.pregled, required this.naOsvjezavanje});

  final PregledPoslovanja pregled;
  final VoidCallback naOsvjezavanje;

  @override
  Widget build(BuildContext context) {
    final metrike = pregled.metrike;
    final poredbeniMjesec = Formati.mjesec(metrike.poredba.prethodniOd);

    return SingleChildScrollView(
      padding: const EdgeInsets.all(Razmaci.ekranMargina),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  'Stanje na dan ${Formati.datumIVrijeme(pregled.naDanUtc)}',
                  style: const TextStyle(
                    color: Boje.tekstPrigusen,
                    fontSize: 12.5,
                  ),
                ),
              ),
              OutlinedButton.icon(
                onPressed: naOsvjezavanje,
                icon: const Icon(Icons.refresh, size: 17),
                label: const Text('Osvježi'),
              ),
            ],
          ),
          const SizedBox(height: Razmaci.l),
          _Metrike(metrike: metrike, poredbeniMjesec: poredbeniMjesec),
          const SizedBox(height: Razmaci.l),
          _DonjiDio(pregled: pregled),
        ],
      ),
    );
  }
}

class _Metrike extends StatelessWidget {
  const _Metrike({required this.metrike, required this.poredbeniMjesec});

  final MetrikePoslovanja metrike;
  final String poredbeniMjesec;

  @override
  Widget build(BuildContext context) {
    final kartice = <Widget>[
      MetrikaKartica(
        naslov: 'VOZILA U NAJMU',
        vrijednost: '${metrike.vozilaUNajmu} / ${metrike.ukupnoAktivnihVozila}',
        pojasnjenje:
            'Trenutna iskorištenost ${Formati.postotak(metrike.trenutnaIskoristenost)}',
        ikona: Icons.two_wheeler_outlined,
        bojaIkone: Boje.primarnaTamnija,
      ),
      MetrikaKartica(
        naslov: 'AKTIVNE REZERVACIJE',
        vrijednost: Formati.broj(metrike.aktivneRezervacije),
        pojasnjenje:
            'Novih ovog mjeseca: ${Formati.broj(metrike.poredba.noveRezervacije.tekuce)}',
        ikona: Icons.receipt_long_outlined,
        bojaIkone: Boje.info,
        poredba: metrike.poredba.noveRezervacije,
        poredbeniMjesec: poredbeniMjesec,
      ),
      MetrikaKartica(
        naslov: 'NETO PRIHOD MJESECA',
        vrijednost: Formati.novac(metrike.netoPrihodTekucegMjeseca),
        pojasnjenje:
            'Naplaćeno ${Formati.novac(metrike.naplacenoTekucegMjeseca)}, vraćeno ${Formati.novac(metrike.refundiranoTekucegMjeseca)}',
        ikona: Icons.payments_outlined,
        bojaIkone: Boje.uspjeh,
        poredba: metrike.poredba.netoPrihod,
        poredbeniMjesec: poredbeniMjesec,
      ),
      MetrikaKartica(
        naslov: 'ČEKA OBRADU',
        vrijednost: Formati.broj(metrike.cekaObradu),
        pojasnjenje:
            '${metrike.neverifikovaneDozvole} dozvola za provjeru, ${metrike.neplaceneRezervacije} neplaćenih rezervacija',
        ikona: Icons.pending_actions_outlined,
        bojaIkone: Boje.upozorenje,
      ),
    ];

    return LayoutBuilder(
      builder: (context, ogranicenja) {
        // Cetiri kartice u redu dok ima mjesta, pa dvije, pa jedna. Prozor se moze
        // suziti na pola ekrana i kartice se tada ne stisnu u neciljive trake.
        final uRedu = ogranicenja.maxWidth > 1180
            ? 4
            : ogranicenja.maxWidth > 660
            ? 2
            : 1;

        final sirina = (ogranicenja.maxWidth - Razmaci.l * (uRedu - 1)) / uRedu;

        return Wrap(
          spacing: Razmaci.l,
          runSpacing: Razmaci.l,
          children: [
            for (final kartica in kartice)
              SizedBox(width: sirina, child: kartica),
          ],
        );
      },
    );
  }
}

class _DonjiDio extends StatelessWidget {
  const _DonjiDio({required this.pregled});

  final PregledPoslovanja pregled;

  @override
  Widget build(BuildContext context) {
    final raspored = Kartica(
      naslov: 'Preuzimanja i vraćanja danas',
      podnaslov: pregled.rasporedDanas.isEmpty
          ? 'Za danas nema zakazanih primopredaja'
          : '${pregled.rasporedDanas.length} stavki, poredanih po vremenu',
      bezUnutrasnjegRazmaka: true,
      dijete: pregled.rasporedDanas.isEmpty
          ? const PrazanPopis(
              poruka: 'Danas nema zakazanih preuzimanja ni vraćanja.',
              ikona: Icons.event_available_outlined,
            )
          : _TabelaRasporeda(stavke: pregled.rasporedDanas),
    );

    final presjeci = Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Kartica(
          naslov: 'Iskorištenost po tipu vozila',
          podnaslov: 'Koliko je vozila svakog tipa sada kod klijenata',
          dijete: pregled.poTipuVozila.isEmpty
              ? const PrazanPopis(poruka: 'Nema aktivnih vozila u floti.')
              : PresjekTrake(stavke: pregled.poTipuVozila),
        ),
        const SizedBox(height: Razmaci.l),
        Kartica(
          naslov: 'Vozila po poslovnici',
          podnaslov: 'Raspored flote i trenutna zauzetost',
          dijete: pregled.poPoslovnici.isEmpty
              ? const PrazanPopis(poruka: 'Nema poslovnica sa vozilima.')
              : PresjekTrake(stavke: pregled.poPoslovnici),
        ),
      ],
    );

    return LayoutBuilder(
      builder: (context, ogranicenja) {
        if (ogranicenja.maxWidth < 1000) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              raspored,
              const SizedBox(height: Razmaci.l),
              presjeci,
            ],
          );
        }

        return Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(flex: 3, child: raspored),
            const SizedBox(width: Razmaci.l),
            Expanded(flex: 2, child: presjeci),
          ],
        );
      },
    );
  }
}

class _TabelaRasporeda extends StatelessWidget {
  const _TabelaRasporeda({required this.stavke});

  final List<StavkaRasporeda> stavke;

  @override
  Widget build(BuildContext context) {
    // Tabela se rasteze do sirine kartice, a kad joj sadrzaj zatreba vise, klizi
    // vodoravno. Sirina se uzima od roditelja, ne od ekrana - kartica je uza od
    // prozora, pa bi joj sirina ekrana odsjekla zadnje kolone.
    return LayoutBuilder(
      builder: (context, ogranicenja) => SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: ConstrainedBox(
          constraints: BoxConstraints(minWidth: ogranicenja.maxWidth),
          child: DataTable(
            columnSpacing: Razmaci.l,
            horizontalMargin: Razmaci.karticaUnutra,
            headingRowHeight: 40,
            dataRowMinHeight: 48,
            dataRowMaxHeight: 56,
            columns: const [
              DataColumn(label: Text('VRIJEME')),
              DataColumn(label: Text('RADNJA')),
              DataColumn(label: Text('REZERVACIJA')),
              DataColumn(label: Text('VOZILO')),
              DataColumn(label: Text('KLIJENT')),
              DataColumn(label: Text('POSLOVNICA')),
              DataColumn(label: Text('STANJE')),
            ],
            rows: [
              for (final stavka in stavke)
                DataRow(
                  cells: [
                    DataCell(
                      Text(
                        Formati.vrijeme(stavka.vrijeme),
                        style: const TextStyle(fontWeight: FontWeight.w600),
                      ),
                    ),
                    DataCell(_Radnja(tip: stavka.tip)),
                    DataCell(Text(stavka.broj)),
                    DataCell(
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Text(stavka.vozilo),
                          Text(
                            stavka.registarskaOznaka,
                            style: const TextStyle(
                              color: Boje.tekstPrigusen,
                              fontSize: 11.5,
                            ),
                          ),
                        ],
                      ),
                    ),
                    DataCell(Text(stavka.klijent)),
                    DataCell(Text(stavka.poslovnica)),
                    DataCell(
                      stavka.evidentirano
                          ? const StatusnaPilula(
                              tekst: 'Evidentirano',
                              pozadina: Boje.uspjehPozadina,
                              bojaTeksta: Boje.uspjehTekst,
                              ikona: Icons.check,
                            )
                          : StatusnaPilula.rezervacija(stavka.status),
                    ),
                  ],
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _Radnja extends StatelessWidget {
  const _Radnja({required this.tip});

  final TipPrimopredaje? tip;

  @override
  Widget build(BuildContext context) {
    final izdavanje = tip == TipPrimopredaje.izdavanje;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(
          izdavanje ? Icons.north_east : Icons.south_west,
          size: 15,
          color: izdavanje ? Boje.info : Boje.uspjeh,
        ),
        const SizedBox(width: Razmaci.s),
        Text(izdavanje ? 'Preuzimanje' : 'Vraćanje'),
      ],
    );
  }
}
