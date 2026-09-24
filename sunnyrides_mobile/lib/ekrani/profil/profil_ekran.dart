import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../servisi/profil_servis.dart';
import '../../stanje/sesija.dart';
import '../../widgeti/obavjestenje.dart';
import '../dozvola/moja_dozvola_ekran.dart';
import '../recenzije/moje_recenzije_ekran.dart';
import 'izmjena_profila_ekran.dart';
import 'promjena_lozinke_ekran.dart';

/// Vlastiti nalog: podaci, dozvola, recenzije, lozinka i odjava.
class ProfilEkran extends StatefulWidget {
  const ProfilEkran({super.key});

  @override
  State<ProfilEkran> createState() => _ProfilEkranStanje();
}

class _ProfilEkranStanje extends State<ProfilEkran> {
  late final ProfilServis _servis;
  final _birac = ImagePicker();

  Korisnik? _korisnik;
  VozackaDozvola? _dozvola;

  bool _ucitavanje = true;
  bool _slanjeSlike = false;
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
      final korisnik = await _servis.moj();
      final dozvola = await _servis.mojaDozvola();

      if (!mounted) {
        return;
      }

      context.read<Sesija>().osvjeziKorisnika(korisnik);

      setState(() {
        _korisnik = korisnik;
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

  Future<void> _promijeniSliku() async {
    final odabrana = await _birac.pickImage(
      source: ImageSource.gallery,
      maxWidth: 1200,
      imageQuality: 85,
    );

    if (odabrana == null || !mounted) {
      return;
    }

    setState(() => _slanjeSlike = true);

    try {
      final sadrzaj = await odabrana.readAsBytes();

      final korisnik = await _servis.postaviSliku(
        imeFajla: odabrana.name,
        sadrzaj: sadrzaj,
      );

      if (!mounted) {
        return;
      }

      context.read<Sesija>().osvjeziKorisnika(korisnik);

      setState(() {
        _korisnik = korisnik;
        _slanjeSlike = false;
      });
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() => _slanjeSlike = false);

      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(greska.poruka)));
    }
  }

  Future<void> _odjava() async {
    final potvrda = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Odjava'),
        content: const Text('Želite li se odjaviti sa ovog uređaja?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Odustani'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Odjavi me'),
          ),
        ],
      ),
    );

    if (potvrda != true || !mounted) {
      return;
    }

    await context.read<Sesija>().odjava();
  }

  @override
  Widget build(BuildContext context) {
    final korisnik = _korisnik;
    final dozvola = _dozvola;

    return Scaffold(
      backgroundColor: Boje.platno,
      appBar: AppBar(title: const Text('Profil')),
      body: Sadrzaj(
        ucitavanje: _ucitavanje,
        greska: _greska,
        naPonovniPokusaj: _ucitaj,
        dijete: korisnik == null
            ? const SizedBox.shrink()
            : RefreshIndicator(
                onRefresh: _ucitaj,
                child: ListView(
                  padding: const EdgeInsets.all(Razmaci.l),
                  children: [
                    _Zaglavlje(
                      korisnik: korisnik,
                      slanje: _slanjeSlike,
                      naSliku: _promijeniSliku,
                    ),
                    const SizedBox(height: Razmaci.l),
                    if (dozvola == null)
                      Obavjestenje.upozorenje(
                        'Bez verifikovane dozvole ne možete rezervisati vozilo. '
                        'Prijavite dozvolu i priložite fotografije obje strane.',
                        naslov: 'Dozvola nije prijavljena',
                      )
                    else if (dozvola.status == StatusDozvole.odbijena)
                      Obavjestenje.greska(
                        dozvola.razlogOdbijanja ??
                            'Dozvola je odbijena. Provjerite podatke i pošaljite '
                                'je ponovo.',
                      )
                    else if (dozvola.status == StatusDozvole.naCekanju)
                      Obavjestenje.info(
                        dozvola.imaObjeStrane
                            ? 'Dozvola je poslana na provjeru. Javit ćemo vam kad '
                                  'je uposlenik pregleda.'
                            : 'Dozvola čeka fotografije. Potrebne su prednja i '
                                  'zadnja strana.',
                      )
                    else if (dozvola.istekla)
                      Obavjestenje.greska(
                        'Dozvola je istekla ${Formati.datum(dozvola.datumIsteka)}. '
                        'Prijavite novu da biste mogli rezervisati.',
                      )
                    else
                      Obavjestenje.uspjeh(
                        'Dozvola je verifikovana. Kategorije: '
                        '${dozvola.kategorije.join(', ')}.',
                      ),
                    const SizedBox(height: Razmaci.l),
                    _Grupa(
                      stavke: [
                        _Stavka(
                          ikona: Icons.person_outline,
                          naslov: 'Moji podaci',
                          opis: 'Ime, email, telefon i datum rođenja',
                          naDodir: () =>
                              _otvori(IzmjenaProfilaEkran(korisnik: korisnik)),
                        ),
                        _Stavka(
                          ikona: Icons.badge_outlined,
                          naslov: 'Moja dozvola',
                          opis: dozvola == null
                              ? 'Nije prijavljena'
                              : 'Broj ${dozvola.brojDozvole}',
                          naDodir: () => _otvori(const MojaDozvolaEkran()),
                        ),
                        _Stavka(
                          ikona: Icons.star_outline,
                          naslov: 'Moje recenzije',
                          opis:
                              'Ocjene koje ste dali i najmovi za ocjenjivanje',
                          naDodir: () => _otvori(const MojeRecenzijeEkran()),
                        ),
                        _Stavka(
                          ikona: Icons.lock_outline,
                          naslov: 'Promjena lozinke',
                          opis: 'Traži staru lozinku',
                          naDodir: () => _otvori(const PromjenaLozinkeEkran()),
                        ),
                      ],
                    ),
                    const SizedBox(height: Razmaci.l),
                    OutlinedButton.icon(
                      onPressed: _odjava,
                      style: OutlinedButton.styleFrom(
                        foregroundColor: Boje.greska,
                      ),
                      icon: const Icon(Icons.logout, size: 18),
                      label: const Text('Odjavi se'),
                    ),
                    const SizedBox(height: Razmaci.l),
                    Center(
                      child: Text(
                        'SunnyRides · nalog od '
                        '${Formati.datum(korisnik.datumRegistracije)}',
                        style: const TextStyle(
                          fontSize: 11,
                          color: Boje.tekstPrigusen,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
      ),
    );
  }

  Future<void> _otvori(Widget ekran) async {
    await Navigator.of(context)
        .push(MaterialPageRoute<void>(builder: (_) => ekran));

    if (!mounted) {
      return;
    }

    await _ucitaj();
  }
}

class _Zaglavlje extends StatelessWidget {
  const _Zaglavlje({
    required this.korisnik,
    required this.slanje,
    required this.naSliku,
  });

  final Korisnik korisnik;
  final bool slanje;
  final VoidCallback naSliku;

  @override
  Widget build(BuildContext context) {
    final slika = context.read<Okruzenje>().apsolutnaSlika(
      korisnik.thumbnailUrl,
    );

    return Container(
      padding: const EdgeInsets.all(Razmaci.l),
      decoration: BoxDecoration(
        color: Boje.povrsina,
        borderRadius: BorderRadius.circular(Zaobljenja.kartica),
        border: Border.all(color: Boje.ivica),
      ),
      child: Row(
        children: [
          Stack(
            children: [
              CircleAvatar(
                radius: 30,
                backgroundColor: Boje.primarnaSvijetla,
                foregroundImage: slika == null ? null : NetworkImage(slika),
                child: Text(
                  korisnik.inicijali,
                  style: const TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                    color: Boje.naPrimarnoj,
                  ),
                ),
              ),
              Positioned(
                right: -2,
                bottom: -2,
                child: Material(
                  color: Boje.primarna,
                  shape: const CircleBorder(),
                  child: InkWell(
                    onTap: slanje ? null : naSliku,
                    customBorder: const CircleBorder(),
                    child: Padding(
                      padding: const EdgeInsets.all(5),
                      child: slanje
                          ? const SizedBox(
                              width: 13,
                              height: 13,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : const Icon(
                              Icons.photo_camera_outlined,
                              size: 13,
                              color: Boje.naPrimarnoj,
                            ),
                    ),
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(width: Razmaci.l),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  korisnik.punoIme,
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  korisnik.email,
                  style: const TextStyle(
                    fontSize: 12.5,
                    color: Boje.tekstPrigusen,
                  ),
                ),
                if (korisnik.telefon != null && korisnik.telefon!.isNotEmpty)
                  Text(
                    korisnik.telefon!,
                    style: const TextStyle(
                      fontSize: 12.5,
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

class _Grupa extends StatelessWidget {
  const _Grupa({required this.stavke});

  final List<Widget> stavke;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Boje.povrsina,
        borderRadius: BorderRadius.circular(Zaobljenja.kartica),
        border: Border.all(color: Boje.ivica),
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(children: stavke),
    );
  }
}

class _Stavka extends StatelessWidget {
  const _Stavka({
    required this.ikona,
    required this.naslov,
    required this.opis,
    required this.naDodir,
  });

  final IconData ikona;
  final String naslov;
  final String opis;
  final VoidCallback naDodir;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      onTap: naDodir,
      leading: Icon(ikona, size: 21, color: Boje.tekstBlazi),
      title: Text(naslov, style: const TextStyle(fontSize: 14)),
      subtitle: Text(opis, style: const TextStyle(fontSize: 11.5)),
      trailing: const Icon(
        Icons.chevron_right,
        size: 20,
        color: Boje.tekstPrigusen,
      ),
    );
  }
}
