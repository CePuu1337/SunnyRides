import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/kalendar.dart';
import '../../servisi/kalendar_servis.dart';
import '../../widgeti/dijalog_forme.dart';
import '../../widgeti/potvrda.dart';
import 'pogodjene_rezervacije.dart';

/// Detalji jedne blokade, sa izmjenom perioda ili razloga i uklanjanjem.
///
/// Otvara se klikom na blok blokade u kalendaru. Izmjena prolazi istu provjeru kao
/// unos: kad se period produži preko neke rezervacije, uposlenik to vidi i bira da
/// skrati blokadu, prebaci rezervaciju na drugo vozilo ili je otkaže. Bez toga bi
/// izmjena bila zaobilazni put oko pravila koje važi pri unosu.
///
/// Kalendar se osvježava poslije svakog zatvaranja ovog dijaloga, jer zamjena ili
/// otkazivanje pogođene rezervacije mijenja kalendar i kad se izmjena ne sačuva.
class BlokadaDijalog extends StatefulWidget {
  const BlokadaDijalog({
    super.key,
    required this.blokadaId,
    required this.vozilo,
  });

  final int blokadaId;
  final RedKalendara vozilo;

  @override
  State<BlokadaDijalog> createState() => _BlokadaDijalogStanje();
}

class _BlokadaDijalogStanje extends State<BlokadaDijalog> {
  late final KalendarServis _servis;
  final _razlog = TextEditingController();

  BlokadaVozila? _blokada;
  bool _ucitavanje = true;
  bool _izmjena = false;

  late DateTime _od;
  late DateTime _do;
  String? _greskaRazloga;

  List<PogodjenaRezervacija> _pogodjene = const [];
  bool _skrati = true;
  bool _provjeravam = false;
  int _zadnjaProvjera = 0;

  bool _snimanje = false;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = KalendarServis(context.read<ApiKlijent>());
    _ucitaj();
  }

  @override
  void dispose() {
    _razlog.dispose();
    super.dispose();
  }

  Future<void> _ucitaj() async {
    try {
      final blokada = await _servis.blokada(widget.blokadaId);

      if (!mounted) {
        return;
      }

      setState(() {
        _blokada = blokada;
        _od = blokada.datumOd.toLocal();
        _do = blokada.datumDo.toLocal();
        _razlog.text = blokada.razlog;
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

  DateTime get _krajZaUpis =>
      krajBlokade(doDatuma: _do, pogodjene: _pogodjene, skrati: _skrati);

  void _pocniIzmjenu() {
    setState(() {
      _izmjena = true;
      _greska = null;
    });

    _provjeriPogodjene();
  }

  Future<void> _provjeriPogodjene() async {
    final redniBroj = ++_zadnjaProvjera;

    setState(() => _provjeravam = true);

    try {
      final pogodjene = await _servis.pogodjeneRezervacije(
        voziloId: widget.vozilo.voziloId,
        od: _od,
        doDatuma: _do,
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

  Future<void> _odaberiTrenutak({required bool pocetak}) async {
    final trenutni = pocetak ? _od : _do;

    final datum = await showDatePicker(
      context: context,
      initialDate: trenutni,
      firstDate: DateTime.now().subtract(const Duration(days: 365)),
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
      _greska = null;

      if (pocetak) {
        _od = novi;

        if (!_do.isAfter(_od)) {
          _do = _od.add(const Duration(days: 1));
        }
      } else {
        _do = novi;
      }
    });

    _provjeriPogodjene();
  }

  Future<void> _zamijeniVozilo(PogodjenaRezervacija rezervacija) async {
    if (await zamijeniVoziloPogodjene(context, rezervacija) && mounted) {
      await _provjeriPogodjene();
    }
  }

  Future<void> _otkaziRezervaciju(PogodjenaRezervacija rezervacija) async {
    try {
      if (await otkaziPogodjenu(context, rezervacija) && mounted) {
        await _provjeriPogodjene();
      }
    } on ApiGreska catch (greska) {
      if (mounted) {
        setState(() => _greska = greska.poruka);
      }
    }
  }

  Future<void> _sacuvaj() async {
    final razlog = _razlog.text.trim();

    if (razlog.length < 3) {
      setState(() => _greskaRazloga = 'Upišite razlog blokade, bar tri znaka.');

      return;
    }

    if (!_do.isAfter(_od)) {
      setState(() => _greska = 'Kraj blokade mora biti poslije početka.');

      return;
    }

    final kraj = _krajZaUpis;

    if (!kraj.isAfter(_od)) {
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
      _greskaRazloga = null;
    });

    try {
      await _servis.izmijeniBlokadu(
        widget.blokadaId,
        od: _od,
        doDatuma: kraj,
        razlog: razlog,
      );

      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Blokada je uspješno izmijenjena.')),
      );
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

  Future<void> _ukloni() async {
    final blokada = _blokada;

    if (blokada == null) {
      return;
    }

    final potvrdjeno = await potvrdiRadnju(
      context,
      naslov: 'Ukloniti blokadu?',
      tekst:
          '${widget.vozilo.vozilo} (${widget.vozilo.registarskaOznaka}) više neće '
          'biti blokirano od ${Formati.datumIVrijeme(blokada.datumOd)} do '
          '${Formati.datumIVrijeme(blokada.datumDo)}, pa se u tom periodu ponovo '
          'nudi klijentima. Blokada se briše i ne može se vratiti.',
      potvrda: 'Da, ukloni',
      opasna: true,
    );

    if (!potvrdjeno || !mounted) {
      return;
    }

    setState(() {
      _snimanje = true;
      _greska = null;
    });

    try {
      await _servis.ukloniBlokadu(widget.blokadaId);

      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context)
          .showSnackBar(const SnackBar(content: Text('Blokada je uklonjena.')));
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
    final blokada = _blokada;

    return DijalogForme(
      naslov: _izmjena ? 'Izmjena blokade' : 'Blokada vozila',
      podnaslov:
          '${widget.vozilo.vozilo} · ${widget.vozilo.registarskaOznaka} · '
          '${widget.vozilo.poslovnica}',
      greska: _greska,
      uToku: _snimanje,
      sirina: 640,
      potvrdaOmogucena: blokada != null && !_provjeravam,
      natpisPotvrde: _izmjena ? 'Sačuvaj izmjene' : 'Izmijeni',
      naSnimanje: _izmjena ? _sacuvaj : _pocniIzmjenu,
      dijete: _ucitavanje
          ? const Padding(
              padding: EdgeInsets.all(Razmaci.xxl),
              child: Center(child: CircularProgressIndicator()),
            )
          : blokada == null
          ? const SizedBox.shrink()
          : _izmjena
          ? _formaIzmjene()
          : _detalji(blokada),
    );
  }

  Widget _detalji(BlokadaVozila blokada) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _Red('Od', Formati.datumIVrijeme(blokada.datumOd)),
        _Red('Do', Formati.datumIVrijeme(blokada.datumDo)),
        _Red('Razlog', blokada.razlog),
        _Red(
          'Unio',
          '${blokada.kreiraoKorisnikIme ?? '—'}, '
              '${Formati.datumIVrijeme(blokada.datumKreiranja)}',
        ),
        const SizedBox(height: Razmaci.l),
        const Divider(),
        const SizedBox(height: Razmaci.s),
        Row(
          children: [
            const Expanded(
              child: Text(
                'Vozilo je popravljeno ranije, ili je blokada unesena greškom?',
                style: TextStyle(color: Boje.tekstPrigusen, fontSize: 12.5),
              ),
            ),
            TextButton.icon(
              onPressed: _snimanje ? null : _ukloni,
              style: TextButton.styleFrom(foregroundColor: Boje.greskaTekst),
              icon: const Icon(Icons.delete_outline, size: 17),
              label: const Text('Ukloni blokadu'),
            ),
          ],
        ),
      ],
    );
  }

  Widget _formaIzmjene() {
    final kraj = _krajZaUpis;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        RedPolja(
          lijevo: PoljeTrenutka(
            natpis: 'Od',
            vrijednost: _od,
            naPritisak: () => _odaberiTrenutak(pocetak: true),
          ),
          desno: PoljeTrenutka(
            natpis: 'Do',
            vrijednost: _do,
            naPritisak: () => _odaberiTrenutak(pocetak: false),
          ),
        ),
        const SizedBox(height: Razmaci.l),
        TextField(
          controller: _razlog,
          maxLines: 2,
          maxLength: 500,
          onChanged: (_) {
            if (_greskaRazloga != null) {
              setState(() => _greskaRazloga = null);
            }
          },
          decoration: InputDecoration(
            labelText: 'Razlog',
            alignLabelWithHint: true,
            errorText: _greskaRazloga,
          ),
        ),
        const SizedBox(height: Razmaci.s),
        PogodjeneRezervacije(
          provjeravam: _provjeravam,
          pogodjene: _pogodjene,
          skrati: _skrati,
          krajBlokade: kraj,
          trazeniKraj: _do,
          naSkracivanje: (skrati) => setState(() => _skrati = skrati),
          naZamjenu: _zamijeniVozilo,
          naOtkazivanje: _otkaziRezervaciju,
        ),
      ],
    );
  }
}

class _Red extends StatelessWidget {
  const _Red(this.natpis, this.vrijednost);

  final String natpis;
  final String vrijednost;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: Razmaci.xs),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 90,
            child: Text(
              natpis,
              style: const TextStyle(color: Boje.tekstPrigusen, fontSize: 13),
            ),
          ),
          Expanded(
            child: Text(vrijednost, style: const TextStyle(fontSize: 13)),
          ),
        ],
      ),
    );
  }
}
