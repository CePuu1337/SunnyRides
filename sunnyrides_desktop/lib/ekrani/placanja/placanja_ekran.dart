import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/placanje.dart';
import '../../modeli/stavka_sifrarnika.dart';
import '../../servisi/placanje_servis.dart';
import '../../servisi/sifrarnik_servis.dart';
import '../../stanje/opseg_poslovnice.dart';
import '../../stanje/sesija.dart';
import '../../widgeti/kartica.dart';
import '../../widgeti/paginator.dart';
import '../../widgeti/polja.dart';
import '../../widgeti/sadrzaj.dart';
import 'prikaz_placanja.dart';

/// Naplate i povrati novca.
///
/// Glavni razlog postojanja je povrat koji je Stripe odbio: worker sam ponavlja samo
/// povrate na koje Stripe nije odgovorio, a odbijeni ostaje odbijen dok ga neko ne
/// pošalje ponovo. Filter „Samo neuspjeli povrati" pokazuje upravo njih.
class PlacanjaEkran extends StatefulWidget {
  const PlacanjaEkran({super.key});

  @override
  State<PlacanjaEkran> createState() => _PlacanjaEkranStanje();
}

class _PlacanjaEkranStanje extends State<PlacanjaEkran> {
  late final PlacanjeServis _servis;
  late final SifrarnikServis _sifrarnici;

  Strana<Placanje> _strana = Strana.prazna();
  List<StavkaSifrarnika> _poslovnice = const [];

  String _broj = '';
  StatusPlacanja? _status;
  int? _poslovnicaId;
  bool _samoNeuspjeliPovrati = false;
  int _stranica = 0;

  static const _velicinaStranice = 10;

  bool _ucitavanje = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    final klijent = context.read<ApiKlijent>();
    _servis = PlacanjeServis(klijent);
    _sifrarnici = SifrarnikServis(klijent);

    final korisnik = context.read<Sesija>().korisnik;
    if (korisnik != null && korisnik.vezanZaPoslovnicu) {
      _poslovnicaId = korisnik.poslovnicaId;
    }

    _ucitajPoslovnice();
    _ucitaj();
  }

  Future<void> _ucitajPoslovnice() async {
    try {
      final poslovnice = await _sifrarnici.ucitaj(SifrarnikServis.poslovnice);

      if (mounted) {
        setState(() => _poslovnice = dozvoljenePoslovnice(context, poslovnice));
      }
    } on ApiGreska {
      // Filter po poslovnici ostaje prazan; lista radi i bez njega.
    }
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      final strana = await _servis.lista(
        rezervacijaBroj: _broj.isEmpty ? null : _broj,
        status: _status,
        poslovnicaId: _poslovnicaId,
        samoNeuspjeliPovrati: _samoNeuspjeliPovrati,
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

  Future<void> _ponovi(Placanje placanje, Povrat povrat) async {
    final poslano = await ponoviPovratUzPotvrdu(
      context,
      _servis,
      placanje,
      povrat,
    );

    if (poslano && mounted) {
      await _ucitaj();
    }
  }

  void _promijeni(VoidCallback izmjena) {
    setState(() {
      izmjena();
      _stranica = 0;
    });
    _ucitaj();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(Razmaci.ekranMargina),
        child: Kartica(
          naslov: 'Plaćanja i povrati',
          podnaslov: 'Naplate rezervacija i povrati novca klijentima',
          bezUnutrasnjegRazmaka: true,
          rastegni: true,
          dijete: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Padding(
                padding: const EdgeInsets.all(Razmaci.karticaUnutra),
                child: Wrap(
                  spacing: Razmaci.m,
                  runSpacing: Razmaci.m,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    PoljePretrage(
                      natpis: 'Broj rezervacije',
                      naPromjenu: (tekst) {
                        _broj = tekst;
                        _stranica = 0;
                        _ucitaj();
                      },
                    ),
                    SizedBox(
                      width: 190,
                      child: DropdownButtonFormField<StatusPlacanja?>(
                        key: ValueKey(_status),
                        initialValue: _status,
                        isExpanded: true,
                        decoration: const InputDecoration(
                          labelText: 'Status plaćanja',
                        ),
                        items: [
                          const DropdownMenuItem<StatusPlacanja?>(
                            value: null,
                            child: Text('Svi'),
                          ),
                          for (final status in StatusPlacanja.values)
                            DropdownMenuItem<StatusPlacanja?>(
                              value: status,
                              child: Text(status.naziv),
                            ),
                        ],
                        onChanged: (status) =>
                            _promijeni(() => _status = status),
                      ),
                    ),
                    PadajuciSifrarnik(
                      natpis: 'Poslovnica',
                      sirina: 220,
                      stavke: _poslovnice,
                      odabrano: _poslovnicaId,
                      svePoljeNatpis: 'Sve poslovnice',
                      naPromjenu: (id) => _promijeni(() => _poslovnicaId = id),
                    ),
                    FilterChip(
                      label: const Text('Samo neuspjeli povrati'),
                      avatar: const Icon(Icons.error_outline, size: 16),
                      selected: _samoNeuspjeliPovrati,
                      onSelected: (odabrano) =>
                          _promijeni(() => _samoNeuspjeliPovrati = odabrano),
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
                      ? PrazanPopis(
                          poruka: _samoNeuspjeliPovrati
                              ? 'Nema odbijenih povrata. Svi povrati su prošli.'
                              : 'Nema plaćanja po ovim filterima.',
                          ikona: Icons.payments_outlined,
                        )
                      : ListView.separated(
                          padding: const EdgeInsets.all(Razmaci.karticaUnutra),
                          itemCount: _strana.stavke.length,
                          separatorBuilder: (_, _) =>
                              const SizedBox(height: Razmaci.m),
                          itemBuilder: (context, indeks) {
                            final placanje = _strana.stavke[indeks];

                            return PrikazPlacanja(
                              placanje: placanje,
                              naPonavljanje: (povrat) =>
                                  _ponovi(placanje, povrat),
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
