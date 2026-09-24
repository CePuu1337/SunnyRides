import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../servisi/korisnik_servis.dart';
import '../../stanje/sesija.dart';
import '../../widgeti/kartica.dart';
import '../../widgeti/paginator.dart';
import '../../widgeti/polja.dart';
import '../../widgeti/sadrzaj.dart';
import '../../widgeti/slicica.dart';
import 'korisnik_forma.dart';

class KorisniciEkran extends StatefulWidget {
  const KorisniciEkran({super.key});

  @override
  State<KorisniciEkran> createState() => _KorisniciEkranStanje();
}

class _KorisniciEkranStanje extends State<KorisniciEkran> {
  late final KorisnikServis _servis;

  Strana<Korisnik> _strana = Strana.prazna();
  List<Uloga> _uloge = const [];

  String _tekst = '';
  String? _uloga;
  bool? _aktivan;
  int _stranica = 0;

  static const _velicinaStranice = 15;

  bool _ucitavanje = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = KorisnikServis(context.read<ApiKlijent>());

    _ucitajUloge();
    _ucitaj();
  }

  Future<void> _ucitajUloge() async {
    try {
      final uloge = await _servis.uloge();

      if (mounted) {
        setState(() => _uloge = uloge);
      }
    } on ApiGreska {
      // Filter po ulozi ostaje prazan; lista i bez njega radi.
    }
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      final strana = await _servis.lista(
        tekst: _tekst.isEmpty ? null : _tekst,
        uloga: _uloga,
        aktivan: _aktivan,
        stranica: _stranica,
        velicinaStranice: _velicinaStranice,
      );

      if (!mounted) {
        return;
      }

      setState(() {
        _strana = strana;
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

  Future<void> _otvoriFormu({Korisnik? korisnik}) async {
    final sacuvano = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (context) => KorisnikForma(korisnik: korisnik, uloge: _uloge),
    );

    if (sacuvano == true) {
      _ucitaj();

      // Ako je administrator mijenjao vlastiti nalog, zaglavlje mora pokazati novo
      // ime umjesto onog od prijave.
      if (mounted && korisnik?.id == context.read<Sesija>().korisnik?.id) {
        _osvjeziSesiju(korisnik!.id);
      }
    }
  }

  Future<void> _osvjeziSesiju(int korisnikId) async {
    try {
      final strana = await _servis.lista(velicinaStranice: 100);
      final azuriran = strana.stavke
          .where((x) => x.id == korisnikId)
          .firstOrNull;

      if (azuriran != null && mounted) {
        context.read<Sesija>().osvjeziKorisnika(azuriran);
      }
    } on ApiGreska {
      // Zaglavlje ostaje sa starim imenom do sljedece prijave. Nije razlog za gresku.
    }
  }

  Future<void> _resetLozinke(Korisnik korisnik) async {
    final postavljena = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (context) => ResetLozinkeDijalog(korisnik: korisnik),
    );

    if (postavljena == true && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Lozinka je postavljena. Javite je korisniku — sistem je ne šalje.',
          ),
        ),
      );
    }
  }

  Future<void> _promijeniBlokadu(Korisnik korisnik) async {
    try {
      if (korisnik.blokiran) {
        await _servis.odblokiraj(korisnik.id);
      } else {
        await _servis.blokiraj(korisnik.id);
      }

      _ucitaj();
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(greska.poruka)));
    }
  }

  Future<void> _deaktiviraj(Korisnik korisnik) async {
    final potvrda = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Deaktivacija naloga'),
        content: Text(
          '${korisnik.punoIme} se više neće moći prijaviti. Rezervacije, plaćanja i '
          'recenzije ostaju — bez njih bi izvještaji govorili neistinu.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Odustani'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Deaktiviraj'),
          ),
        ],
      ),
    );

    if (potvrda != true) {
      return;
    }

    try {
      await _servis.deaktiviraj(korisnik.id);
      _ucitaj();
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(greska.poruka)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final ja = context.read<Sesija>().korisnik;

    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(Razmaci.ekranMargina),
        child: Kartica(
          naslov: 'Korisnici',
          podnaslov: 'Nalozi klijenata i osoblja',
          bezUnutrasnjegRazmaka: true,
          rastegni: true,
          akcija: ElevatedButton.icon(
            onPressed: _uloge.isEmpty ? null : () => _otvoriFormu(),
            icon: const Icon(Icons.person_add_alt, size: 18),
            label: const Text('Novi nalog'),
          ),
          dijete: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Padding(
                padding: const EdgeInsets.all(Razmaci.karticaUnutra),
                child: Wrap(
                  spacing: Razmaci.m,
                  runSpacing: Razmaci.m,
                  children: [
                    PoljePretrage(
                      natpis: 'Ime, korisničko ime ili email',
                      sirina: 290,
                      naPromjenu: (tekst) {
                        _tekst = tekst;
                        _stranica = 0;
                        _ucitaj();
                      },
                    ),
                    SizedBox(
                      width: 190,
                      child: DropdownButtonFormField<String?>(
                        key: ValueKey(_uloga),
                        initialValue: _uloga,
                        isExpanded: true,
                        decoration: const InputDecoration(labelText: 'Uloga'),
                        items: [
                          const DropdownMenuItem<String?>(
                            value: null,
                            child: Text('Sve uloge'),
                          ),
                          for (final uloga in _uloge)
                            DropdownMenuItem<String?>(
                              value: uloga.naziv,
                              child: Text(uloga.naziv),
                            ),
                        ],
                        onChanged: (vrijednost) {
                          setState(() {
                            _uloga = vrijednost;
                            _stranica = 0;
                          });
                          _ucitaj();
                        },
                      ),
                    ),
                    SizedBox(
                      width: 190,
                      child: DropdownButtonFormField<bool?>(
                        key: ValueKey(_aktivan),
                        initialValue: _aktivan,
                        isExpanded: true,
                        decoration: const InputDecoration(labelText: 'Stanje'),
                        items: const [
                          DropdownMenuItem<bool?>(
                            value: null,
                            child: Text('Svi'),
                          ),
                          DropdownMenuItem<bool?>(
                            value: true,
                            child: Text('Aktivni'),
                          ),
                          DropdownMenuItem<bool?>(
                            value: false,
                            child: Text('Deaktivirani'),
                          ),
                        ],
                        onChanged: (vrijednost) {
                          setState(() {
                            _aktivan = vrijednost;
                            _stranica = 0;
                          });
                          _ucitaj();
                        },
                      ),
                    ),
                  ],
                ),
              ),
              const Divider(height: 1),
              Expanded(
                child: Sadrzaj(
                  ucitavanje: _ucitavanje,
                  greska: _greska,
                  naPonovniPokusaj: _ucitaj,
                  dijete: _strana.jePrazna
                      ? const PrazanPopis(
                          poruka: 'Nema naloga po ovim filterima.',
                          ikona: Icons.people_outline,
                        )
                      : ListView.separated(
                          itemCount: _strana.stavke.length,
                          separatorBuilder: (context, indeks) =>
                              const Divider(height: 1),
                          itemBuilder: (context, indeks) {
                            final korisnik = _strana.stavke[indeks];

                            return _Red(
                              korisnik: korisnik,
                              jeJa: korisnik.id == ja?.id,
                              naIzmjenu: () => _otvoriFormu(korisnik: korisnik),
                              naResetLozinke: () => _resetLozinke(korisnik),
                              naBlokadu: () => _promijeniBlokadu(korisnik),
                              naDeaktivaciju: () => _deaktiviraj(korisnik),
                            );
                          },
                        ),
                ),
              ),
              Paginator(
                stranica: _stranica,
                velicinaStranice: _velicinaStranice,
                prikazano: _strana.stavke.length,
                ukupno: _strana.ukupno,
                naStranicu: (stranica) {
                  setState(() => _stranica = stranica);
                  _ucitaj();
                },
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _Red extends StatelessWidget {
  const _Red({
    required this.korisnik,
    required this.jeJa,
    required this.naIzmjenu,
    required this.naResetLozinke,
    required this.naBlokadu,
    required this.naDeaktivaciju,
  });

  final Korisnik korisnik;

  /// Prijavljeni administrator gleda vlastiti nalog. Blokada i deaktivacija sebe se
  /// ne nude - server ih ionako odbija, a ovako se i ne moze doci do te greske.
  final bool jeJa;

  final VoidCallback naIzmjenu;
  final VoidCallback naResetLozinke;
  final VoidCallback naBlokadu;
  final VoidCallback naDeaktivaciju;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(
        horizontal: Razmaci.karticaUnutra,
        vertical: Razmaci.m,
      ),
      child: Row(
        children: [
          Slicica(
            putanja: korisnik.thumbnailUrl,
            sirina: 40,
            visina: 40,
            zamjenskaIkona: Icons.person_outline,
          ),
          const SizedBox(width: Razmaci.m),
          Expanded(
            flex: 3,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Flexible(
                      child: Text(
                        korisnik.punoIme,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          fontSize: 13.5,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                    if (jeJa) ...[
                      const SizedBox(width: Razmaci.s),
                      const Text(
                        '(vi)',
                        style: TextStyle(
                          color: Boje.tekstPrigusen,
                          fontSize: 11.5,
                        ),
                      ),
                    ],
                  ],
                ),
                const SizedBox(height: 2),
                Text(
                  '${korisnik.korisnickoIme} · ${korisnik.email}',
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: Boje.tekstPrigusen,
                    fontSize: 11.5,
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            flex: 2,
            child: Wrap(
              spacing: Razmaci.xs,
              runSpacing: Razmaci.xs,
              children: [
                for (final uloga in korisnik.uloge)
                  StatusnaPilula(
                    tekst: uloga,
                    pozadina: Boje.neutralnoPozadina,
                    bojaTeksta: Boje.neutralnoTekst,
                  ),
              ],
            ),
          ),
          SizedBox(width: 120, child: _Stanje(korisnik: korisnik)),
          IconButton(
            tooltip: 'Izmijeni',
            onPressed: naIzmjenu,
            icon: const Icon(Icons.edit_outlined, size: 18),
          ),
          PopupMenuButton<String>(
            tooltip: 'Više',
            icon: const Icon(Icons.more_vert, size: 18),
            onSelected: (izbor) {
              switch (izbor) {
                case 'lozinka':
                  naResetLozinke();
                case 'blokada':
                  naBlokadu();
                case 'deaktivacija':
                  naDeaktivaciju();
              }
            },
            itemBuilder: (context) => [
              const PopupMenuItem(
                value: 'lozinka',
                child: Row(
                  children: [
                    Icon(Icons.key_outlined, size: 18),
                    SizedBox(width: Razmaci.m),
                    Text('Nova lozinka'),
                  ],
                ),
              ),
              if (!jeJa)
                PopupMenuItem(
                  value: 'blokada',
                  child: Row(
                    children: [
                      Icon(
                        korisnik.blokiran
                            ? Icons.lock_open_outlined
                            : Icons.block_outlined,
                        size: 18,
                      ),
                      const SizedBox(width: Razmaci.m),
                      Text(korisnik.blokiran ? 'Odblokiraj' : 'Blokiraj'),
                    ],
                  ),
                ),
              if (!jeJa && korisnik.aktivan)
                const PopupMenuItem(
                  value: 'deaktivacija',
                  child: Row(
                    children: [
                      Icon(
                        Icons.person_off_outlined,
                        size: 18,
                        color: Boje.greskaTekst,
                      ),
                      SizedBox(width: Razmaci.m),
                      Text(
                        'Deaktiviraj nalog',
                        style: TextStyle(color: Boje.greskaTekst),
                      ),
                    ],
                  ),
                ),
            ],
          ),
        ],
      ),
    );
  }
}

class _Stanje extends StatelessWidget {
  const _Stanje({required this.korisnik});

  final Korisnik korisnik;

  @override
  Widget build(BuildContext context) {
    if (!korisnik.aktivan) {
      return const StatusnaPilula(
        tekst: 'Deaktiviran',
        pozadina: Boje.neutralnoPozadina,
        bojaTeksta: Boje.neutralnoTekst,
      );
    }

    if (korisnik.blokiran) {
      return const StatusnaPilula(
        tekst: 'Blokiran',
        pozadina: Boje.greskaPozadina,
        bojaTeksta: Boje.greskaTekst,
        ikona: Icons.block,
      );
    }

    return const StatusnaPilula(
      tekst: 'Aktivan',
      pozadina: Boje.uspjehPozadina,
      bojaTeksta: Boje.uspjehTekst,
    );
  }
}
