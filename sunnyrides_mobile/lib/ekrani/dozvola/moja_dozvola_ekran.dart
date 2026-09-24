import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../servisi/profil_servis.dart';
import '../../widgeti/obavjestenje.dart';
import 'prijava_dozvole_ekran.dart';

/// Vlastita vozacka dozvola: podaci, status i fotografije obje strane.
///
/// Dozvola se ne moze odobriti dok nisu prilozene i prednja i zadnja strana -
/// kategorije stoje na zadnjoj, pa uposlenik bez nje nema sta provjeriti.
class MojaDozvolaEkran extends StatefulWidget {
  const MojaDozvolaEkran({super.key});

  @override
  State<MojaDozvolaEkran> createState() => _MojaDozvolaEkranStanje();
}

class _MojaDozvolaEkranStanje extends State<MojaDozvolaEkran> {
  late final ProfilServis _servis;
  final _birac = ImagePicker();

  VozackaDozvola? _dozvola;
  bool _ucitavanje = true;
  StranaDozvole? _uToku;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = ProfilServis(context.read<ApiKlijent>());
    _ucitaj();
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      final dozvola = await _servis.mojaDozvola();

      if (!mounted) {
        return;
      }

      setState(() {
        _dozvola = dozvola;
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

  Future<void> _posaljiFotografiju(StranaDozvole strana) async {
    final izvor = await showModalBottomSheet<ImageSource>(
      context: context,
      showDragHandle: true,
      builder: (context) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: const Text('Fotografiraj'),
              onTap: () => Navigator.of(context).pop(ImageSource.camera),
            ),
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: const Text('Odaberi iz galerije'),
              onTap: () => Navigator.of(context).pop(ImageSource.gallery),
            ),
          ],
        ),
      ),
    );

    if (izvor == null || !mounted) {
      return;
    }

    final odabrana = await _birac.pickImage(
      source: izvor,
      maxWidth: 1600,
      imageQuality: 88,
    );

    if (odabrana == null || !mounted) {
      return;
    }

    setState(() => _uToku = strana);

    try {
      final sadrzaj = await odabrana.readAsBytes();

      final dozvola = await _servis.postaviFotografiju(
        strana: strana,
        imeFajla: odabrana.name,
        sadrzaj: sadrzaj,
      );

      if (!mounted) {
        return;
      }

      setState(() {
        _dozvola = dozvola;
        _uToku = null;
      });
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() => _uToku = null);

      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(greska.poruka)));
    }
  }

  Future<void> _prijavi() async {
    await Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => PrijavaDozvoleEkran(postojeca: _dozvola),
      ),
    );

    if (!mounted) {
      return;
    }

    await _ucitaj();
  }

  @override
  Widget build(BuildContext context) {
    final dozvola = _dozvola;

    return Scaffold(
      backgroundColor: Boje.platno,
      appBar: AppBar(title: const Text('Moja dozvola')),
      body: Sadrzaj(
        ucitavanje: _ucitavanje,
        greska: _greska,
        naPonovniPokusaj: _ucitaj,
        dijete: dozvola == null
            ? PrazanPopis(
                poruka:
                    'Dozvola još nije prijavljena. Bez verifikovane dozvole '
                    'rezervacija nije moguća.',
                ikona: Icons.badge_outlined,
                akcija: FilledButton(
                  onPressed: _prijavi,
                  child: const Text('Prijavi dozvolu'),
                ),
              )
            : ListView(
                padding: const EdgeInsets.all(Razmaci.l),
                children: [
                  _Status(dozvola: dozvola),
                  const SizedBox(height: Razmaci.l),
                  _Okvir(
                    naslov: 'Podaci sa dozvole',
                    dijete: Column(
                      children: [
                        _Red(oznaka: 'Broj', vrijednost: dozvola.brojDozvole),
                        _Red(
                          oznaka: 'Izdata',
                          vrijednost: Formati.datum(dozvola.datumIzdavanja),
                        ),
                        _Red(
                          oznaka: 'Važi do',
                          vrijednost: Formati.datum(dozvola.datumIsteka),
                        ),
                        _Red(
                          oznaka: 'Kategorije',
                          vrijednost: dozvola.kategorije.isEmpty
                              ? '-'
                              : dozvola.kategorije.join(', '),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: Razmaci.l),
                  _Okvir(
                    naslov: 'Fotografije',
                    dijete: Column(
                      children: [
                        _StranaRed(
                          naslov: 'Prednja strana',
                          opis: 'Podaci o vlasniku i rok važenja',
                          prilozena: dozvola.imaPrednjuStranu,
                          uToku: _uToku == StranaDozvole.prednja,
                          naDodir: () =>
                              _posaljiFotografiju(StranaDozvole.prednja),
                        ),
                        const Divider(height: Razmaci.l),
                        _StranaRed(
                          naslov: 'Zadnja strana',
                          opis: 'Kategorije i datumi po kategorijama',
                          prilozena: dozvola.imaZadnjuStranu,
                          uToku: _uToku == StranaDozvole.zadnja,
                          naDodir: () =>
                              _posaljiFotografiju(StranaDozvole.zadnja),
                        ),
                        if (!dozvola.imaObjeStrane) ...[
                          const SizedBox(height: Razmaci.m),
                          Obavjestenje.info(
                            'Potrebne su obje strane. Uposlenik ne može odobriti '
                            'dozvolu dok ne vidi i kategorije sa zadnje strane.',
                          ),
                        ],
                      ],
                    ),
                  ),
                  const SizedBox(height: Razmaci.l),
                  OutlinedButton.icon(
                    onPressed: _prijavi,
                    icon: const Icon(Icons.edit_outlined, size: 18),
                    label: const Text('Izmijeni podatke dozvole'),
                  ),
                  const SizedBox(height: Razmaci.s),
                  const Text(
                    'Svaka izmjena podataka vraća dozvolu na provjeru.',
                    textAlign: TextAlign.center,
                    style: TextStyle(fontSize: 11, color: Boje.tekstPrigusen),
                  ),
                ],
              ),
      ),
    );
  }
}

class _Status extends StatelessWidget {
  const _Status({required this.dozvola});

  final VozackaDozvola dozvola;

  @override
  Widget build(BuildContext context) {
    if (dozvola.status == StatusDozvole.odbijena) {
      return Obavjestenje.greska(
        dozvola.razlogOdbijanja ??
            'Dozvola je odbijena. Provjerite podatke i pošaljite je ponovo.',
      );
    }

    if (dozvola.istekla) {
      return Obavjestenje.greska(
        'Dozvola je istekla ${Formati.datum(dozvola.datumIsteka)}.',
      );
    }

    if (dozvola.status == StatusDozvole.naCekanju) {
      return Obavjestenje.info(
        dozvola.imaObjeStrane
            ? 'Dozvola je na provjeri. Javit ćemo vam kad je uposlenik pregleda.'
            : 'Dozvola čeka fotografije obje strane.',
        naslov: 'Na provjeri',
      );
    }

    return Obavjestenje.uspjeh(
      'Dozvola je verifikovana '
              '${dozvola.datumVerifikacije == null ? '' : Formati.datum(dozvola.datumVerifikacije!)}'
          .trim(),
    );
  }
}

class _StranaRed extends StatelessWidget {
  const _StranaRed({
    required this.naslov,
    required this.opis,
    required this.prilozena,
    required this.uToku,
    required this.naDodir,
  });

  final String naslov;
  final String opis;
  final bool prilozena;
  final bool uToku;
  final VoidCallback naDodir;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Container(
          width: 40,
          height: 40,
          decoration: BoxDecoration(
            color: prilozena ? Boje.uspjehPozadina : Boje.platno,
            borderRadius: BorderRadius.circular(Zaobljenja.dugme),
          ),
          child: Icon(
            prilozena ? Icons.check : Icons.add_a_photo_outlined,
            size: 18,
            color: prilozena ? Boje.uspjehTekst : Boje.tekstPrigusen,
          ),
        ),
        const SizedBox(width: Razmaci.m),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                naslov,
                style: const TextStyle(
                  fontSize: 13.5,
                  fontWeight: FontWeight.w600,
                ),
              ),
              Text(
                prilozena ? 'Priložena' : opis,
                style: const TextStyle(
                  fontSize: 11.5,
                  color: Boje.tekstPrigusen,
                ),
              ),
            ],
          ),
        ),
        uToku
            ? const SizedBox(
                width: 18,
                height: 18,
                child: CircularProgressIndicator(strokeWidth: 2),
              )
            : TextButton(
                onPressed: naDodir,
                child: Text(prilozena ? 'Zamijeni' : 'Dodaj'),
              ),
      ],
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

class _Red extends StatelessWidget {
  const _Red({required this.oznaka, required this.vrijednost});

  final String oznaka;
  final String vrijednost;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Text(
              oznaka,
              style: const TextStyle(fontSize: 12.5, color: Boje.tekstBlazi),
            ),
          ),
          Expanded(
            child: Text(
              vrijednost,
              textAlign: TextAlign.end,
              style: const TextStyle(
                fontSize: 12.5,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
