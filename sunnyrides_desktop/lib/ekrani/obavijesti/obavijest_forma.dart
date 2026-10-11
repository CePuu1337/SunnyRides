import 'dart:typed_data';

import 'package:file_selector/file_selector.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/recenzija.dart';
import '../../servisi/moderacija_servis.dart';
import '../../widgeti/dijalog_forme.dart';

/// Unos i izmjena obavijesti, sa slikom i datumom objave.
///
/// Klijenti vide samo obavijest koja ima sliku, pa server aktivnu obavijest bez slike
/// odbija. Slika se zato bira na formi, prije snimanja, i kod nove obavijesti: forma
/// zapis prvo snimi kao neaktivan, prilozi sliku, pa ga tek onda aktivira.
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

  /// Zapis na serveru. Kod nove obavijesti nastaje pri prvom snimanju, pa ako neki
  /// od sljedecih koraka pukne, ponovno snimanje mijenja isti zapis umjesto da
  /// pravi novi.
  Obavijest? _zapis;

  /// Slika koja je vec na serveru.
  String? _slikaUrl;

  /// Slika odabrana na formi, a jos neposlana. Bajtovi se citaju jednom, pri odabiru.
  _OdabranaSlika? _novaSlika;

  /// Postojeca slika je oznacena za uklanjanje.
  bool _ukloniPostojecu = false;

  bool _snimanje = false;
  String? _greska;
  String? _greskaSlike;

  bool get _jeIzmjena => widget.obavijest != null;

  bool get _imaSliku =>
      _novaSlika != null || (_slikaUrl != null && !_ukloniPostojecu);

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
    _zapis = obavijest;
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
    final formaIspravna = _forma.currentState!.validate();

    setState(() {
      _greskaSlike = _aktivna && !_imaSliku
          ? 'Aktivna obavijest mora imati sliku. Dodajte sliku (JPG, PNG ili '
                'WEBP, do 5 MB) ili isključite „Aktivna".'
          : null;
    });

    if (!formaIspravna || _greskaSlike != null) {
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
      // Redoslijed je bitan: slika mora biti na serveru prije aktiviranja, a
      // uklanja se tek kad je obavijest vec neaktivna.
      final zapis =
          _zapis ?? await _servis.dodaj({...zahtjev, 'aktivna': false});
      _zapis = zapis;

      final nova = _novaSlika;

      if (nova != null) {
        final saSlikom = await _servis.postaviSliku(
          zapis.id,
          imeFajla: nova.naziv,
          sadrzaj: nova.bajtovi,
        );

        _novaSlika = null;
        _slikaUrl = saSlikom.slikaUrl;
        _ukloniPostojecu = false;
      }

      await _servis.izmijeni(zapis.id, zahtjev);

      if (_ukloniPostojecu && _slikaUrl != null) {
        await _servis.ukloniSliku(zapis.id);

        _slikaUrl = null;
        _ukloniPostojecu = false;
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

  Future<void> _odaberiSliku() async {
    const vrste = XTypeGroup(
      label: 'Slike',
      extensions: ['jpg', 'jpeg', 'png', 'webp'],
    );

    final fajl = await openFile(acceptedTypeGroups: [vrste]);

    if (fajl == null) {
      return;
    }

    final bajtovi = await fajl.readAsBytes();

    if (!mounted) {
      return;
    }

    setState(() {
      _novaSlika = _OdabranaSlika(fajl.name, bajtovi);
      _greskaSlike = null;
    });
  }

  void _ukloniSliku() {
    setState(() {
      if (_novaSlika != null) {
        _novaSlika = null;
      } else {
        _ukloniPostojecu = true;
      }
    });
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
              bajtovi: _novaSlika?.bajtovi,
              url: _ukloniPostojecu
                  ? null
                  : _okruzenje.apsolutnaSlika(_slikaUrl),
              greska: _greskaSlike,
              uToku: _snimanje,
              naDodavanje: _odaberiSliku,
              naUklanjanje: _ukloniSliku,
            ),
          ],
        ),
      ),
    );
  }
}

class _OdabranaSlika {
  const _OdabranaSlika(this.naziv, this.bajtovi);

  final String naziv;
  final Uint8List bajtovi;
}

class _Slika extends StatelessWidget {
  const _Slika({
    required this.bajtovi,
    required this.url,
    required this.greska,
    required this.uToku,
    required this.naDodavanje,
    required this.naUklanjanje,
  });

  /// Novoodabrana slika, jos neposlana.
  final Uint8List? bajtovi;

  /// Slika koja je vec na serveru.
  final String? url;

  final String? greska;
  final bool uToku;
  final VoidCallback naDodavanje;
  final VoidCallback naUklanjanje;

  @override
  Widget build(BuildContext context) {
    final imaSliku = bajtovi != null || url != null;

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
            if (imaSliku)
              TextButton.icon(
                onPressed: uToku ? null : naUklanjanje,
                icon: const Icon(Icons.delete_outline, size: 17),
                label: const Text('Ukloni'),
              ),
            const SizedBox(width: Razmaci.s),
            OutlinedButton.icon(
              onPressed: uToku ? null : naDodavanje,
              icon: const Icon(Icons.add_photo_alternate_outlined, size: 17),
              label: Text(imaSliku ? 'Zamijeni' : 'Dodaj sliku'),
            ),
          ],
        ),
        const SizedBox(height: Razmaci.m),
        if (bajtovi != null)
          ClipRRect(
            borderRadius: BorderRadius.circular(Zaobljenja.dugme),
            child: Image.memory(bajtovi!, height: 150, fit: BoxFit.cover),
          )
        else if (url != null)
          ClipRRect(
            borderRadius: BorderRadius.circular(Zaobljenja.dugme),
            child: Image.network(url!, height: 150, fit: BoxFit.cover),
          )
        else
          const Text(
            'Obavijest nema sliku. Bez slike se može sačuvati samo kao neaktivna.',
            style: TextStyle(color: Boje.tekstPrigusen, fontSize: 12.5),
          ),
        if (greska != null) ...[
          const SizedBox(height: Razmaci.s),
          Text(
            greska!,
            style: const TextStyle(color: Boje.greskaTekst, fontSize: 12),
          ),
        ],
      ],
    );
  }
}
