import 'package:file_selector/file_selector.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/recenzija.dart';
import '../../servisi/moderacija_servis.dart';
import '../../widgeti/dijalog_forme.dart';

/// Unos i izmjena obavijesti, sa slikom i datumom objave.
class ObavijestForma extends StatefulWidget {
  const ObavijestForma({super.key, this.obavijest});

  final Obavijest? obavijest;

  @override
  State<ObavijestForma> createState() => _ObavijestFormaStanje();
}

class _ObavijestFormaStanje extends State<ObavijestForma> {
  final _forma = GlobalKey<FormState>();
  late final TextEditingController _naslov;
  late final TextEditingController _tekst;

  late final ObavijestServis _servis;
  late final Okruzenje _okruzenje;

  late DateTime _datumObjave;
  late bool _aktivna;

  String? _slikaUrl;
  bool _snimanje = false;
  String? _greska;

  bool get _jeIzmjena => widget.obavijest != null;

  @override
  void initState() {
    super.initState();

    _servis = ObavijestServis(context.read<ApiKlijent>());
    _okruzenje = context.read<Okruzenje>();

    final obavijest = widget.obavijest;

    _naslov = TextEditingController(text: obavijest?.naslov ?? '');
    _tekst = TextEditingController(text: obavijest?.tekst ?? '');
    _datumObjave = obavijest?.datumObjave.toLocal() ?? DateTime.now();
    _aktivna = obavijest?.aktivna ?? true;
    _slikaUrl = obavijest?.slikaUrl;
  }

  @override
  void dispose() {
    _naslov.dispose();
    _tekst.dispose();
    super.dispose();
  }

  Future<void> _odaberiDatum() async {
    final datum = await showDatePicker(
      context: context,
      initialDate: _datumObjave,
      firstDate: DateTime.now().subtract(const Duration(days: 365)),
      lastDate: DateTime.now().add(const Duration(days: 365)),
      helpText: 'Kad obavijest postaje vidljiva',
    );

    if (datum == null || !mounted) {
      return;
    }

    final vrijeme = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(_datumObjave),
      builder: (context, dijete) => MediaQuery(
        data: MediaQuery.of(context).copyWith(alwaysUse24HourFormat: true),
        child: dijete!,
      ),
    );

    if (vrijeme == null) {
      return;
    }

    setState(() {
      _datumObjave = DateTime(
        datum.year,
        datum.month,
        datum.day,
        vrijeme.hour,
        vrijeme.minute,
      );
    });
  }

  Future<void> _sacuvaj() async {
    if (!_forma.currentState!.validate()) {
      return;
    }

    setState(() {
      _snimanje = true;
      _greska = null;
    });

    final zahtjev = <String, dynamic>{
      'naslov': _naslov.text.trim(),
      'tekst': _tekst.text.trim(),
      'datumObjave': _datumObjave.toUtc().toIso8601String(),
      'aktivna': _aktivna,
    };

    try {
      if (_jeIzmjena) {
        await _servis.izmijeni(widget.obavijest!.id, zahtjev);
      } else {
        await _servis.dodaj(zahtjev);
      }

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

  Future<void> _dodajSliku() async {
    const vrste = XTypeGroup(
      label: 'Slike',
      extensions: ['jpg', 'jpeg', 'png', 'webp'],
    );

    final fajl = await openFile(acceptedTypeGroups: [vrste]);

    if (fajl == null) {
      return;
    }

    setState(() {
      _snimanje = true;
      _greska = null;
    });

    try {
      final sadrzaj = await fajl.readAsBytes();

      final obavijest = await _servis.postaviSliku(
        widget.obavijest!.id,
        imeFajla: fajl.name,
        sadrzaj: sadrzaj,
      );

      if (!mounted) {
        return;
      }

      setState(() => _slikaUrl = obavijest.slikaUrl);
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() => _greska = greska.poruka);
    } finally {
      if (mounted) {
        setState(() => _snimanje = false);
      }
    }
  }

  Future<void> _ukloniSliku() async {
    setState(() => _snimanje = true);

    try {
      await _servis.ukloniSliku(widget.obavijest!.id);

      if (mounted) {
        setState(() => _slikaUrl = null);
      }
    } on ApiGreska catch (greska) {
      if (mounted) {
        setState(() => _greska = greska.poruka);
      }
    } finally {
      if (mounted) {
        setState(() => _snimanje = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final zakazana = _datumObjave.isAfter(DateTime.now());

    return DijalogForme(
      naslov: _jeIzmjena ? 'Izmjena obavijesti' : 'Nova obavijest',
      podnaslov: 'Objava koju klijenti vide na početnom ekranu',
      greska: _greska,
      uToku: _snimanje,
      naSnimanje: _sacuvaj,
      sirina: 680,
      dijete: Form(
        key: _forma,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextFormField(
              controller: _naslov,
              decoration: const InputDecoration(labelText: 'Naslov'),
              validator: (vrijednost) {
                final tekst = vrijednost?.trim() ?? '';

                if (tekst.length < 3 || tekst.length > 150) {
                  return 'Naslov mora imati između 3 i 150 znakova.';
                }

                return null;
              },
            ),
            const SizedBox(height: Razmaci.l),
            TextFormField(
              controller: _tekst,
              maxLines: 6,
              decoration: const InputDecoration(
                labelText: 'Tekst obavijesti',
                alignLabelWithHint: true,
              ),
              validator: (vrijednost) {
                final tekst = vrijednost?.trim() ?? '';

                if (tekst.length < 10) {
                  return 'Napišite obavijest, bar deset znakova.';
                }

                return null;
              },
            ),
            const SizedBox(height: Razmaci.l),
            RedPolja(
              lijevo: InkWell(
                onTap: _odaberiDatum,
                borderRadius: BorderRadius.circular(Zaobljenja.polje),
                child: InputDecorator(
                  decoration: const InputDecoration(
                    labelText: 'Datum objave',
                    suffixIcon: Icon(Icons.event_outlined, size: 18),
                  ),
                  child: Text(Formati.datumIVrijeme(_datumObjave)),
                ),
              ),
              desno: SwitchListTile(
                value: _aktivna,
                onChanged: (vrijednost) =>
                    setState(() => _aktivna = vrijednost),
                contentPadding: EdgeInsets.zero,
                title: const Text('Aktivna'),
                subtitle: const Text(
                  'Neaktivna se ne prikazuje bez obzira na datum.',
                  style: TextStyle(fontSize: 11.5),
                ),
              ),
            ),
            if (zakazana) ...[
              const SizedBox(height: Razmaci.s),
              Text(
                'Datum objave je u budućnosti, pa je obavijest zakazana — klijenti je '
                'do tada ne vide.',
                style: const TextStyle(
                  color: Boje.upozorenjeTekst,
                  fontSize: 12,
                ),
              ),
            ],
            const SizedBox(height: Razmaci.xl),
            const Divider(),
            const SizedBox(height: Razmaci.m),
            _Slika(
              // Slika se veze za postojeci zapis, pa se kod nove obavijesti dodaje
              // tek nakon prvog snimanja. Bez zapisa nema ni cemu da se prilozi.
              dostupno: _jeIzmjena,
              url: _okruzenje.apsolutnaSlika(_slikaUrl),
              uToku: _snimanje,
              naDodavanje: _dodajSliku,
              naUklanjanje: _ukloniSliku,
            ),
          ],
        ),
      ),
    );
  }
}

class _Slika extends StatelessWidget {
  const _Slika({
    required this.dostupno,
    required this.url,
    required this.uToku,
    required this.naDodavanje,
    required this.naUklanjanje,
  });

  final bool dostupno;
  final String? url;
  final bool uToku;
  final VoidCallback naDodavanje;
  final VoidCallback naUklanjanje;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            const Expanded(
              child: Text(
                'Slika obavijesti',
                style: TextStyle(fontSize: 13.5, fontWeight: FontWeight.w600),
              ),
            ),
            if (dostupno) ...[
              if (url != null)
                TextButton.icon(
                  onPressed: uToku ? null : naUklanjanje,
                  icon: const Icon(Icons.delete_outline, size: 17),
                  label: const Text('Ukloni'),
                ),
              const SizedBox(width: Razmaci.s),
              OutlinedButton.icon(
                onPressed: uToku ? null : naDodavanje,
                icon: const Icon(Icons.add_photo_alternate_outlined, size: 17),
                label: Text(url == null ? 'Dodaj sliku' : 'Zamijeni'),
              ),
            ],
          ],
        ),
        const SizedBox(height: Razmaci.m),
        if (!dostupno)
          const Text(
            'Sliku možete dodati nakon što obavijest sačuvate.',
            style: TextStyle(color: Boje.tekstPrigusen, fontSize: 12.5),
          )
        else if (url == null)
          const Text(
            'Obavijest nema sliku.',
            style: TextStyle(color: Boje.tekstPrigusen, fontSize: 12.5),
          )
        else
          ClipRRect(
            borderRadius: BorderRadius.circular(Zaobljenja.dugme),
            child: Image.network(url!, height: 150, fit: BoxFit.cover),
          ),
      ],
    );
  }
}
