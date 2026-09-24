import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../servisi/rezervacija_servis.dart';
import '../../widgeti/obavjestenje.dart';
import '../../widgeti/slika.dart';
import 'otkazivanje_list.dart';
import 'placanje_ekran.dart';

/// Jedna rezervacija: vozilo, termin, iznosi, oprema i historija statusa.
class DetaljiRezervacijeEkran extends StatefulWidget {
  const DetaljiRezervacijeEkran({super.key, required this.rezervacijaId});

  final int rezervacijaId;

  @override
  State<DetaljiRezervacijeEkran> createState() =>
      _DetaljiRezervacijeEkranStanje();
}

class _DetaljiRezervacijeEkranStanje extends State<DetaljiRezervacijeEkran> {
  late final RezervacijaServis _servis;

  Rezervacija? _rezervacija;
  bool _ucitavanje = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = RezervacijaServis(context.read<ApiKlijent>());
    _ucitaj();
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      final rezervacija = await _servis.detalji(widget.rezervacijaId);

      if (!mounted) {
        return;
      }

      setState(() {
        _rezervacija = rezervacija;
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

  Future<void> _plati() async {
    final rezervacija = _rezervacija;

    if (rezervacija == null) {
      return;
    }

    await Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => PlacanjeEkran(rezervacija: rezervacija),
      ),
    );

    if (!mounted) {
      return;
    }

    await _ucitaj();
  }

  Future<void> _otkazi() async {
    final rezervacija = _rezervacija;

    if (rezervacija == null) {
      return;
    }

    final otkazano = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => OtkazivanjeList(rezervacija: rezervacija),
    );

    if (!mounted) {
      return;
    }

    if (otkazano == true) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Rezervacija je otkazana.')));
    }

    await _ucitaj();
  }

  /// Kad je, zasto i sa cijom napomenom je rezervacija otkazana - onoliko koliko
  /// server posalje. Sistemsko otkazivanje nema razlog iz sifarnika, pa tada stoji
  /// samo napomena.
  static String _opisOtkazivanja(Rezervacija rezervacija) {
    final redovi = <String>[];

    if (rezervacija.datumOtkazivanja != null) {
      redovi.add(
        'Otkazano ${Formati.datumIVrijeme(rezervacija.datumOtkazivanja!)}',
      );
    } else {
      redovi.add('Rezervacija je otkazana.');
    }

    if (rezervacija.razlogOtkazivanjaNaziv != null) {
      redovi.add('Razlog: ${rezervacija.razlogOtkazivanjaNaziv}');
    }

    if (rezervacija.napomenaOtkazivanja != null &&
        rezervacija.napomenaOtkazivanja!.isNotEmpty) {
      redovi.add(rezervacija.napomenaOtkazivanja!);
    }

    return redovi.join('\n');
  }

  @override
  Widget build(BuildContext context) {
    final rezervacija = _rezervacija;

    return Scaffold(
      backgroundColor: Boje.platno,
      appBar: AppBar(title: Text(rezervacija?.broj ?? 'Rezervacija')),
      body: Sadrzaj(
        ucitavanje: _ucitavanje,
        greska: _greska,
        naPonovniPokusaj: _ucitaj,
        dijete: rezervacija == null
            ? const SizedBox.shrink()
            : RefreshIndicator(
                onRefresh: _ucitaj,
                child: ListView(
                  padding: const EdgeInsets.all(Razmaci.l),
                  children: [
                    Row(
                      children: [
                        Slika(
                          putanja: rezervacija.thumbnailUrl,
                          sirina: 88,
                          visina: 66,
                        ),
                        const SizedBox(width: Razmaci.m),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                rezervacija.vozilo.isEmpty
                                    ? rezervacija.registarskaOznaka ?? '-'
                                    : rezervacija.vozilo,
                                style: const TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                              const SizedBox(height: 2),
                              Text(
                                rezervacija.registarskaOznaka ?? '',
                                style: const TextStyle(
                                  fontSize: 12,
                                  color: Boje.tekstPrigusen,
                                ),
                              ),
                              const SizedBox(height: Razmaci.s),
                              StatusnaPilula.rezervacija(rezervacija.status),
                            ],
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: Razmaci.l),
                    if (!rezervacija.isPaid &&
                        rezervacija.status == StatusRezervacije.naCekanju)
                      Padding(
                        padding: const EdgeInsets.only(bottom: Razmaci.l),
                        child: Obavjestenje.upozorenje(
                          'Rezervacija čeka plaćanje. Vozilo se drži samo do '
                          'isteka roka, nakon toga se termin pušta u ponudu.',
                          naslov: 'Plaćanje nije završeno',
                        ),
                      ),
                    if (rezervacija.status == StatusRezervacije.otkazana)
                      Padding(
                        padding: const EdgeInsets.only(bottom: Razmaci.l),
                        child: Obavjestenje.greska(
                          _opisOtkazivanja(rezervacija),
                        ),
                      ),
                    _Okvir(
                      naslov: 'Termin i preuzimanje',
                      dijete: Column(
                        children: [
                          _Red(
                            oznaka: 'Preuzimanje',
                            vrijednost: Formati.datumIVrijeme(
                              rezervacija.datumOd,
                            ),
                          ),
                          _Red(
                            oznaka: 'Povrat',
                            vrijednost: Formati.datumIVrijeme(
                              rezervacija.datumDo,
                            ),
                          ),
                          _Red(
                            oznaka: 'Poslovnica',
                            vrijednost: rezervacija.poslovnicaNaziv ?? '-',
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: Razmaci.l),
                    _Okvir(
                      naslov: 'Iznosi',
                      dijete: Column(
                        children: [
                          if (rezervacija.iznosPopusta > 0)
                            _Red(
                              oznaka: 'Popust',
                              vrijednost: Formati.novac(
                                rezervacija.iznosPopusta,
                              ),
                            ),
                          for (final stavka in rezervacija.stavkeOpreme)
                            _Red(
                              oznaka: '${stavka.naziv} × ${stavka.kolicina}',
                              vrijednost: Formati.novac(stavka.iznos),
                            ),
                          if (rezervacija.paketOsiguranjaNaziv != null)
                            _Red(
                              oznaka: 'Osiguranje',
                              vrijednost: rezervacija.paketOsiguranjaNaziv!,
                            ),
                          _Red(
                            oznaka: 'Depozit',
                            vrijednost: Formati.novac(
                              rezervacija.iznosDepozita,
                            ),
                          ),
                          const Divider(height: Razmaci.l),
                          _Red(
                            oznaka: 'Ukupno',
                            vrijednost: Formati.novac(rezervacija.ukupanIznos),
                            podebljano: true,
                          ),
                          const SizedBox(height: Razmaci.xs),
                          Align(
                            alignment: Alignment.centerRight,
                            child: StatusnaPilula.placeno(rezervacija.isPaid),
                          ),
                        ],
                      ),
                    ),
                    if (rezervacija.historijaStatusa.isNotEmpty) ...[
                      const SizedBox(height: Razmaci.l),
                      _Okvir(
                        naslov: 'Historija',
                        dijete: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            for (final korak in rezervacija.historijaStatusa)
                              Padding(
                                padding: const EdgeInsets.only(
                                  bottom: Razmaci.s,
                                ),
                                child: Row(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    const Padding(
                                      padding: EdgeInsets.only(top: 4),
                                      child: Icon(
                                        Icons.circle,
                                        size: 7,
                                        color: Boje.ivicaJaca,
                                      ),
                                    ),
                                    const SizedBox(width: Razmaci.s),
                                    Expanded(
                                      child: Column(
                                        crossAxisAlignment:
                                            CrossAxisAlignment.start,
                                        children: [
                                          Text(
                                            korak.opis,
                                            style: const TextStyle(
                                              fontSize: 12.5,
                                            ),
                                          ),
                                          Text(
                                            Formati.datumIVrijeme(
                                              korak.datumVrijeme,
                                            ),
                                            style: const TextStyle(
                                              fontSize: 11,
                                              color: Boje.tekstPrigusen,
                                            ),
                                          ),
                                        ],
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                          ],
                        ),
                      ),
                    ],
                    const SizedBox(height: Razmaci.xxl),
                  ],
                ),
              ),
      ),
      bottomNavigationBar: rezervacija == null
          ? null
          : SafeArea(
              child: Padding(
                padding: const EdgeInsets.all(Razmaci.l),
                child: Row(
                  children: [
                    if (rezervacija.seMozeOtkazati)
                      Expanded(
                        child: OutlinedButton(
                          onPressed: _otkazi,
                          style: OutlinedButton.styleFrom(
                            foregroundColor: Boje.greska,
                          ),
                          child: const Text('Otkaži'),
                        ),
                      ),
                    if (rezervacija.seMozeOtkazati && !rezervacija.isPaid)
                      const SizedBox(width: Razmaci.m),
                    if (!rezervacija.isPaid &&
                        rezervacija.status == StatusRezervacije.naCekanju)
                      Expanded(
                        child: FilledButton(
                          onPressed: _plati,
                          child: const Text('Plati'),
                        ),
                      ),
                  ],
                ),
              ),
            ),
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
  const _Red({
    required this.oznaka,
    required this.vrijednost,
    this.podebljano = false,
  });

  final String oznaka;
  final String vrijednost;
  final bool podebljano;

  @override
  Widget build(BuildContext context) {
    final stil = TextStyle(
      fontSize: podebljano ? 14 : 12.5,
      fontWeight: podebljano ? FontWeight.w700 : FontWeight.w400,
    );

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2.5),
      child: Row(
        children: [
          Expanded(
            child: Text(
              oznaka,
              style: stil.copyWith(
                color: podebljano ? Boje.tekst : Boje.tekstBlazi,
              ),
            ),
          ),
          Text(vrijednost, style: stil),
        ],
      ),
    );
  }
}
