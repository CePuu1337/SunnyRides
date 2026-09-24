import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/vozilo.dart';
import '../../servisi/cijena_servis.dart';
import '../../servisi/rezervacija_servis.dart';
import '../../widgeti/obavjestenje.dart';
import '../../widgeti/slika.dart';
import 'placanje_ekran.dart';

/// Sastavljanje rezervacije: termin, oprema, osiguranje i razrada cijene.
///
/// Svaki iznos na ovom ekranu dolazi sa servera. Promjena bilo cega salje novi
/// obracun - aplikacija ne sabira ni popust ni ukupno.
class NovaRezervacijaEkran extends StatefulWidget {
  const NovaRezervacijaEkran({
    super.key,
    required this.vozilo,
    this.datumOd,
    this.datumDo,
  });

  final Vozilo vozilo;
  final DateTime? datumOd;
  final DateTime? datumDo;

  @override
  State<NovaRezervacijaEkran> createState() => _NovaRezervacijaEkranStanje();
}

class _NovaRezervacijaEkranStanje extends State<NovaRezervacijaEkran> {
  late final CijenaServis _cijene;
  late final RezervacijaServis _rezervacije;

  late DateTime _od;
  late DateTime _do;

  List<VrstaOpreme> _oprema = const [];
  List<PaketOsiguranja> _paketi = const [];
  final Map<int, int> _kolicine = {};
  int? _paketId;

  CijenaRezervacije? _obracun;
  Dostupnost? _dostupnost;

  bool _ucitavanje = true;
  bool _racunanje = false;
  bool _slanje = false;
  String? _greska;
  String? _greskaObracuna;

  @override
  void initState() {
    super.initState();

    final klijent = context.read<ApiKlijent>();

    _cijene = CijenaServis(klijent);
    _rezervacije = RezervacijaServis(klijent);

    // Bez termina iz pretrage nudi se sutrasnji dan, od devet do osamnaest.
    final sutra = DateTime.now().add(const Duration(days: 1));

    _od = widget.datumOd ?? DateTime(sutra.year, sutra.month, sutra.day, 9);
    _do = widget.datumDo ?? _od.add(const Duration(hours: 9));

    _pripremi();
  }

  Future<void> _pripremi() async {
    try {
      final rezultati = await Future.wait([
        _cijene.vrsteOpreme(),
        _cijene.paketiOsiguranja(),
      ]);

      if (!mounted) {
        return;
      }

      setState(() {
        _oprema = rezultati[0] as List<VrstaOpreme>;
        _paketi = rezultati[1] as List<PaketOsiguranja>;
        _ucitavanje = false;
      });

      await _osvjeziObracun();
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

  ZahtjevRezervacije get _zahtjev => ZahtjevRezervacije(
    voziloId: widget.vozilo.id,
    datumOd: _od,
    datumDo: _do,
    oprema: _kolicine,
    paketOsiguranjaId: _paketId,
  );

  Future<void> _osvjeziObracun() async {
    setState(() {
      _racunanje = true;
      _greskaObracuna = null;
    });

    try {
      final rezultati = await Future.wait([
        _cijene.obracun(_zahtjev),
        _cijene.provjeriTermin(
          voziloId: widget.vozilo.id,
          datumOd: _od,
          datumDo: _do,
        ),
      ]);

      if (!mounted) {
        return;
      }

      setState(() {
        _obracun = rezultati[0] as CijenaRezervacije;
        _dostupnost = rezultati[1] as Dostupnost;
        _racunanje = false;
      });
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() {
        _obracun = null;
        _greskaObracuna = greska.poruka;
        _racunanje = false;
      });
    }
  }

  Future<void> _odaberiDatum({required bool pocetak}) async {
    final sada = DateTime.now();
    final polazni = pocetak ? _od : _do;

    final datum = await showDatePicker(
      context: context,
      initialDate: polazni,
      firstDate: DateTime(sada.year, sada.month, sada.day),
      lastDate: DateTime(sada.year + 1, 12, 31),
    );

    if (datum == null || !mounted) {
      return;
    }

    final vrijeme = await showTimePicker(
      context: context,
      initialTime: TimeOfDay(hour: polazni.hour, minute: polazni.minute),
    );

    if (vrijeme == null || !mounted) {
      return;
    }

    final novi = DateTime(
      datum.year,
      datum.month,
      datum.day,
      vrijeme.hour,
      vrijeme.minute,
    );

    setState(() {
      if (pocetak) {
        _od = novi;

        if (!_do.isAfter(_od)) {
          _do = _od.add(const Duration(hours: 4));
        }
      } else {
        _do = novi;
      }
    });

    if (_do.isAfter(_od)) {
      await _osvjeziObracun();
    }
  }

  void _promijeniKolicinu(VrstaOpreme vrsta, int promjena) {
    final trenutna = _kolicine[vrsta.id] ?? 0;
    final nova = (trenutna + promjena).clamp(0, 5);

    if (nova == trenutna) {
      return;
    }

    setState(() {
      if (nova == 0) {
        _kolicine.remove(vrsta.id);
      } else {
        _kolicine[vrsta.id] = nova;
      }
    });

    _osvjeziObracun();
  }

  void _odaberiPaket(int? paketId) {
    if (_paketId == paketId) {
      return;
    }

    setState(() => _paketId = paketId);
    _osvjeziObracun();
  }

  Future<void> _potvrdi() async {
    setState(() => _slanje = true);

    try {
      final rezervacija = await _rezervacije.kreiraj(_zahtjev);

      if (!mounted) {
        return;
      }

      setState(() => _slanje = false);

      await Navigator.of(context).pushReplacement(
        MaterialPageRoute<void>(
          builder: (_) => PlacanjeEkran(rezervacija: rezervacija),
        ),
      );
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() => _slanje = false);

      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(greska.poruka)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final obracun = _obracun;
    final dostupnost = _dostupnost;
    final terminJeIspravan = _do.isAfter(_od);

    final mozeDalje =
        terminJeIspravan &&
        obracun != null &&
        !_racunanje &&
        !_slanje &&
        (dostupnost?.slobodno ?? false);

    return Scaffold(
      backgroundColor: Boje.platno,
      appBar: AppBar(title: const Text('Nova rezervacija')),
      body: Sadrzaj(
        ucitavanje: _ucitavanje,
        greska: _greska,
        naPonovniPokusaj: _pripremi,
        dijete: ListView(
          padding: const EdgeInsets.all(Razmaci.l),
          children: [
            _Vozilo(vozilo: widget.vozilo),
            const SizedBox(height: Razmaci.l),
            _Okvir(
              naslov: 'Termin',
              dijete: Column(
                children: [
                  _DatumRed(
                    oznaka: 'Preuzimanje',
                    vrijednost: _od,
                    naDodir: () => _odaberiDatum(pocetak: true),
                  ),
                  const SizedBox(height: Razmaci.s),
                  _DatumRed(
                    oznaka: 'Povrat',
                    vrijednost: _do,
                    naDodir: () => _odaberiDatum(pocetak: false),
                  ),
                  if (!terminJeIspravan) ...[
                    const SizedBox(height: Razmaci.m),
                    Obavjestenje.greska(
                      'Povrat mora biti poslije preuzimanja.',
                    ),
                  ] else if (dostupnost != null && !dostupnost.slobodno) ...[
                    const SizedBox(height: Razmaci.m),
                    Obavjestenje.upozorenje(
                      dostupnost.razlog.isEmpty
                          ? 'Vozilo je zauzeto u ovom terminu.'
                          : dostupnost.razlog,
                      naslov: 'Termin nije slobodan',
                    ),
                  ],
                ],
              ),
            ),
            if (_oprema.isNotEmpty) ...[
              const SizedBox(height: Razmaci.l),
              _Okvir(
                naslov: 'Dodatna oprema',
                dijete: Column(
                  children: [
                    for (final vrsta in _oprema)
                      _OpremaRed(
                        vrsta: vrsta,
                        kolicina: _kolicine[vrsta.id] ?? 0,
                        naPromjenu: (promjena) =>
                            _promijeniKolicinu(vrsta, promjena),
                      ),
                  ],
                ),
              ),
            ],
            if (_paketi.isNotEmpty) ...[
              const SizedBox(height: Razmaci.l),
              _Okvir(
                naslov: 'Osiguranje',
                dijete: Column(
                  children: [
                    _PaketRed(
                      naziv: 'Bez dodatnog osiguranja',
                      opis: 'Šteta se naplaćuje iz depozita.',
                      odabran: _paketId == null,
                      naOdabir: () => _odaberiPaket(null),
                    ),
                    for (final paket in _paketi)
                      _PaketRed(
                        naziv: paket.naziv,
                        opis:
                            '${Formati.novac(paket.cijenaPoDanu)} po danu · '
                            'učešće ${Formati.novac(paket.iznosUcesca)}',
                        odabran: _paketId == paket.id,
                        naOdabir: () => _odaberiPaket(paket.id),
                      ),
                  ],
                ),
              ),
            ],
            const SizedBox(height: Razmaci.l),
            _Razrada(
              obracun: obracun,
              racunanje: _racunanje,
              greska: _greskaObracuna,
            ),
            const SizedBox(height: Razmaci.xxl),
          ],
        ),
      ),
      bottomNavigationBar: SafeArea(
        child: Container(
          padding: const EdgeInsets.all(Razmaci.l),
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
                  const Text(
                    'Za naplatu',
                    style: TextStyle(fontSize: 11.5, color: Boje.tekstPrigusen),
                  ),
                  Text(
                    obracun == null ? '-' : Formati.novac(obracun.ukupanIznos),
                    style: const TextStyle(
                      fontSize: 17,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
              ),
              const SizedBox(width: Razmaci.l),
              Expanded(
                child: FilledButton(
                  onPressed: mozeDalje ? _potvrdi : null,
                  child: _slanje
                      ? const SizedBox(
                          width: 18,
                          height: 18,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Text('Nastavi na plaćanje'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _Vozilo extends StatelessWidget {
  const _Vozilo({required this.vozilo});

  final Vozilo vozilo;

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
        children: [
          Slika(putanja: vozilo.thumbnailUrl, sirina: 74, visina: 56),
          const SizedBox(width: Razmaci.m),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  vozilo.naziv,
                  style: const TextStyle(
                    fontSize: 14.5,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  vozilo.lokacija.isEmpty
                      ? vozilo.registarskaOznaka
                      : vozilo.lokacija,
                  style: const TextStyle(
                    fontSize: 12,
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

class _DatumRed extends StatelessWidget {
  const _DatumRed({
    required this.oznaka,
    required this.vrijednost,
    required this.naDodir,
  });

  final String oznaka;
  final DateTime vrijednost;
  final VoidCallback naDodir;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: naDodir,
      borderRadius: BorderRadius.circular(Zaobljenja.dugme),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: Razmaci.s),
        child: Row(
          children: [
            const Icon(Icons.schedule, size: 17, color: Boje.tekstPrigusen),
            const SizedBox(width: Razmaci.s),
            Expanded(child: Text(oznaka, style: const TextStyle(fontSize: 13))),
            Text(
              Formati.datumIVrijeme(vrijednost),
              style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
            ),
            const Icon(
              Icons.chevron_right,
              size: 18,
              color: Boje.tekstPrigusen,
            ),
          ],
        ),
      ),
    );
  }
}

class _OpremaRed extends StatelessWidget {
  const _OpremaRed({
    required this.vrsta,
    required this.kolicina,
    required this.naPromjenu,
  });

  final VrstaOpreme vrsta;
  final int kolicina;
  final ValueChanged<int> naPromjenu;

  @override
  Widget build(BuildContext context) {
    final cijena = vrsta.cijenaPoDanu != null
        ? '${Formati.novac(vrsta.cijenaPoDanu!)} po danu'
        : vrsta.fiksnaCijena != null
        ? '${Formati.novac(vrsta.fiksnaCijena!)} jednokratno'
        : '';

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: Razmaci.xs),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(vrsta.naziv, style: const TextStyle(fontSize: 13)),
                if (cijena.isNotEmpty)
                  Text(
                    cijena,
                    style: const TextStyle(
                      fontSize: 11,
                      color: Boje.tekstPrigusen,
                    ),
                  ),
              ],
            ),
          ),
          IconButton(
            onPressed: kolicina == 0 ? null : () => naPromjenu(-1),
            icon: const Icon(Icons.remove_circle_outline),
            iconSize: 21,
            visualDensity: VisualDensity.compact,
          ),
          SizedBox(
            width: 22,
            child: Text(
              '$kolicina',
              textAlign: TextAlign.center,
              style: const TextStyle(
                fontSize: 13.5,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          IconButton(
            onPressed: () => naPromjenu(1),
            icon: const Icon(Icons.add_circle_outline),
            iconSize: 21,
            visualDensity: VisualDensity.compact,
          ),
        ],
      ),
    );
  }
}

class _Razrada extends StatelessWidget {
  const _Razrada({required this.obracun, required this.racunanje, this.greska});

  final CijenaRezervacije? obracun;
  final bool racunanje;
  final String? greska;

  @override
  Widget build(BuildContext context) {
    if (greska != null) {
      return Obavjestenje.greska(greska!);
    }

    if (obracun == null) {
      return const Center(
        child: Padding(
          padding: EdgeInsets.all(Razmaci.l),
          child: CircularProgressIndicator(),
        ),
      );
    }

    final cijena = obracun!;

    return Opacity(
      opacity: racunanje ? 0.5 : 1,
      child: _Okvir(
        naslov: 'Razrada cijene',
        dijete: Column(
          children: [
            _Stavka(
              oznaka: cijena.naplataPoSatu
                  ? 'Najam (${cijena.brojSati} h × ${Formati.novac(cijena.satnaTarifa)})'
                  : 'Najam (${cijena.brojDana} d × ${Formati.novac(cijena.dnevnaTarifa)})',
              iznos: cijena.osnovicaNajma,
            ),
            if (cijena.mnozilac != 1)
              _Stavka(
                oznaka: 'Sezona ${cijena.nazivSezone ?? ''}'.trim(),
                iznos: cijena.iznosNajma - cijena.osnovicaNajma,
              ),
            if (cijena.iznosPopusta > 0)
              _Stavka(
                oznaka: 'Popust (${Formati.postotak(cijena.procenatPopusta)})',
                iznos: -cijena.iznosPopusta,
                istakni: true,
              ),
            for (final stavka in cijena.oprema)
              _Stavka(
                oznaka: '${stavka.naziv} × ${stavka.kolicina}',
                iznos: stavka.iznos,
              ),
            if (cijena.iznosOsiguranja > 0)
              _Stavka(
                oznaka: cijena.paketOsiguranjaNaziv ?? 'Osiguranje',
                iznos: cijena.iznosOsiguranja,
              ),
            _Stavka(
              oznaka: 'Depozit (vraća se nakon povrata)',
              iznos: cijena.iznosDepozita,
            ),
            const Divider(height: Razmaci.xl),
            _Stavka(
              oznaka: 'Ukupno',
              iznos: cijena.ukupanIznos,
              podebljano: true,
            ),
          ],
        ),
      ),
    );
  }
}

class _Stavka extends StatelessWidget {
  const _Stavka({
    required this.oznaka,
    required this.iznos,
    this.podebljano = false,
    this.istakni = false,
  });

  final String oznaka;
  final double iznos;
  final bool podebljano;
  final bool istakni;

  @override
  Widget build(BuildContext context) {
    final stil = TextStyle(
      fontSize: podebljano ? 14.5 : 13,
      fontWeight: podebljano ? FontWeight.w700 : FontWeight.w400,
      color: istakni ? Boje.uspjeh : Boje.tekst,
    );

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        children: [
          Expanded(child: Text(oznaka, style: stil)),
          Text(Formati.novac(iznos), style: stil),
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

/// Jedan paket osiguranja u listi. Umjesto Radio widgeta stoji obican red koji se
/// moze dodirnuti - isti izgled, a bez zavisnosti od API-ja koji se kroz verzije
/// Fluttera mijenjao.
class _PaketRed extends StatelessWidget {
  const _PaketRed({
    required this.naziv,
    required this.opis,
    required this.odabran,
    required this.naOdabir,
  });

  final String naziv;
  final String opis;
  final bool odabran;
  final VoidCallback naOdabir;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: naOdabir,
      borderRadius: BorderRadius.circular(Zaobljenja.dugme),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: Razmaci.s),
        child: Row(
          children: [
            Icon(
              odabran
                  ? Icons.radio_button_checked
                  : Icons.radio_button_unchecked,
              size: 19,
              color: odabran ? Boje.primarnaTamnija : Boje.ivicaJaca,
            ),
            const SizedBox(width: Razmaci.m),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    naziv,
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: odabran ? FontWeight.w600 : FontWeight.w400,
                    ),
                  ),
                  Text(
                    opis,
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
      ),
    );
  }
}
