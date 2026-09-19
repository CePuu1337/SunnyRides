import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/kalendar.dart';
import '../../servisi/kalendar_servis.dart';
import '../../widgeti/dijalog_forme.dart';
import '../../widgeti/obavjestenje.dart';

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

  @override
  void initState() {
    super.initState();

    _servis = KalendarServis(context.read<ApiKlijent>());

    _postaviPocetniTermin();
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

      Navigator.of(context).pop(true);
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
      naslov: 'Ručni unos rezervacije',
      podnaslov:
          '${widget.vozilo.vozilo} · ${widget.vozilo.registarskaOznaka} · '
          '${widget.vozilo.poslovnica}',
      greska: _greska,
      uToku: _snimanje,
      natpisPotvrde: 'Unesi rezervaciju',
      naSnimanje: _sacuvaj,
      sirina: 820,
      dijete: _ucitavanje
          ? const Padding(
              padding: EdgeInsets.all(Razmaci.xxl),
              child: Center(child: CircularProgressIndicator()),
            )
          : Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(flex: 3, child: _unos()),
                const SizedBox(width: Razmaci.xl),
                Expanded(flex: 2, child: _obracun()),
              ],
            ),
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
