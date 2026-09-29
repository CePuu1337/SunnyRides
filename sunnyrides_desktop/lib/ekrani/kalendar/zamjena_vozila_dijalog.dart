import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/kalendar.dart';
import '../../modeli/vozilo.dart';
import '../../servisi/rezervacija_servis.dart';
import '../../widgeti/dijalog_forme.dart';
import '../../widgeti/obavjestenje.dart';
import '../../widgeti/slicica.dart';

/// Zamjena vozila na rezervaciji koju pogadja blokada.
///
/// Ponudjena vozila bira server: slobodna u terminu rezervacije, istog tipa, iste ili
/// vise dnevne tarife, u istom gradu i dozvoljena klijentu po dozvoli. Dijalog samo
/// prikazuje tu listu i salje izbor - pravila se ne ponavljaju ovdje, pa se ne mogu
/// razici sa onima koje server stvarno provjerava.
class ZamjenaVozilaDijalog extends StatefulWidget {
  const ZamjenaVozilaDijalog({super.key, required this.rezervacija});

  final PogodjenaRezervacija rezervacija;

  @override
  State<ZamjenaVozilaDijalog> createState() => _ZamjenaVozilaDijalogStanje();
}

class _ZamjenaVozilaDijalogStanje extends State<ZamjenaVozilaDijalog> {
  late final RezervacijaServis _servis;

  final _napomena = TextEditingController();

  List<Vozilo> _kandidati = const [];
  int? _odabrano;

  bool _ucitavanje = true;
  bool _snimanje = false;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = RezervacijaServis(context.read<ApiKlijent>());
    _ucitaj();
  }

  @override
  void dispose() {
    _napomena.dispose();
    super.dispose();
  }

  Future<void> _ucitaj() async {
    try {
      final kandidati = await _servis.zamjenskaVozila(widget.rezervacija.id);

      if (!mounted) {
        return;
      }

      setState(() {
        _kandidati = kandidati;
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

  Future<void> _zamijeni() async {
    final odabrano = _kandidati.where((x) => x.id == _odabrano).firstOrNull;

    if (odabrano == null) {
      setState(() => _greska = 'Odaberite zamjensko vozilo.');

      return;
    }

    final potvrda = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Zamjena vozila'),
        content: Text(
          'Rezervacija ${widget.rezervacija.broj} prelazi na '
          '${odabrano.puniNaziv} (${odabrano.registarskaOznaka}). Cijena ostaje '
          'ista, a klijent dobija obavještenje i email o zamjeni.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Odustani'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Zamijeni'),
          ),
        ],
      ),
    );

    if (potvrda != true || !mounted) {
      return;
    }

    setState(() {
      _snimanje = true;
      _greska = null;
    });

    try {
      final napomena = _napomena.text.trim();

      await _servis.zamijeniVozilo(
        widget.rezervacija.id,
        novoVoziloId: odabrano.id,
        napomena: napomena.isEmpty ? null : napomena,
      );

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
      naslov: 'Zamjensko vozilo',
      podnaslov:
          '${widget.rezervacija.broj} · '
          '${Formati.datumIVrijeme(widget.rezervacija.datumOd)} – '
          '${Formati.datumIVrijeme(widget.rezervacija.datumDo)}',
      greska: _greska,
      uToku: _snimanje,
      natpisPotvrde: 'Zamijeni vozilo',
      potvrdaOmogucena: _odabrano != null,
      naSnimanje: _zamijeni,
      sirina: 620,
      dijete: _ucitavanje
          ? const Padding(
              padding: EdgeInsets.all(Razmaci.xxl),
              child: Center(child: CircularProgressIndicator()),
            )
          : Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Obavjestenje.info(
                  'Ponuđena su vozila slobodna u terminu rezervacije, istog tipa, '
                  'iste ili više klase, u istom gradu, koja klijent smije voziti. '
                  'Prva su vozila iz iste poslovnice.',
                ),
                const SizedBox(height: Razmaci.l),
                if (_kandidati.isEmpty)
                  Obavjestenje.upozorenje(
                    'Nema slobodnog vozila iste ili više klase za ovaj termin. '
                    'Rezervaciju možete otkazati uz puni povrat.',
                  )
                else
                  RadioGroup<int>(
                    groupValue: _odabrano,
                    onChanged: (id) => setState(() {
                      _odabrano = id;
                      _greska = null;
                    }),
                    child: Column(
                      children: [
                        for (final vozilo in _kandidati)
                          _Kandidat(
                            vozilo: vozilo,
                            odabran: vozilo.id == _odabrano,
                          ),
                      ],
                    ),
                  ),
                const SizedBox(height: Razmaci.l),
                TextField(
                  controller: _napomena,
                  maxLines: 2,
                  maxLength: 500,
                  decoration: const InputDecoration(
                    labelText: 'Napomena za klijenta (nije obavezna)',
                    hintText:
                        'Rezervisano vozilo je na servisu zbog kvara kočnica.',
                    alignLabelWithHint: true,
                  ),
                ),
              ],
            ),
    );
  }
}

class _Kandidat extends StatelessWidget {
  const _Kandidat({required this.vozilo, required this.odabran});

  final Vozilo vozilo;
  final bool odabran;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: Razmaci.s),
      decoration: BoxDecoration(
        color: odabran ? Boje.primarnaSvijetla : Boje.platno,
        borderRadius: BorderRadius.circular(Zaobljenja.dugme),
      ),
      child: RadioListTile<int>(
        value: vozilo.id,
        secondary: Slicica(
          putanja: vozilo.thumbnailUrl,
          zamjenskaIkona: Icons.two_wheeler_outlined,
        ),
        title: Text(
          '${vozilo.puniNaziv} · ${vozilo.registarskaOznaka}',
          style: const TextStyle(fontSize: 13.5, fontWeight: FontWeight.w600),
        ),
        subtitle: Text(
          [
            vozilo.poslovnicaNaziv,
            '${Formati.novac(vozilo.dnevnaTarifa)} / dan',
            vozilo.pogon,
          ].where((x) => x != null && x.isNotEmpty).join(' · '),
          style: const TextStyle(fontSize: 12),
        ),
      ),
    );
  }
}
