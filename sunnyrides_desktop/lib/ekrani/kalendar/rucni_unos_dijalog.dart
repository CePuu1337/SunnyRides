import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/kalendar.dart';
import '../../servisi/kalendar_servis.dart';
import '../../widgeti/dijalog_forme.dart';
import '../../widgeti/obavjestenje.dart';

/// Sta je dijalog na kraju uradio. Kalendar po ovome zna sta da javi korisniku.
enum IshodUnosa { rezervacija, blokada }

/// Rezervacija koju uposlenik unosi za klijenta sa saltera.
///
/// Prolazi iste provjere kao kad klijent rezervise sam: dozvola, dostupnost, zalihe
/// opreme i cijena na serveru. Nastaje kao rezervacija koja ceka placanje i drzi
/// termin - unos sa saltera ne preskace nijedan korak.
class RucniUnosDijalog extends StatefulWidget {
  const RucniUnosDijalog({
    super.key,
    required this.vozilo,
    required this.dan,
    required this.bufferSati,
  });

  final RedKalendara vozilo;
  final DateTime dan;
  final double bufferSati;

  @override
  State<RucniUnosDijalog> createState() => _RucniUnosDijalogStanje();
}

class _RucniUnosDijalogStanje extends State<RucniUnosDijalog> {
  late final KalendarServis _servis;

  KlijentZaOdabir? _klijent;
  late DateTime _od;
  late DateTime _do;

  List<VrstaOpreme> _vrsteOpreme = const [];
  List<PaketOsiguranja> _paketi = const [];
  final Map<int, int> _oprema = {};
  int? _paketId;

  CijenaRezervacije? _cijena;
  String? _greskaObracuna;
  bool _racuna = false;
  int _zadnjiObracun = 0;

  bool _ucitavanje = true;
  bool _snimanje = false;
  String? _greska;

  // --- kvar i blokada vozila ---

  /// Dijalog ima dva nacina rada: unos rezervacije i prijava kvara. Isti je zato sto
  /// se oba otvaraju istim potezom - klikom na dan u kalendaru - i oba se ticu istog
  /// vozila i istog termina.
  bool _kvar = false;

  late DateTime _blokadaOd;
  late DateTime _blokadaDo;
  final _razlog = TextEditingController();

  List<PogodjenaRezervacija> _pogodjene = const [];
  bool _skratiDoRezervacije = true;
  bool _provjeravam = false;
  int _zadnjaProvjera = 0;

  @override
  void initState() {
    super.initState();

    _servis = KalendarServis(context.read<ApiKlijent>());

    _postaviPocetniTermin();

    // Kvar pocinje sada, jer je vozilo od ovog trenutka neupotrebljivo. Kraj je tri
    // dana kasnije, kao gruba pretpostavka koju uposlenik mijenja.
    final sada = DateTime.now();
    _blokadaOd = widget.dan.isAfter(sada) ? widget.dan : sada;
    _blokadaDo = _blokadaOd.add(const Duration(days: 3));

    _ucitaj();
  }

  /// Podrazumijevani termin je od deset ujutro do deset sutradan. Ako je taj dan
  /// danas i deset je vec proslo, pocetak se pomjera na sljedeci puni sat.
  void _postaviPocetniTermin() {
    final sada = DateTime.now();
    var od = DateTime(widget.dan.year, widget.dan.month, widget.dan.day, 10);

    if (od.isBefore(sada)) {
      od = DateTime(sada.year, sada.month, sada.day, sada.hour + 1);
    }

    _od = od;
    _do = od.add(const Duration(days: 1));
  }

  Future<void> _ucitaj() async {
    try {
      final vrste = await _servis.vrsteOpreme();
      final paketi = await _servis.paketiOsiguranja();

      if (!mounted) {
        return;
      }

      setState(() {
        _vrsteOpreme = vrste;
        _paketi = paketi;
        _ucitavanje = false;
      });

      _preracunaj();
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
  void dispose() {
    _razlog.dispose();
    super.dispose();
  }

  /// Kraj blokade kakav ce stvarno biti upisan.
  ///
  /// Kad blokada preklapa rezervaciju, a skracivanje je ukljuceno, blokada se
  /// zaustavlja na pocetku te rezervacije. Vozilo je tada nedostupno sve do termina
  /// koji je vec obecan nekom drugom - a sta ce sa tim terminom odlucuje uposlenik
  /// telefonom, ne forma umjesto njega.
  DateTime get _stvarniKrajBlokade {
    if (!_skratiDoRezervacije || _pogodjene.isEmpty) {
      return _blokadaDo;
    }

    final prva = _pogodjene
        .map((x) => x.datumOd.toLocal())
        .reduce((a, b) => a.isBefore(b) ? a : b);

    return prva.isBefore(_blokadaDo) ? prva : _blokadaDo;
  }

  bool get _blokadaImaSmisla => _stvarniKrajBlokade.isAfter(_blokadaOd);

  Future<void> _provjeriPogodjene() async {
    final redniBroj = ++_zadnjaProvjera;

    setState(() => _provjeravam = true);

    try {
      final pogodjene = await _servis.pogodjeneRezervacije(
        voziloId: widget.vozilo.voziloId,
        od: _blokadaOd,
        doDatuma: _blokadaDo,
      );

      if (!mounted || redniBroj != _zadnjaProvjera) {
        return;
      }

      setState(() {
        _pogodjene = pogodjene;
        _provjeravam = false;
      });
    } on ApiGreska catch (greska) {
      if (!mounted || redniBroj != _zadnjaProvjera) {
        return;
      }

      setState(() {
        _provjeravam = false;
        _greska = greska.poruka;
      });
    }
  }

  Future<void> _odaberiTrenutakBlokade({required bool pocetak}) async {
    final trenutni = pocetak ? _blokadaOd : _blokadaDo;

    final datum = await showDatePicker(
      context: context,
      initialDate: trenutni,
      firstDate: DateTime.now().subtract(const Duration(days: 1)),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );

    if (datum == null || !mounted) {
      return;
    }

    final vrijeme = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(trenutni),
      builder: (context, dijete) => MediaQuery(
        data: MediaQuery.of(context).copyWith(alwaysUse24HourFormat: true),
        child: dijete!,
      ),
    );

    if (vrijeme == null) {
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
        _blokadaOd = novi;

        if (!_blokadaDo.isAfter(_blokadaOd)) {
          _blokadaDo = _blokadaOd.add(const Duration(days: 1));
        }
      } else {
        _blokadaDo = novi;
      }
    });

    _provjeriPogodjene();
  }

  Future<void> _sacuvajBlokadu() async {
    final razlog = _razlog.text.trim();

    if (razlog.length < 3) {
      setState(() => _greska = 'Upišite razlog blokade, bar tri znaka.');

      return;
    }

    if (!_blokadaImaSmisla) {
      setState(
        () => _greska =
            'Prva rezervacija počinje prije kraja blokade, pa skraćena blokada ne bi '
            'trajala ništa. Pomjerite početak ili isključite skraćivanje.',
      );

      return;
    }

    setState(() {
      _snimanje = true;
      _greska = null;
    });

    try {
      await _servis.blokirajVozilo(
        voziloId: widget.vozilo.voziloId,
        od: _blokadaOd,
        doDatuma: _stvarniKrajBlokade,
        razlog: razlog,
      );

      if (!mounted) {
        return;
      }

      Navigator.of(context).pop(IshodUnosa.blokada);
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() {
        _snimanje = false;
        _greska = greska.poruka;
      });
    }
  }

  void _promijeniNacin(bool kvar) {
    setState(() {
      _kvar = kvar;
      _greska = null;
    });

    if (kvar) {
      _provjeriPogodjene();
    }
  }

  ZahtjevRezervacije get _zahtjev => ZahtjevRezervacije(
    voziloId: widget.vozilo.voziloId,
    datumOd: _od,
    datumDo: _do,
    oprema: Map.of(_oprema),
    paketOsiguranjaId: _paketId,
  );

  /// Cijenu racuna server pri svakoj promjeni. Ako stignu dva odgovora, vazi samo
  /// onaj za zadnju promjenu - raniji bi pokazao iznos za termin koji vise nije odabran.
  Future<void> _preracunaj() async {
    final redniBroj = ++_zadnjiObracun;

    setState(() {
      _racuna = true;
      _greskaObracuna = null;
    });

    try {
      final cijena = await _servis.obracun(_zahtjev);

      if (!mounted || redniBroj != _zadnjiObracun) {
        return;
      }

      setState(() {
        _cijena = cijena;
        _racuna = false;
      });
    } on ApiGreska catch (greska) {
      if (!mounted || redniBroj != _zadnjiObracun) {
        return;
      }

      setState(() {
        _cijena = null;
        _greskaObracuna = greska.poruka;
        _racuna = false;
      });
    }
  }

  Future<void> _odaberiTrenutak({required bool pocetak}) async {
    final trenutni = pocetak ? _od : _do;

    final datum = await showDatePicker(
      context: context,
      initialDate: trenutni,
      firstDate: DateTime.now().subtract(const Duration(days: 1)),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );

    if (datum == null || !mounted) {
      return;
    }

    final vrijeme = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(trenutni),
      builder: (context, dijete) => MediaQuery(
        data: MediaQuery.of(context).copyWith(alwaysUse24HourFormat: true),
        child: dijete!,
      ),
    );

    if (vrijeme == null) {
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

        // Kraj prije pocetka nema smisla. Umjesto greske, kraj se pomjeri da period
        // ostane iste duzine kao prije.
        if (!_do.isAfter(_od)) {
          _do = _od.add(const Duration(days: 1));
        }
      } else {
        _do = novi;
      }
    });

    _preracunaj();
  }

  void _promijeniOpremu(int vrstaId, int kolicina) {
    setState(() {
      if (kolicina <= 0) {
        _oprema.remove(vrstaId);
      } else {
        _oprema[vrstaId] = kolicina;
      }
    });

    _preracunaj();
  }

  Future<void> _sacuvaj() async {
    final klijent = _klijent;

    if (klijent == null) {
      setState(() => _greska = 'Odaberite klijenta.');

      return;
    }

    if (_cijena == null) {
      setState(() => _greska = _greskaObracuna ?? 'Obračun još nije spreman.');

      return;
    }

    setState(() {
      _snimanje = true;
      _greska = null;
    });

    try {
      await _servis.kreirajZaKlijenta(klijent.id, _zahtjev);

      if (!mounted) {
        return;
      }

      Navigator.of(context).pop(IshodUnosa.rezervacija);
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() {
        _snimanje = false;
        _greska = greska.poruka;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return DijalogForme(
      naslov: _kvar ? 'Kvar i blokada vozila' : 'Ručni unos rezervacije',
      podnaslov:
          '${widget.vozilo.vozilo} · ${widget.vozilo.registarskaOznaka} · '
          '${widget.vozilo.poslovnica}',
      greska: _greska,
      uToku: _snimanje,
      natpisPotvrde: _kvar ? 'Blokiraj vozilo' : 'Unesi rezervaciju',
      naSnimanje: _kvar ? _sacuvajBlokadu : _sacuvaj,
      sirina: 820,
      dijete: _ucitavanje
          ? const Padding(
              padding: EdgeInsets.all(Razmaci.xxl),
              child: Center(child: CircularProgressIndicator()),
            )
          : _kvar
          ? _blokada()
          : Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(flex: 3, child: _unos()),
                    const SizedBox(width: Razmaci.xl),
                    Expanded(flex: 2, child: _obracun()),
                  ],
                ),
                const SizedBox(height: Razmaci.xl),
                const Divider(),
                const SizedBox(height: Razmaci.s),
                Row(
                  children: [
                    const Expanded(
                      child: Text(
                        'Vozilo nije ispravno i ne može se izdavati?',
                        style: TextStyle(
                          color: Boje.tekstPrigusen,
                          fontSize: 12.5,
                        ),
                      ),
                    ),
                    OutlinedButton.icon(
                      onPressed: () => _promijeniNacin(true),
                      icon: const Icon(Icons.build_outlined, size: 17),
                      label: const Text('Prijavi kvar'),
                    ),
                  ],
                ),
              ],
            ),
    );
  }

  Widget _blokada() {
    final skraceno = _stvarniKrajBlokade != _blokadaDo;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Obavjestenje.info(
          'Blokirano vozilo se ne nudi u pretrazi i ne može se rezervisati u tom '
          'periodu. Postojeće rezervacije se ovim ne otkazuju.',
        ),
        const SizedBox(height: Razmaci.xl),
        const _Naslov('Period nedostupnosti'),
        RedPolja(
          lijevo: _PoljeTrenutka(
            natpis: 'Od',
            vrijednost: _blokadaOd,
            naPritisak: () => _odaberiTrenutakBlokade(pocetak: true),
          ),
          desno: _PoljeTrenutka(
            natpis: 'Do',
            vrijednost: _blokadaDo,
            naPritisak: () => _odaberiTrenutakBlokade(pocetak: false),
          ),
        ),
        const SizedBox(height: Razmaci.xl),
        const _Naslov('Razlog'),
        TextField(
          controller: _razlog,
          maxLines: 2,
          maxLength: 500,
          onChanged: (_) {
            if (_greska != null) {
              setState(() => _greska = null);
            }
          },
          decoration: const InputDecoration(
            labelText: 'Šta je sa vozilom',
            hintText: 'Kvar na kočnicama, vozilo na servisu.',
            alignLabelWithHint: true,
          ),
        ),
        const SizedBox(height: Razmaci.s),
        if (_provjeravam)
          const Row(
            children: [
              SizedBox(
                width: 14,
                height: 14,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
              SizedBox(width: Razmaci.m),
              Text(
                'Provjeravam koje rezervacije ovo pogađa…',
                style: TextStyle(color: Boje.tekstPrigusen, fontSize: 12.5),
              ),
            ],
          )
        else if (_pogodjene.isEmpty)
          Obavjestenje.uspjeh(
            'U tom periodu nema nijedne rezervacije na ovom vozilu.',
          )
        else ...[
          Obavjestenje.upozorenje(
            'U tom periodu ${_pogodjene.length == 1 ? 'postoji rezervacija' : 'postoje ${_pogodjene.length} rezervacije'} '
            'na ovom vozilu. Blokada ih ne otkazuje — klijente treba nazvati.',
          ),
          const SizedBox(height: Razmaci.m),
          CheckboxListTile(
            value: _skratiDoRezervacije,
            onChanged: (novo) =>
                setState(() => _skratiDoRezervacije = novo ?? false),
            contentPadding: EdgeInsets.zero,
            controlAffinity: ListTileControlAffinity.leading,
            title: const Text('Skrati blokadu do prve rezervacije'),
            subtitle: Text(
              skraceno
                  ? 'Blokada će trajati do ${Formati.datumIVrijeme(_stvarniKrajBlokade)}, '
                        'kad počinje prva rezervacija.'
                  : 'Blokada se završava prije prve rezervacije, pa nema šta da se skrati.',
              style: const TextStyle(fontSize: 12),
            ),
          ),
          const SizedBox(height: Razmaci.m),
          for (final rezervacija in _pogodjene)
            _PogodjenaKartica(rezervacija: rezervacija),
        ],
        const SizedBox(height: Razmaci.l),
        const Divider(),
        const SizedBox(height: Razmaci.s),
        Row(
          children: [
            const Expanded(
              child: Text(
                'Vozilo je ipak ispravno?',
                style: TextStyle(color: Boje.tekstPrigusen, fontSize: 12.5),
              ),
            ),
            TextButton.icon(
              onPressed: () => _promijeniNacin(false),
              icon: const Icon(Icons.arrow_back, size: 17),
              label: const Text('Nazad na unos rezervacije'),
            ),
          ],
        ),
      ],
    );
  }

  Widget _unos() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const _Naslov('Klijent'),
        _BiracKlijenta(
          servis: _servis,
          naOdabir: (klijent) => setState(() {
            _klijent = klijent;
            _greska = null;
          }),
        ),
        if (_klijent != null && _klijent!.prepreka != null) ...[
          const SizedBox(height: Razmaci.s),
          Obavjestenje.upozorenje(
            '${_klijent!.prepreka} Server će ovakvu rezervaciju odbiti.',
          ),
        ],
        const SizedBox(height: Razmaci.xl),
        const _Naslov('Termin'),
        RedPolja(
          lijevo: _PoljeTrenutka(
            natpis: 'Preuzimanje',
            vrijednost: _od,
            naPritisak: () => _odaberiTrenutak(pocetak: true),
          ),
          desno: _PoljeTrenutka(
            natpis: 'Vraćanje',
            vrijednost: _do,
            naPritisak: () => _odaberiTrenutak(pocetak: false),
          ),
        ),
        if (widget.bufferSati > 0) ...[
          const SizedBox(height: Razmaci.s),
          Text(
            'Između dva najma vozilo treba ${Formati.broj(widget.bufferSati)} h pripreme, '
            'pa termin odmah uz tuđi najam neće proći.',
            style: const TextStyle(color: Boje.tekstPrigusen, fontSize: 12),
          ),
        ],
        const SizedBox(height: Razmaci.xl),
        const _Naslov('Dodatna oprema'),
        if (_vrsteOpreme.isEmpty)
          const Text(
            'Nema opreme u ponudi.',
            style: TextStyle(color: Boje.tekstPrigusen, fontSize: 12.5),
          )
        else
          for (final vrsta in _vrsteOpreme)
            _RedOpreme(
              vrsta: vrsta,
              kolicina: _oprema[vrsta.id] ?? 0,
              naPromjenu: (kolicina) => _promijeniOpremu(vrsta.id, kolicina),
            ),
        const SizedBox(height: Razmaci.xl),
        const _Naslov('Osiguranje'),
        DropdownButtonFormField<int?>(
          initialValue: _paketId,
          isExpanded: true,
          decoration: const InputDecoration(labelText: 'Paket osiguranja'),
          items: [
            const DropdownMenuItem<int?>(
              value: null,
              child: Text('Bez dodatnog osiguranja'),
            ),
            for (final paket in _paketi)
              DropdownMenuItem<int?>(
                value: paket.id,
                child: Text(
                  '${paket.naziv} · ${Formati.novac(paket.cijenaPoDanu)}/dan, '
                  'učešće ${Formati.novac(paket.iznosUcesca)}',
                ),
              ),
          ],
          onChanged: (id) {
            setState(() => _paketId = id);
            _preracunaj();
          },
        ),
      ],
    );
  }

  Widget _obracun() {
    final cijena = _cijena;

    return Container(
      padding: const EdgeInsets.all(Razmaci.l),
      decoration: BoxDecoration(
        color: Boje.platno,
        borderRadius: BorderRadius.circular(Zaobljenja.kartica),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              const Expanded(child: _Naslov('Obračun')),
              if (_racuna)
                const SizedBox(
                  width: 14,
                  height: 14,
                  child: CircularProgressIndicator(strokeWidth: 2),
                ),
            ],
          ),
          if (_greskaObracuna != null)
            Obavjestenje.greska(_greskaObracuna!)
          else if (cijena == null)
            const Text(
              'Računa se…',
              style: TextStyle(color: Boje.tekstPrigusen, fontSize: 12.5),
            )
          else ...[
            _RedIznosa(
              cijena.naplataPoSatu
                  ? 'Najam, ${cijena.brojSati} h × ${Formati.novac(cijena.satnaTarifa)}'
                  : 'Najam, ${cijena.brojDana} d × ${Formati.novac(cijena.dnevnaTarifa)}',
              Formati.novac(cijena.osnovicaNajma),
            ),
            if (cijena.mnozilac != 1)
              _RedIznosa(
                'Sezona${cijena.nazivSezone == null ? '' : ' (${cijena.nazivSezone})'}',
                '× ${cijena.mnozilac.toStringAsFixed(2)}',
              ),
            if (cijena.iznosPopusta > 0)
              _RedIznosa(
                'Popust ${Formati.postotak(cijena.procenatPopusta)}',
                '− ${Formati.novac(cijena.iznosPopusta)}',
              ),
            for (final stavka in cijena.oprema)
              _RedIznosa(
                '${stavka.naziv} × ${stavka.kolicina}',
                Formati.novac(stavka.iznos),
              ),
            if (cijena.iznosOsiguranja > 0)
              _RedIznosa(
                'Osiguranje${cijena.paketOsiguranjaNaziv == null ? '' : ' (${cijena.paketOsiguranjaNaziv})'}',
                Formati.novac(cijena.iznosOsiguranja),
              ),
            _RedIznosa('Depozit', Formati.novac(cijena.iznosDepozita)),
            const Divider(height: Razmaci.xl),
            _RedIznosa(
              'Ukupno',
              Formati.novac(cijena.ukupanIznos),
              istaknuto: true,
            ),
            const SizedBox(height: Razmaci.m),
            const Text(
              'Iznos računa server. Rezervacija drži termin dok klijent ne plati, '
              'a neplaćena se sama otkazuje kad rok istekne.',
              style: TextStyle(
                color: Boje.tekstPrigusen,
                fontSize: 11.5,
                height: 1.4,
              ),
            ),
          ],
        ],
      ),
    );
  }
}

/// Polje za pretragu klijenta, sa rezultatima ispod.
///
/// Kad pretraga ne uspije, to se kaze ispod polja. Prazna lista bi izgledala kao da
/// klijent ne postoji, a uzrok je zapravo odbijen zahtjev ili pao server.
class _BiracKlijenta extends StatefulWidget {
  const _BiracKlijenta({required this.servis, required this.naOdabir});

  final KalendarServis servis;
  final ValueChanged<KlijentZaOdabir?> naOdabir;

  @override
  State<_BiracKlijenta> createState() => _BiracKlijentaStanje();
}

class _BiracKlijentaStanje extends State<_BiracKlijenta> {
  String? _greska;

  Future<Iterable<KlijentZaOdabir>> _trazi(TextEditingValue vrijednost) async {
    final tekst = vrijednost.text.trim();

    // Prazno polje znaci da je odabir ponisten.
    if (tekst.isEmpty) {
      widget.naOdabir(null);

      return const Iterable<KlijentZaOdabir>.empty();
    }

    if (tekst.length < 2) {
      return const Iterable<KlijentZaOdabir>.empty();
    }

    try {
      final klijenti = await widget.servis.klijenti(tekst);

      if (mounted && _greska != null) {
        setState(() => _greska = null);
      }

      return klijenti;
    } on ApiGreska catch (greska) {
      if (mounted) {
        setState(() => _greska = greska.poruka);
      }

      return const Iterable<KlijentZaOdabir>.empty();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Autocomplete<KlijentZaOdabir>(
          displayStringForOption: (klijent) =>
              '${klijent.punoIme} · ${klijent.email}',
          optionsBuilder: _trazi,
          onSelected: widget.naOdabir,
          fieldViewBuilder: (context, kontroler, fokus, naPotvrdu) => TextField(
            controller: kontroler,
            focusNode: fokus,
            decoration: const InputDecoration(
              labelText: 'Ime, prezime, email ili telefon',
              prefixIcon: Icon(Icons.person_search_outlined, size: 20),
            ),
          ),
          optionsViewBuilder: (context, odaberi, opcije) => Align(
            alignment: Alignment.topLeft,
            child: Material(
              elevation: 4,
              borderRadius: BorderRadius.circular(Zaobljenja.dugme),
              child: ConstrainedBox(
                constraints: const BoxConstraints(
                  maxHeight: 280,
                  maxWidth: 440,
                ),
                child: ListView(
                  padding: EdgeInsets.zero,
                  shrinkWrap: true,
                  children: [
                    for (final klijent in opcije)
                      ListTile(
                        dense: true,
                        onTap: () => odaberi(klijent),
                        title: Text(klijent.punoIme),
                        subtitle: Text(
                          klijent.prepreka ?? klijent.email,
                          style: TextStyle(
                            color: klijent.prepreka == null
                                ? Boje.tekstPrigusen
                                : Boje.upozorenjeTekst,
                            fontSize: 12,
                          ),
                        ),
                        trailing: klijent.prepreka == null
                            ? const Icon(
                                Icons.check_circle_outline,
                                size: 18,
                                color: Boje.uspjeh,
                              )
                            : const Icon(
                                Icons.warning_amber_outlined,
                                size: 18,
                                color: Boje.upozorenje,
                              ),
                      ),
                  ],
                ),
              ),
            ),
          ),
        ),
        if (_greska != null) ...[
          const SizedBox(height: Razmaci.s),
          Text(
            'Pretraga nije uspjela: $_greska',
            style: const TextStyle(color: Boje.greskaTekst, fontSize: 12),
          ),
        ],
      ],
    );
  }
}

class _PoljeTrenutka extends StatelessWidget {
  const _PoljeTrenutka({
    required this.natpis,
    required this.vrijednost,
    required this.naPritisak,
  });

  final String natpis;
  final DateTime vrijednost;
  final VoidCallback naPritisak;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: naPritisak,
      borderRadius: BorderRadius.circular(Zaobljenja.polje),
      child: InputDecorator(
        decoration: InputDecoration(
          labelText: natpis,
          suffixIcon: const Icon(Icons.event_outlined, size: 18),
        ),
        child: Text(Formati.datumIVrijeme(vrijednost)),
      ),
    );
  }
}

class _RedOpreme extends StatelessWidget {
  const _RedOpreme({
    required this.vrsta,
    required this.kolicina,
    required this.naPromjenu,
  });

  final VrstaOpreme vrsta;
  final int kolicina;
  final ValueChanged<int> naPromjenu;

  static const _najvise = 5;

  @override
  Widget build(BuildContext context) {
    final cijena = vrsta.cijenaPoDanu != null
        ? '${Formati.novac(vrsta.cijenaPoDanu!)} po danu'
        : vrsta.fiksnaCijena != null
        ? '${Formati.novac(vrsta.fiksnaCijena!)} jednokratno'
        : '';

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(vrsta.naziv, style: const TextStyle(fontSize: 13)),
                Text(
                  cijena,
                  style: const TextStyle(
                    color: Boje.tekstPrigusen,
                    fontSize: 11.5,
                  ),
                ),
              ],
            ),
          ),
          IconButton(
            tooltip: 'Manje',
            onPressed: kolicina > 0 ? () => naPromjenu(kolicina - 1) : null,
            icon: const Icon(Icons.remove_circle_outline, size: 20),
          ),
          SizedBox(
            width: 24,
            child: Text(
              '$kolicina',
              textAlign: TextAlign.center,
              style: const TextStyle(fontWeight: FontWeight.w600),
            ),
          ),
          IconButton(
            tooltip: 'Više',
            onPressed: kolicina < _najvise
                ? () => naPromjenu(kolicina + 1)
                : null,
            icon: const Icon(Icons.add_circle_outline, size: 20),
          ),
        ],
      ),
    );
  }
}

class _Naslov extends StatelessWidget {
  const _Naslov(this.tekst);

  final String tekst;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: Razmaci.s),
      child: Text(
        tekst,
        style: const TextStyle(fontSize: 13.5, fontWeight: FontWeight.w600),
      ),
    );
  }
}

class _RedIznosa extends StatelessWidget {
  const _RedIznosa(this.natpis, this.vrijednost, {this.istaknuto = false});

  final String natpis;
  final String vrijednost;
  final bool istaknuto;

  @override
  Widget build(BuildContext context) {
    final stil = TextStyle(
      fontSize: istaknuto ? 15 : 12.5,
      fontWeight: istaknuto ? FontWeight.w700 : FontWeight.w400,
      color: istaknuto ? Boje.tekst : Boje.tekstBlazi,
    );

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(child: Text(natpis, style: stil)),
          const SizedBox(width: Razmaci.s),
          Text(vrijednost, style: stil),
        ],
      ),
    );
  }
}

/// Rezervacija koju blokada pogadja, sa kontaktom klijenta.
///
/// Telefon i email su tu jer blokada nikoga ne obavjestava sama - uposlenik koji
/// vozilo skida iz ponude mora imati koga nazvati, odmah, bez trazenja po drugim
/// ekranima.
class _PogodjenaKartica extends StatelessWidget {
  const _PogodjenaKartica({required this.rezervacija});

  final PogodjenaRezervacija rezervacija;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: Razmaci.s),
      padding: const EdgeInsets.all(Razmaci.m),
      decoration: BoxDecoration(
        color: Boje.platno,
        borderRadius: BorderRadius.circular(Zaobljenja.dugme),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Text(
                      rezervacija.broj,
                      style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(width: Razmaci.s),
                    StatusnaPilula.rezervacija(rezervacija.status),
                  ],
                ),
                const SizedBox(height: 2),
                Text(
                  '${Formati.datumIVrijeme(rezervacija.datumOd)} – '
                  '${Formati.datumIVrijeme(rezervacija.datumDo)}',
                  style: const TextStyle(
                    color: Boje.tekstPrigusen,
                    fontSize: 12,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(width: Razmaci.m),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                rezervacija.klijentImePrezime ?? '',
                style: const TextStyle(fontSize: 12.5),
              ),
              Text(
                rezervacija.klijentTelefon ?? rezervacija.klijentEmail ?? '',
                style: const TextStyle(color: Boje.tekstPrigusen, fontSize: 12),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
