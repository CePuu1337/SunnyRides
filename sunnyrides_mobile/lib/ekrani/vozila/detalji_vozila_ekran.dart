import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/recenzija.dart';
import '../../modeli/vozilo.dart';
import '../../servisi/cijena_servis.dart';
import '../../servisi/katalog_servis.dart';
import '../../servisi/recenzija_servis.dart';
import '../../widgeti/kartica_vozila.dart';
import '../../widgeti/obavjestenje.dart';
import '../../widgeti/ocjena.dart';
import '../../widgeti/slika.dart';
import '../rezervacije/nova_rezervacija_ekran.dart';

/// Detalji jednog vozila: galerija, podaci, cijena, recenzije i slicna vozila.
class DetaljiVozilaEkran extends StatefulWidget {
  const DetaljiVozilaEkran({
    super.key,
    required this.voziloId,
    this.datumOd,
    this.datumDo,
  });

  final int voziloId;

  /// Termin iz pretrage, ako ga je korisnik tamo zadao. Prenosi se dalje u
  /// rezervaciju, da ga ne unosi dva puta.
  final DateTime? datumOd;
  final DateTime? datumDo;

  @override
  State<DetaljiVozilaEkran> createState() => _DetaljiVozilaEkranStanje();
}

class _DetaljiVozilaEkranStanje extends State<DetaljiVozilaEkran> {
  late final KatalogServis _katalog;
  late final CijenaServis _cijene;
  late final RecenzijaServis _recenzije;

  Vozilo? _vozilo;
  List<SlikaVozila> _slike = const [];
  List<Recenzija> _ocjene = const [];
  int _ukupnoRecenzija = 0;
  List<Preporuka> _slicna = const [];
  Cjenovnik? _cjenovnik;

  bool _ucitavanje = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    final klijent = context.read<ApiKlijent>();

    _katalog = KatalogServis(klijent);
    _cijene = CijenaServis(klijent);
    _recenzije = RecenzijaServis(klijent);

    _ucitaj();
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      // Vozilo prvo, jer cjenovnik trazi njegov model.
      final vozilo = await _katalog.vozilo(widget.voziloId);

      final ostalo = await Future.wait([
        _katalog.slike(widget.voziloId),
        _recenzije.zaVozilo(widget.voziloId, koliko: 5),
        _katalog.slicnaVozila(widget.voziloId),
        _cijene.vazeciCjenovnik(vozilo.modelVozilaId, datum: widget.datumOd),
      ]);

      if (!mounted) {
        return;
      }

      final strana = ostalo[1] as Strana<Recenzija>;

      setState(() {
        _vozilo = vozilo;
        _slike = ostalo[0] as List<SlikaVozila>;
        _ocjene = strana.stavke;
        _ukupnoRecenzija = strana.ukupno ?? strana.stavke.length;
        _slicna = ostalo[2] as List<Preporuka>;
        _cjenovnik = ostalo[3] as Cjenovnik?;
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

  void _rezervisi() {
    final vozilo = _vozilo;

    if (vozilo == null) {
      return;
    }

    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => NovaRezervacijaEkran(
          vozilo: vozilo,
          datumOd: widget.datumOd,
          datumDo: widget.datumDo,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final vozilo = _vozilo;

    return Scaffold(
      backgroundColor: Boje.platno,
      appBar: AppBar(title: Text(vozilo?.naziv ?? 'Vozilo')),
      body: Sadrzaj(
        ucitavanje: _ucitavanje,
        greska: _greska,
        naPonovniPokusaj: _ucitaj,
        dijete: vozilo == null
            ? const SizedBox.shrink()
            : ListView(
                padding: const EdgeInsets.only(bottom: Razmaci.xl),
                children: [
                  _Galerija(slike: _slike, zamjena: vozilo.thumbnailUrl),
                  Padding(
                    padding: const EdgeInsets.all(Razmaci.l),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          vozilo.naziv,
                          style: const TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        const SizedBox(height: Razmaci.xs),
                        Row(
                          children: [
                            if (vozilo.prosjecnaOcjena != null) ...[
                              Zvjezdice(
                                ocjena: vozilo.prosjecnaOcjena!,
                                velicina: 15,
                              ),
                              const SizedBox(width: Razmaci.xs),
                              Text(
                                '${vozilo.prosjecnaOcjena!.toStringAsFixed(1)} '
                                '(${vozilo.brojRecenzija})',
                                style: const TextStyle(
                                  fontSize: 12.5,
                                  color: Boje.tekstPrigusen,
                                ),
                              ),
                            ] else
                              const Text(
                                'Još nema ocjena',
                                style: TextStyle(
                                  fontSize: 12.5,
                                  color: Boje.tekstPrigusen,
                                ),
                              ),
                          ],
                        ),
                        const SizedBox(height: Razmaci.l),
                        _Podaci(vozilo: vozilo),
                        const SizedBox(height: Razmaci.l),
                        _Cijene(vozilo: vozilo, cjenovnik: _cjenovnik),
                        const SizedBox(height: Razmaci.l),
                        _Recenzije(stavke: _ocjene, ukupno: _ukupnoRecenzija),
                        if (_slicna.isNotEmpty) ...[
                          const SizedBox(height: Razmaci.xl),
                          const Text(
                            'Slična vozila',
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ],
                      ],
                    ),
                  ),
                  if (_slicna.isNotEmpty)
                    SizedBox(
                      height: 258,
                      child: ListView.separated(
                        scrollDirection: Axis.horizontal,
                        padding: const EdgeInsets.symmetric(
                          horizontal: Razmaci.l,
                        ),
                        itemCount: _slicna.length,
                        separatorBuilder: (_, _) =>
                            const SizedBox(width: Razmaci.m),
                        itemBuilder: (context, indeks) {
                          final stavka = _slicna[indeks];

                          return KarticaVozila(
                            vozilo: stavka.vozilo,
                            sirina: 220,
                            ocjena: stavka.vozilo.prosjecnaOcjena,
                            naDodir: () =>
                                Navigator.of(context).pushReplacement(
                                  MaterialPageRoute<void>(
                                    builder: (_) => DetaljiVozilaEkran(
                                      voziloId: stavka.vozilo.id,
                                      datumOd: widget.datumOd,
                                      datumDo: widget.datumDo,
                                    ),
                                  ),
                                ),
                          );
                        },
                      ),
                    ),
                ],
              ),
      ),
      bottomNavigationBar: vozilo == null || _ucitavanje
          ? null
          : _DonjaTraka(vozilo: vozilo, naRezervaciju: _rezervisi),
    );
  }
}

class _Galerija extends StatefulWidget {
  const _Galerija({required this.slike, this.zamjena});

  final List<SlikaVozila> slike;
  final String? zamjena;

  @override
  State<_Galerija> createState() => _GalerijaStanje();
}

class _GalerijaStanje extends State<_Galerija> {
  final _kontroler = PageController();
  int _aktivna = 0;

  @override
  void dispose() {
    _kontroler.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (widget.slike.isEmpty) {
      return Slika(putanja: widget.zamjena, visina: 220, zaobljenje: 0);
    }

    return SizedBox(
      height: 220,
      child: Stack(
        children: [
          PageView.builder(
            controller: _kontroler,
            itemCount: widget.slike.length,
            onPageChanged: (indeks) => setState(() => _aktivna = indeks),
            itemBuilder: (context, indeks) =>
                Slika(putanja: widget.slike[indeks].url, zaobljenje: 0),
          ),
          if (widget.slike.length > 1)
            Positioned(
              bottom: Razmaci.m,
              left: 0,
              right: 0,
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  for (var i = 0; i < widget.slike.length; i++)
                    Container(
                      width: i == _aktivna ? 18 : 7,
                      height: 7,
                      margin: const EdgeInsets.symmetric(horizontal: 3),
                      decoration: BoxDecoration(
                        color: i == _aktivna ? Boje.primarna : Colors.white70,
                        borderRadius: BorderRadius.circular(Zaobljenja.pilula),
                      ),
                    ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}

class _Podaci extends StatelessWidget {
  const _Podaci({required this.vozilo});

  final Vozilo vozilo;

  @override
  Widget build(BuildContext context) {
    final stavke = <(IconData, String, String)>[
      (Icons.category_outlined, 'Tip', vozilo.tipVozilaNaziv ?? '-'),
      (
        vozilo.jeElektricno ? Icons.bolt_outlined : Icons.speed_outlined,
        vozilo.jeElektricno ? 'Snaga' : 'Zapremina',
        vozilo.pogon,
      ),
      (
        vozilo.jeElektricno
            ? Icons.battery_charging_full_outlined
            : Icons.local_gas_station_outlined,
        'Pogon',
        vozilo.tipGorivaNaziv ?? '-',
      ),
      (Icons.event_outlined, 'Godište', '${vozilo.godinaProizvodnje}'),
      (
        Icons.badge_outlined,
        'Kategorija',
        vozilo.kategorijaDozvoleOznaka ?? '-',
      ),
      (
        Icons.place_outlined,
        'Preuzimanje',
        vozilo.lokacija.isEmpty ? '-' : vozilo.lokacija,
      ),
    ];

    return _Okvir(
      naslov: 'Podaci o vozilu',
      dijete: Column(
        children: [
          for (final stavka in stavke)
            Padding(
              padding: const EdgeInsets.only(bottom: Razmaci.m),
              child: Row(
                children: [
                  Icon(stavka.$1, size: 17, color: Boje.tekstPrigusen),
                  const SizedBox(width: Razmaci.s),
                  Expanded(
                    child: Text(
                      stavka.$2,
                      style: const TextStyle(
                        fontSize: 13,
                        color: Boje.tekstPrigusen,
                      ),
                    ),
                  ),
                  Text(
                    stavka.$3,
                    style: const TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}

class _Cijene extends StatelessWidget {
  const _Cijene({required this.vozilo, this.cjenovnik});

  final Vozilo vozilo;
  final Cjenovnik? cjenovnik;

  @override
  Widget build(BuildContext context) {
    final sezona = cjenovnik;

    return _Okvir(
      naslov: 'Cijena najma',
      dijete: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _Red(
            oznaka: 'Po satu',
            vrijednost: Formati.novac(vozilo.satnaTarifa),
          ),
          _Red(
            oznaka: 'Po danu',
            vrijednost: Formati.novac(vozilo.dnevnaTarifa),
          ),
          _Red(
            oznaka: 'Depozit',
            vrijednost: Formati.novac(vozilo.iznosDepozita),
            napomena: 'vraća se nakon povrata vozila',
          ),
          if (sezona != null && sezona.mnozilac != 1) ...[
            const SizedBox(height: Razmaci.s),
            Obavjestenje.info(
              'U periodu "${sezona.naziv}" cijena se množi sa '
              '${sezona.mnozilac.toStringAsFixed(2)}.',
            ),
          ],
          if (sezona != null && sezona.popustPrag1 > 0) ...[
            const SizedBox(height: Razmaci.m),
            const Text(
              'Popust na duži najam',
              style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: Razmaci.xs),
            Text(
              'Od ${sezona.popustPrag1} dana: '
              '-${Formati.postotak(sezona.popustProcenat1)}'
              '${sezona.popustPrag2 > 0 ? '\nOd ${sezona.popustPrag2} dana: '
                        '-${Formati.postotak(sezona.popustProcenat2)}' : ''}',
              style: const TextStyle(
                fontSize: 12.5,
                height: 1.5,
                color: Boje.tekstBlazi,
              ),
            ),
          ],
          const SizedBox(height: Razmaci.s),
          const Text(
            'Konačan iznos se računa na serveru kada odaberete termin, opremu i '
            'osiguranje.',
            style: TextStyle(
              fontSize: 11.5,
              color: Boje.tekstPrigusen,
              height: 1.4,
            ),
          ),
        ],
      ),
    );
  }
}

class _Red extends StatelessWidget {
  const _Red({required this.oznaka, required this.vrijednost, this.napomena});

  final String oznaka;
  final String vrijednost;
  final String? napomena;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: Razmaci.s),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(oznaka, style: const TextStyle(fontSize: 13)),
                if (napomena != null)
                  Text(
                    napomena!,
                    style: const TextStyle(
                      fontSize: 11,
                      color: Boje.tekstPrigusen,
                    ),
                  ),
              ],
            ),
          ),
          Text(
            vrijednost,
            style: const TextStyle(fontSize: 13.5, fontWeight: FontWeight.w700),
          ),
        ],
      ),
    );
  }
}

class _Recenzije extends StatelessWidget {
  const _Recenzije({required this.stavke, required this.ukupno});

  final List<Recenzija> stavke;
  final int ukupno;

  @override
  Widget build(BuildContext context) {
    return _Okvir(
      naslov: ukupno == 0 ? 'Recenzije' : 'Recenzije ($ukupno)',
      dijete: stavke.isEmpty
          ? const Text(
              'Ovo vozilo još nije ocijenjeno.',
              style: TextStyle(fontSize: 12.5, color: Boje.tekstPrigusen),
            )
          : Column(
              children: [
                for (final recenzija in stavke)
                  Padding(
                    padding: const EdgeInsets.only(bottom: Razmaci.m),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Zvjezdice(
                              ocjena: recenzija.ocjena.toDouble(),
                              velicina: 14,
                            ),
                            const SizedBox(width: Razmaci.s),
                            Expanded(
                              child: Text(
                                recenzija.korisnikImePrezime ?? 'Klijent',
                                style: const TextStyle(
                                  fontSize: 12.5,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ),
                            Text(
                              Formati.datum(recenzija.datumKreiranja),
                              style: const TextStyle(
                                fontSize: 11,
                                color: Boje.tekstPrigusen,
                              ),
                            ),
                          ],
                        ),
                        if (recenzija.komentar != null &&
                            recenzija.komentar!.isNotEmpty) ...[
                          const SizedBox(height: Razmaci.xs),
                          Text(
                            recenzija.komentar!,
                            style: const TextStyle(
                              fontSize: 12.5,
                              height: 1.45,
                              color: Boje.tekstBlazi,
                            ),
                          ),
                        ],
                      ],
                    ),
                  ),
              ],
            ),
    );
  }
}

class _Okvir extends StatelessWidget {
  const _Okvir({required this.naslov, required this.dijete});

  final String naslov;
  final Widget dijete;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(Razmaci.l),
      decoration: BoxDecoration(
        color: Boje.povrsina,
        borderRadius: BorderRadius.circular(Zaobljenja.kartica),
        border: Border.all(color: Boje.ivica),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            naslov,
            style: const TextStyle(fontSize: 14.5, fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: Razmaci.m),
          dijete,
        ],
      ),
    );
  }
}

class _DonjaTraka extends StatelessWidget {
  const _DonjaTraka({required this.vozilo, required this.naRezervaciju});

  final Vozilo vozilo;
  final VoidCallback naRezervaciju;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Container(
        padding: const EdgeInsets.fromLTRB(
          Razmaci.l,
          Razmaci.m,
          Razmaci.l,
          Razmaci.m,
        ),
        decoration: const BoxDecoration(
          color: Boje.povrsina,
          border: Border(top: BorderSide(color: Boje.ivica)),
        ),
        child: Row(
          children: [
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  Formati.novac(vozilo.dnevnaTarifa),
                  style: const TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const Text(
                  'po danu',
                  style: TextStyle(fontSize: 11.5, color: Boje.tekstPrigusen),
                ),
              ],
            ),
            const SizedBox(width: Razmaci.l),
            Expanded(
              child: FilledButton(
                onPressed: vozilo.aktivno ? naRezervaciju : null,
                child: Text(vozilo.aktivno ? 'Rezerviši' : 'Nije dostupno'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
