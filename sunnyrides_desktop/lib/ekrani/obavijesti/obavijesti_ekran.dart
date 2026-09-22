import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/recenzija.dart';
import '../../servisi/moderacija_servis.dart';
import '../../widgeti/kartica.dart';
import '../../widgeti/paginator.dart';
import '../../widgeti/polja.dart';
import '../../widgeti/sadrzaj.dart';
import '../../widgeti/slicica.dart';
import 'obavijest_forma.dart';

class ObavijestiEkran extends StatefulWidget {
  const ObavijestiEkran({super.key});

  @override
  State<ObavijestiEkran> createState() => _ObavijestiEkranStanje();
}

class _ObavijestiEkranStanje extends State<ObavijestiEkran> {
  late final ObavijestServis _servis;

  Strana<Obavijest> _strana = Strana.prazna();
  String _tekst = '';
  bool? _aktivna;
  int _stranica = 0;

  static const _velicinaStranice = 15;

  bool _ucitavanje = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = ObavijestServis(context.read<ApiKlijent>());
    _ucitaj();
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      final strana = await _servis.lista(
        tekst: _tekst.isEmpty ? null : _tekst,
        aktivna: _aktivna,
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

  Future<void> _otvoriFormu({Obavijest? obavijest}) async {
    final sacuvano = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (context) => ObavijestForma(obavijest: obavijest),
    );

    if (sacuvano == true) {
      _ucitaj();
    }
  }

  Future<void> _obrisi(Obavijest obavijest) async {
    final potvrda = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Brisanje obavijesti'),
        content: Text(
          '„${obavijest.naslov}" se briše trajno i nestaje iz mobilne aplikacije.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Odustani'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Obriši'),
          ),
        ],
      ),
    );

    if (potvrda != true) {
      return;
    }

    try {
      await _servis.obrisi(obavijest.id);
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
    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(Razmaci.ekranMargina),
        child: Kartica(
          naslov: 'Obavijesti',
          podnaslov: 'Objave koje klijenti vide na početnom ekranu',
          bezUnutrasnjegRazmaka: true,
          akcija: ElevatedButton.icon(
            onPressed: () => _otvoriFormu(),
            icon: const Icon(Icons.add, size: 18),
            label: const Text('Nova obavijest'),
          ),
          dijete: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Padding(
                padding: const EdgeInsets.all(Razmaci.karticaUnutra),
                child: Wrap(
                  spacing: Razmaci.m,
                  runSpacing: Razmaci.m,
                  children: [
                    PoljePretrage(
                      natpis: 'Naslov ili tekst',
                      naPromjenu: (tekst) {
                        _tekst = tekst;
                        _stranica = 0;
                        _ucitaj();
                      },
                    ),
                    SizedBox(
                      width: 190,
                      child: DropdownButtonFormField<bool?>(
                        key: ValueKey(_aktivna),
                        initialValue: _aktivna,
                        isExpanded: true,
                        decoration: const InputDecoration(labelText: 'Stanje'),
                        items: const [
                          DropdownMenuItem<bool?>(
                            value: null,
                            child: Text('Sve'),
                          ),
                          DropdownMenuItem<bool?>(
                            value: true,
                            child: Text('Aktivne'),
                          ),
                          DropdownMenuItem<bool?>(
                            value: false,
                            child: Text('Neaktivne'),
                          ),
                        ],
                        onChanged: (vrijednost) {
                          setState(() {
                            _aktivna = vrijednost;
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
              SizedBox(
                height: 470,
                child: Sadrzaj(
                  ucitavanje: _ucitavanje,
                  greska: _greska,
                  naPonovniPokusaj: _ucitaj,
                  dijete: _strana.jePrazna
                      ? const PrazanPopis(
                          poruka: 'Nema obavijesti po ovim filterima.',
                          ikona: Icons.campaign_outlined,
                        )
                      : ListView.separated(
                          itemCount: _strana.stavke.length,
                          separatorBuilder: (context, indeks) =>
                              const Divider(height: 1),
                          itemBuilder: (context, indeks) {
                            final obavijest = _strana.stavke[indeks];

                            return _Red(
                              obavijest: obavijest,
                              naIzmjenu: () =>
                                  _otvoriFormu(obavijest: obavijest),
                              naBrisanje: () => _obrisi(obavijest),
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
    required this.obavijest,
    required this.naIzmjenu,
    required this.naBrisanje,
  });

  final Obavijest obavijest;
  final VoidCallback naIzmjenu;
  final VoidCallback naBrisanje;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(
        horizontal: Razmaci.karticaUnutra,
        vertical: Razmaci.m,
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Slicica(
            putanja: obavijest.thumbnailUrl,
            sirina: 64,
            visina: 48,
            zamjenskaIkona: Icons.campaign_outlined,
          ),
          const SizedBox(width: Razmaci.l),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Flexible(
                      child: Text(
                        obavijest.naslov,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          fontSize: 13.5,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                    const SizedBox(width: Razmaci.s),
                    _Stanje(obavijest: obavijest),
                  ],
                ),
                const SizedBox(height: 2),
                Text(
                  Formati.datumIVrijeme(obavijest.datumObjave),
                  style: const TextStyle(
                    color: Boje.tekstPrigusen,
                    fontSize: 11.5,
                  ),
                ),
                const SizedBox(height: Razmaci.s),
                Text(
                  obavijest.tekst,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontSize: 12.5, height: 1.4),
                ),
              ],
            ),
          ),
          const SizedBox(width: Razmaci.m),
          IconButton(
            tooltip: 'Izmijeni',
            onPressed: naIzmjenu,
            icon: const Icon(Icons.edit_outlined, size: 18),
          ),
          IconButton(
            tooltip: 'Obriši',
            onPressed: naBrisanje,
            icon: const Icon(
              Icons.delete_outline,
              size: 18,
              color: Boje.greskaTekst,
            ),
          ),
        ],
      ),
    );
  }
}

class _Stanje extends StatelessWidget {
  const _Stanje({required this.obavijest});

  final Obavijest obavijest;

  @override
  Widget build(BuildContext context) {
    if (!obavijest.aktivna) {
      return const StatusnaPilula(
        tekst: 'Neaktivna',
        pozadina: Boje.neutralnoPozadina,
        bojaTeksta: Boje.neutralnoTekst,
      );
    }

    if (obavijest.zakazana) {
      return const StatusnaPilula(
        tekst: 'Zakazana',
        pozadina: Boje.upozorenjePozadina,
        bojaTeksta: Boje.upozorenjeTekst,
        ikona: Icons.schedule,
      );
    }

    return const StatusnaPilula(
      tekst: 'Objavljena',
      pozadina: Boje.uspjehPozadina,
      bojaTeksta: Boje.uspjehTekst,
    );
  }
}
