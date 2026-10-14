import 'dart:async';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/tacka.dart';
import '../../modeli/vozilo.dart';
import '../../servisi/katalog_servis.dart';
import '../../stanje/lokacija.dart';
import '../../stanje/navigacija.dart';
import '../../stanje/sesija.dart';
import '../../widgeti/kartica_vozila.dart';
import '../../widgeti/obavjestenje.dart';
import '../../widgeti/polaziste_udaljenosti.dart';
import '../../widgeti/slika.dart';
import '../../widgeti/zvono.dart';
import '../vozila/detalji_vozila_ekran.dart';

/// Prvi ekran nakon prijave: pozdrav, brza pretraga, preporuke i objave agencije.
class PocetnaEkran extends StatefulWidget {
  const PocetnaEkran({super.key});

  @override
  State<PocetnaEkran> createState() => _PocetnaEkranStanje();
}

class _PocetnaEkranStanje extends State<PocetnaEkran> {
  late final KatalogServis _katalog;
  late final Lokacija _lokacija;
  late final Sesija _sesija;
  Tacka? _zadnjaTacka;
  int? _zadnjiGrad;

  /// Redni broj zadnjeg upita za najblizu poslovnicu. Odgovor na stariji upit
  /// stize ponekad poslije novijeg - na primjer kad klijent brzo prebaci sa
  /// lokacije na grad - i tada se odbacuje, da ne prepise noviji.
  int _upitNajblize = 0;

  List<Preporuka> _preporuke = const [];
  NajblizaPoslovnica? _najbliza;
  List<Obavijest> _obavijesti = const [];
  List<Stavka> _tipovi = const [];
  DozvoljeneKategorije? _kategorije;
  StreamSubscription<TipNotifikacije?>? _odlukaODozvoli;

  bool _ucitavanje = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _katalog = KatalogServis(context.read<ApiKlijent>());
    _lokacija = context.read<Lokacija>()..addListener(_naPromjenuPolazista);
    _sesija = context.read<Sesija>()..addListener(_naPromjenuPolazista);
    _zadnjiGrad = _sesija.korisnik?.gradId;

    // Kad uposlenik odluci o dozvoli, mijenja se sta klijent smije rezervisati.
    // Ekran se tada sam osvjezi, bez odjave i ponovne prijave.
    _odlukaODozvoli = context.read<NotifikacijeStanje>().odlukaODozvoli.listen(
      (_) => _ucitaj(),
    );

    _ucitaj();
  }

  @override
  void dispose() {
    _odlukaODozvoli?.cancel();
    _lokacija.removeListener(_naPromjenuPolazista);
    _sesija.removeListener(_naPromjenuPolazista);
    super.dispose();
  }

  /// Najbliza poslovnica se mijenja kad klijent ukljuci lokaciju telefona ili
  /// promijeni grad u profilu. Ostatak ekrana od toga ne zavisi, pa se osvjezava
  /// samo kartica.
  Future<void> _naPromjenuPolazista() async {
    final grad = _sesija.korisnik?.gradId;

    if (_lokacija.tacka == _zadnjaTacka && grad == _zadnjiGrad) {
      return;
    }

    _zadnjaTacka = _lokacija.tacka;
    _zadnjiGrad = grad;

    final broj = ++_upitNajblize;

    try {
      final najbliza = await _katalog.najblizaPoslovnica(
        lokacija: _lokacija.tacka,
      );

      if (mounted && broj == _upitNajblize) {
        setState(() => _najbliza = najbliza);
      }
    } on ApiGreska catch (greska) {
      // Ostatak ekrana radi i bez kartice, ali klijent mora znati da prikazana
      // udaljenost vise ne odgovara onome sto je odabrao.
      if (mounted && broj == _upitNajblize) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(greska.poruka)));
      }
    }
  }

  /// Povlacenje za osvjezavanje ponovo cita i lokaciju telefona, ako je
  /// ukljucena - klijent koji se pomjerio tako dobije novu najblizu poslovnicu.
  Future<void> _osvjezi() async {
    final poruka = await _lokacija.osvjezi();

    if (poruka != null && mounted) {
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(poruka)));
    }

    await _ucitaj(tiho: true);
  }

  /// Povlacenje za osvjezavanje ucitava tiho: sadrzaj ostaje na ekranu dok ne
  /// stigne novi, umjesto da ga zamijeni kruzic.
  Future<void> _ucitaj({bool tiho = false}) async {
    setState(() {
      _ucitavanje = !tiho;
      _greska = null;
    });

    final broj = ++_upitNajblize;

    try {
      // Nezavisni upiti idu paralelno - jedan po jedan bi ekran drzao prazan duze
      // nego sto treba.
      final rezultati = await Future.wait<Object?>([
        _katalog.preporuke(),
        _katalog.obavijesti(koliko: 3),
        _katalog.tipoviVozila(),
        _katalog.mojeKategorije(),
        _katalog.najblizaPoslovnica(lokacija: _lokacija.tacka),
      ]);

      if (!mounted) {
        return;
      }

      setState(() {
        _preporuke = rezultati[0] as List<Preporuka>;
        _obavijesti = rezultati[1] as List<Obavijest>;
        _tipovi = rezultati[2] as List<Stavka>;
        _kategorije = rezultati[3] as DozvoljeneKategorije;
        if (broj == _upitNajblize) {
          _najbliza = rezultati[4] as NajblizaPoslovnica?;
        }
        _ucitavanje = false;
      });

      await context.read<NotifikacijeStanje>().osvjezi();
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

  void _otvoriVozilo(int voziloId) {
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => DetaljiVozilaEkran(voziloId: voziloId),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final korisnik = context.watch<Sesija>().korisnik;

    return Scaffold(
      backgroundColor: Boje.platno,
      body: SafeArea(
        bottom: false,
        child: Sadrzaj(
          ucitavanje: _ucitavanje,
          greska: _greska,
          naPonovniPokusaj: _ucitaj,
          dijete: RefreshIndicator(
            onRefresh: _osvjezi,
            child: ListView(
              padding: const EdgeInsets.only(bottom: Razmaci.xxl),
              children: [
                _Zaglavlje(ime: korisnik?.ime ?? ''),
                const SizedBox(height: Razmaci.l),
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: Razmaci.l),
                  child: _PoljePretrage(
                    naDodir: () => context.read<Navigacija>().otvoriPretragu(),
                  ),
                ),
                if (_kategorije != null && !_kategorije!.mozeRezervisati) ...[
                  const SizedBox(height: Razmaci.l),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: Razmaci.l),
                    child: Obavjestenje.upozorenje(
                      _kategorije!.obrazlozenje,
                      naslov: 'Vozačka dozvola nije spremna',
                    ),
                  ),
                ],
                if (_najbliza != null) ...[
                  const SizedBox(height: Razmaci.l),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: Razmaci.l),
                    child: _NajblizaPoslovnica(
                      poslovnica: _najbliza!,
                      naDodir: () => context.read<Navigacija>().otvoriPretragu(
                        poslovnicaId: _najbliza!.id,
                      ),
                    ),
                  ),
                ],
                if (_tipovi.isNotEmpty) ...[
                  const SizedBox(height: Razmaci.l),
                  _Tipovi(
                    tipovi: _tipovi,
                    naOdabir: (tip) => context
                        .read<Navigacija>()
                        .otvoriPretragu(tipVozilaId: tip.id),
                  ),
                ],
                const SizedBox(height: Razmaci.xl),
                _Naslov(
                  tekst: 'Preporučeno za vas',
                  akcija: 'Sve ponude',
                  naAkciju: () => context.read<Navigacija>().otvoriPretragu(),
                ),
                const SizedBox(height: Razmaci.m),
                _Preporuke(stavke: _preporuke, naOdabir: _otvoriVozilo),
                if (_obavijesti.isNotEmpty) ...[
                  const SizedBox(height: Razmaci.xl),
                  const _Naslov(tekst: 'Novosti'),
                  const SizedBox(height: Razmaci.m),
                  for (final obavijest in _obavijesti)
                    Padding(
                      padding: const EdgeInsets.fromLTRB(
                        Razmaci.l,
                        0,
                        Razmaci.l,
                        Razmaci.m,
                      ),
                      child: _KarticaObavijesti(obavijest: obavijest),
                    ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _Zaglavlje extends StatelessWidget {
  const _Zaglavlje({required this.ime});

  final String ime;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.fromLTRB(
        Razmaci.l,
        Razmaci.m,
        Razmaci.s,
        Razmaci.l,
      ),
      decoration: const BoxDecoration(
        color: Boje.navy,
        borderRadius: BorderRadius.vertical(
          bottom: Radius.circular(Razmaci.xl),
        ),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  _pozdrav(),
                  style: const TextStyle(
                    color: Boje.navyPrigusen,
                    fontSize: 12.5,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  ime.isEmpty ? 'Dobro došli' : ime,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 20,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
          ),
          const Zvono(bijelo: true),
        ],
      ),
    );
  }

  /// Pozdrav prema dobu dana, po lokalnom vremenu uredjaja.
  static String _pozdrav() {
    final sat = DateTime.now().hour;

    if (sat < 11) {
      return 'Dobro jutro';
    }

    if (sat < 18) {
      return 'Dobar dan';
    }

    return 'Dobro veče';
  }
}

class _PoljePretrage extends StatelessWidget {
  const _PoljePretrage({required this.naDodir});

  final VoidCallback naDodir;

  @override
  Widget build(BuildContext context) {
    // Polje samo vodi na pretragu; tipka se tamo, gdje su i filteri i rezultati.
    return Material(
      color: Boje.povrsina,
      borderRadius: BorderRadius.circular(Zaobljenja.pilula),
      child: InkWell(
        onTap: naDodir,
        borderRadius: BorderRadius.circular(Zaobljenja.pilula),
        child: Container(
          height: 46,
          padding: const EdgeInsets.symmetric(horizontal: Razmaci.l),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(Zaobljenja.pilula),
            border: Border.all(color: Boje.ivica),
          ),
          child: const Row(
            children: [
              Icon(Icons.search, size: 20, color: Boje.tekstPrigusen),
              SizedBox(width: Razmaci.s),
              Text(
                'Pronađi vozilo za svoj termin',
                style: TextStyle(color: Boje.tekstPrigusen, fontSize: 13.5),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Kartica "Vasa najbliza poslovnica". Dodir otvara pretragu ogranicenu na nju.
class _NajblizaPoslovnica extends StatelessWidget {
  const _NajblizaPoslovnica({required this.poslovnica, required this.naDodir});

  final NajblizaPoslovnica poslovnica;
  final VoidCallback naDodir;

  @override
  Widget build(BuildContext context) {
    final adresa = [
      poslovnica.adresa,
      poslovnica.gradNaziv,
    ].where((x) => x != null && x.isNotEmpty).join(', ');

    return Material(
      color: Boje.povrsina,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(Zaobljenja.kartica),
        side: const BorderSide(color: Boje.ivica),
      ),
      child: InkWell(
        onTap: naDodir,
        borderRadius: BorderRadius.circular(Zaobljenja.kartica),
        child: Padding(
          padding: const EdgeInsets.fromLTRB(
            Razmaci.m,
            Razmaci.m,
            Razmaci.s,
            Razmaci.xs,
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  Container(
                    width: 40,
                    height: 40,
                    decoration: const BoxDecoration(
                      color: Boje.primarnaSvijetla,
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(
                      Icons.storefront_outlined,
                      size: 20,
                      color: Boje.primarnaTamnija,
                    ),
                  ),
                  const SizedBox(width: Razmaci.m),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Vaša najbliža poslovnica',
                          style: TextStyle(
                            fontSize: 11.5,
                            color: Boje.tekstPrigusen,
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          poslovnica.naziv,
                          style: const TextStyle(
                            fontSize: 14.5,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          adresa,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            fontSize: 12.5,
                            color: Boje.tekstBlazi,
                          ),
                        ),
                        if (poslovnica.radnoVrijeme != null &&
                            poslovnica.radnoVrijeme!.isNotEmpty)
                          Text(
                            'Radno vrijeme ${poslovnica.radnoVrijeme}',
                            style: const TextStyle(
                              fontSize: 12,
                              color: Boje.tekstPrigusen,
                            ),
                          ),
                      ],
                    ),
                  ),
                  const SizedBox(width: Razmaci.s),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(
                        Formati.udaljenost(poslovnica.udaljenostKm),
                        style: const TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const Text(
                        'zračno',
                        style: TextStyle(
                          fontSize: 11,
                          color: Boje.tekstPrigusen,
                        ),
                      ),
                    ],
                  ),
                  const Icon(Icons.chevron_right, color: Boje.tekstPrigusen),
                ],
              ),
              const SizedBox(height: Razmaci.xs),
              const PolazisteUdaljenosti(),
            ],
          ),
        ),
      ),
    );
  }
}

class _Tipovi extends StatelessWidget {
  const _Tipovi({required this.tipovi, required this.naOdabir});

  final List<Stavka> tipovi;
  final void Function(Stavka) naOdabir;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 38,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: Razmaci.l),
        itemCount: tipovi.length,
        separatorBuilder: (_, _) => const SizedBox(width: Razmaci.s),
        itemBuilder: (context, indeks) {
          final tip = tipovi[indeks];

          return ActionChip(
            avatar: Icon(_ikona(tip.naziv), size: 16, color: Boje.tekstBlazi),
            label: Text(tip.naziv),
            onPressed: () => naOdabir(tip),
          );
        },
      ),
    );
  }

  /// Ikona po nazivu tipa, sa opstom ikonom za sve sto se ne prepozna - sifarnik
  /// se moze dopuniti, pa se ne smije oslanjati na tacan spisak.
  static IconData _ikona(String naziv) {
    final malim = naziv.toLowerCase();

    if (malim.contains('skuter') || malim.contains('moped')) {
      return Icons.moped_outlined;
    }

    if (malim.contains('quad') || malim.contains('atv')) {
      return Icons.agriculture_outlined;
    }

    if (malim.contains('motoc') || malim.contains('motor')) {
      return Icons.two_wheeler_outlined;
    }

    return Icons.directions_bike_outlined;
  }
}

class _Naslov extends StatelessWidget {
  const _Naslov({required this.tekst, this.akcija, this.naAkciju});

  final String tekst;
  final String? akcija;
  final VoidCallback? naAkciju;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(left: Razmaci.l, right: Razmaci.s),
      child: Row(
        children: [
          Expanded(
            child: Text(
              tekst,
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
            ),
          ),
          if (akcija != null)
            TextButton(onPressed: naAkciju, child: Text(akcija!)),
        ],
      ),
    );
  }
}

class _Preporuke extends StatelessWidget {
  const _Preporuke({required this.stavke, required this.naOdabir});

  final List<Preporuka> stavke;
  final void Function(int) naOdabir;

  @override
  Widget build(BuildContext context) {
    if (stavke.isEmpty) {
      return const Padding(
        padding: EdgeInsets.symmetric(horizontal: Razmaci.l),
        child: Obavjestenje(
          tekst:
              'Preporuke se grade iz vaših ranijih najmova i ocjena. Nakon prve '
              'završene rezervacije ovdje će stajati vozila birana prema vama.',
          naslov: 'Još nemamo dovoljno podataka',
          pozadina: Boje.infoPozadina,
          bojaTeksta: Boje.infoTekst,
          ikona: Icons.auto_awesome_outlined,
        ),
      );
    }

    return SizedBox(
      height: 340,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: Razmaci.l),
        itemCount: stavke.length,
        separatorBuilder: (_, _) => const SizedBox(width: Razmaci.m),
        itemBuilder: (context, indeks) {
          final preporuka = stavke[indeks];

          return KarticaVozila(
            vozilo: preporuka.vozilo,
            sirina: 244,
            obrazlozenje: preporuka.obrazlozenje,
            // Zvjezdica je prosjek recenzija vozila, kao i u pretrazi. Procjena modela
            // za korisnika stoji u obrazlozenju, uz taj prosjek.
            ocjena: preporuka.vozilo.prosjecnaOcjena,
            naDodir: () => naOdabir(preporuka.vozilo.id),
          );
        },
      ),
    );
  }
}

class _KarticaObavijesti extends StatelessWidget {
  const _KarticaObavijesti({required this.obavijest});

  final Obavijest obavijest;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(Razmaci.m),
      decoration: BoxDecoration(
        color: Boje.povrsina,
        borderRadius: BorderRadius.circular(Zaobljenja.kartica),
        border: Border.all(color: Boje.ivica),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Slika(
            putanja: obavijest.thumbnailUrl ?? obavijest.slikaUrl,
            sirina: 64,
            visina: 64,
            zamjenskaIkona: Icons.campaign_outlined,
          ),
          const SizedBox(width: Razmaci.m),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  obavijest.naslov,
                  style: const TextStyle(
                    fontSize: 13.5,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  obavijest.tekst,
                  maxLines: 3,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    fontSize: 12.5,
                    height: 1.4,
                    color: Boje.tekstBlazi,
                  ),
                ),
                const SizedBox(height: Razmaci.s),
                Text(
                  Formati.datum(obavijest.datumObjave),
                  style: const TextStyle(
                    fontSize: 11,
                    color: Boje.tekstPrigusen,
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
