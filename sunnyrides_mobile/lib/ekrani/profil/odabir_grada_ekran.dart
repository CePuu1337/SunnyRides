import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/vozilo.dart';
import '../../servisi/katalog_servis.dart';
import '../../servisi/profil_servis.dart';
import '../../stanje/sesija.dart';
import '../../widgeti/obavjestenje.dart';

/// Izbor grada, prije ulaska u aplikaciju.
///
/// Klijent ga vidi odmah poslije registracije: forma registracije je anonimna i ne
/// moze ucitati listu gradova, pa se grad bira cim je nalog prijavljen. Isti ekran
/// dobije i svaki drugi klijent koji jos nema grad. Ne moze se preskociti, jer bez
/// grada aplikacija ne zna koje su mu poslovnice najblize.
class OdabirGradaEkran extends StatefulWidget {
  const OdabirGradaEkran({super.key});

  @override
  State<OdabirGradaEkran> createState() => _OdabirGradaEkranStanje();
}

class _OdabirGradaEkranStanje extends State<OdabirGradaEkran> {
  late final KatalogServis _katalog;
  late final ProfilServis _profil;

  List<Stavka> _gradovi = const [];
  int? _odabrani;

  bool _ucitavanje = true;
  bool _slanje = false;
  String? _greskaUcitavanja;
  String? _greska;

  @override
  void initState() {
    super.initState();

    final klijent = context.read<ApiKlijent>();
    _katalog = KatalogServis(klijent);
    _profil = ProfilServis(klijent);

    _ucitaj();
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greskaUcitavanja = null;
    });

    try {
      final gradovi = await _katalog.gradovi();

      if (!mounted) {
        return;
      }

      setState(() {
        _gradovi = gradovi;
        _ucitavanje = false;
      });
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() {
        _greskaUcitavanja = greska.poruka;
        _ucitavanje = false;
      });
    }
  }

  Future<void> _nastavi() async {
    final gradId = _odabrani;

    if (gradId == null) {
      setState(() => _greska = 'Odaberite grad u kojem ste.');

      return;
    }

    setState(() {
      _slanje = true;
      _greska = null;
    });

    try {
      final korisnik = await _profil.postaviGrad(gradId);

      if (!mounted) {
        return;
      }

      // Sa gradom u profilu korijen aplikacije sam prelazi na pocetni ekran.
      context.read<Sesija>().osvjeziKorisnika(korisnik);
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() {
        _slanje = false;
        _greska = greska.poruka;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final ime = context.watch<Sesija>().korisnik?.ime ?? '';

    return Scaffold(
      backgroundColor: Boje.platno,
      body: SafeArea(
        child: Sadrzaj(
          ucitavanje: _ucitavanje,
          greska: _greskaUcitavanja,
          naPonovniPokusaj: _ucitaj,
          dijete: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(
                  Razmaci.l,
                  Razmaci.xl,
                  Razmaci.l,
                  Razmaci.l,
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Icon(
                      Icons.location_city_outlined,
                      size: 32,
                      color: Boje.primarnaTamnija,
                    ),
                    const SizedBox(height: Razmaci.m),
                    Text(
                      ime.isEmpty ? 'Gdje ste?' : '$ime, gdje ste?',
                      style: const TextStyle(
                        fontSize: 20,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: Razmaci.s),
                    const Text(
                      'Odaberite grad u kojem ste. Po njemu vam prvo nudimo vozila '
                      'iz najbližih poslovnica. Grad kasnije možete promijeniti '
                      'u profilu.',
                      style: TextStyle(
                        fontSize: 13.5,
                        height: 1.4,
                        color: Boje.tekstBlazi,
                      ),
                    ),
                  ],
                ),
              ),
              if (_greska != null)
                Padding(
                  padding: const EdgeInsets.fromLTRB(
                    Razmaci.l,
                    0,
                    Razmaci.l,
                    Razmaci.m,
                  ),
                  child: Obavjestenje.greska(_greska!),
                ),
              Expanded(
                child: ListView.separated(
                  padding: const EdgeInsets.symmetric(horizontal: Razmaci.l),
                  itemCount: _gradovi.length,
                  separatorBuilder: (_, _) => const SizedBox(height: Razmaci.s),
                  itemBuilder: (context, indeks) {
                    final grad = _gradovi[indeks];
                    final odabran = grad.id == _odabrani;

                    return Material(
                      color: odabran ? Boje.primarnaSvijetla : Boje.povrsina,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(Zaobljenja.kartica),
                        side: BorderSide(
                          color: odabran ? Boje.primarnaTamnija : Boje.ivica,
                        ),
                      ),
                      child: ListTile(
                        title: Text(grad.naziv),
                        trailing: odabran
                            ? const Icon(
                                Icons.check_circle,
                                color: Boje.primarnaTamnija,
                              )
                            : null,
                        onTap: _slanje
                            ? null
                            : () => setState(() {
                                _odabrani = grad.id;
                                _greska = null;
                              }),
                      ),
                    );
                  },
                ),
              ),
              Padding(
                padding: const EdgeInsets.all(Razmaci.l),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    FilledButton(
                      onPressed: _slanje ? null : _nastavi,
                      child: Text(_slanje ? 'Čuvanje...' : 'Nastavi'),
                    ),
                    const SizedBox(height: Razmaci.s),
                    TextButton(
                      onPressed: _slanje
                          ? null
                          : () => context.read<Sesija>().odjava(),
                      child: const Text('Odjavi se'),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
