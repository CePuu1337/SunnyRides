import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/dozvola.dart';
import '../../servisi/dozvola_servis.dart';
import '../../widgeti/kartica.dart';
import '../../widgeti/obavjestenje.dart';
import '../../widgeti/sadrzaj.dart';
import 'odbijanje_dijalog.dart';

/// Jedan zahtjev za verifikaciju: fotografija, podaci, kategorije i odluka.
class DozvolaDetalji extends StatefulWidget {
  const DozvolaDetalji({
    super.key,
    required this.dozvolaId,
    required this.servis,
    required this.naObradu,
  });

  final int dozvolaId;
  final DozvolaServis servis;

  /// Zove se kad je dozvola odobrena ili odbijena, da se lista osvjezi.
  final VoidCallback naObradu;

  @override
  State<DozvolaDetalji> createState() => _DozvolaDetaljiStanje();
}

class _DozvolaDetaljiStanje extends State<DozvolaDetalji> {
  VozackaDozvola? _dozvola;
  DozvoljeneKategorije? _kategorije;
  final _fotografije = <StranaDozvole, Uint8List>{};

  bool _ucitavanje = true;
  bool _obrada = false;
  String? _greska;
  String? _greskaObrade;

  @override
  void initState() {
    super.initState();
    _ucitaj();
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      final dozvola = await widget.servis.detalji(widget.dozvolaId);
      final kategorije = await widget.servis.dozvoljeneKategorije(
        widget.dozvolaId,
      );

      if (!mounted) {
        return;
      }

      setState(() {
        _dozvola = dozvola;
        _kategorije = kategorije;
        _ucitavanje = false;
      });

      for (final strana in StranaDozvole.values) {
        if (dozvola.imaStranu(strana)) {
          await _ucitajFotografiju(strana);
        }
      }
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

  Future<void> _ucitajFotografiju(StranaDozvole strana) async {
    try {
      final bajtovi = await widget.servis.fotografija(widget.dozvolaId, strana);

      if (mounted) {
        setState(() => _fotografije[strana] = bajtovi);
      }
    } on ApiGreska {
      // Fotografija nije stigla, ali podaci jesu. Umjesto greske preko cijelog
      // ekrana, ostaje prazan okvir sa porukom u njemu.
    }
  }

  Future<void> _odobri() async {
    await _obradi(
      () => widget.servis.odobri(widget.dozvolaId),
      'Dozvola je odobrena. Klijent može rezervisati vozila svoje kategorije.',
    );
  }

  Future<void> _odbij() async {
    final razlog = await showDialog<String>(
      context: context,
      barrierDismissible: false,
      builder: (context) =>
          OdbijanjeDijalog(klijent: _dozvola?.klijentImePrezime ?? ''),
    );

    if (razlog == null) {
      return;
    }

    await _obradi(
      () => widget.servis.odbij(widget.dozvolaId, razlog),
      'Dozvola je odbijena, a razlog poslan klijentu.',
    );
  }

  Future<void> _obradi(
    Future<VozackaDozvola> Function() radnja,
    String poruka,
  ) async {
    setState(() {
      _obrada = true;
      _greskaObrade = null;
    });

    try {
      final dozvola = await radnja();

      if (!mounted) {
        return;
      }

      setState(() {
        _dozvola = dozvola;
        _obrada = false;
      });

      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(poruka)));
      widget.naObradu();
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() {
        _obrada = false;
        _greskaObrade = greska.poruka;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final dozvola = _dozvola;

    return Kartica(
      naslov: dozvola?.klijentImePrezime ?? 'Detalji zahtjeva',
      podnaslov: dozvola == null
          ? null
          : '${dozvola.klijentEmail ?? ''} · ${dozvola.godine} godina',
      akcija: dozvola == null || !dozvola.cekaVerifikaciju
          ? null
          : _Odluka(
              uToku: _obrada,
              // Server odobrenje bez obje strane odbija, pa dugme ni ne treba biti
              // aktivno - uposlenik odmah vidi da mu nedostaje podatak, a ne tek
              // nakon klika.
              mozeOdobriti: dozvola.imaObjeStrane,
              naOdobrenje: _odobri,
              naOdbijanje: _odbij,
            ),
      dijete: Sadrzaj(
        ucitavanje: _ucitavanje,
        greska: _greska,
        naPonovniPokusaj: _ucitaj,
        dijete: dozvola == null
            ? const SizedBox.shrink()
            : _Sadrzaj(
                dozvola: dozvola,
                kategorije: _kategorije,
                fotografije: _fotografije,
                greskaObrade: _greskaObrade,
              ),
      ),
    );
  }
}

class _Odluka extends StatelessWidget {
  const _Odluka({
    required this.uToku,
    required this.mozeOdobriti,
    required this.naOdobrenje,
    required this.naOdbijanje,
  });

  final bool uToku;
  final bool mozeOdobriti;
  final VoidCallback naOdobrenje;
  final VoidCallback naOdbijanje;

  @override
  Widget build(BuildContext context) {
    if (uToku) {
      return const SizedBox(
        width: 18,
        height: 18,
        child: CircularProgressIndicator(strokeWidth: 2),
      );
    }

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        OutlinedButton.icon(
          onPressed: naOdbijanje,
          style: OutlinedButton.styleFrom(
            foregroundColor: Boje.greskaTekst,
            side: const BorderSide(color: Boje.greskaPozadina),
          ),
          icon: const Icon(Icons.close, size: 17),
          label: const Text('Odbij'),
        ),
        const SizedBox(width: Razmaci.m),
        Tooltip(
          message: mozeOdobriti
              ? ''
              : 'Nedostaje fotografija jedne strane dozvole.',
          child: ElevatedButton.icon(
            onPressed: mozeOdobriti ? naOdobrenje : null,
            icon: const Icon(Icons.check, size: 17),
            label: const Text('Odobri'),
          ),
        ),
      ],
    );
  }
}

class _Sadrzaj extends StatelessWidget {
  const _Sadrzaj({
    required this.dozvola,
    required this.kategorije,
    required this.fotografije,
    required this.greskaObrade,
  });

  final VozackaDozvola dozvola;
  final DozvoljeneKategorije? kategorije;
  final Map<StranaDozvole, Uint8List> fotografije;
  final String? greskaObrade;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (greskaObrade != null) ...[
            Obavjestenje.greska(greskaObrade!),
            const SizedBox(height: Razmaci.l),
          ],
          if (dozvola.istekla) ...[
            Obavjestenje.upozorenje(
              'Rok važenja dozvole je istekao '
              '${Formati.datum(dozvola.datumIsteka)}. Ovakva dozvola ne prolazi ni '
              'kad je odobrena — provjera pri rezervaciji gleda rok, ne status.',
            ),
            const SizedBox(height: Razmaci.l),
          ],
          if (dozvola.status == StatusDozvole.odbijena &&
              dozvola.razlogOdbijanja != null) ...[
            Obavjestenje.greska('Odbijeno: ${dozvola.razlogOdbijanja}'),
            const SizedBox(height: Razmaci.l),
          ],
          if (!dozvola.imaObjeStrane && dozvola.cekaVerifikaciju) ...[
            Obavjestenje.upozorenje(
              dozvola.imaPrednjuStranu
                  ? 'Klijent nije priložio zadnju stranu dozvole, na kojoj su '
                        'kategorije. Bez nje se dozvola ne može odobriti.'
                  : 'Nedostaje fotografija prednje strane dozvole.',
            ),
            const SizedBox(height: Razmaci.l),
          ],
          LayoutBuilder(
            builder: (context, ogranicenja) {
              final fotografijaPrikaz = Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  for (final strana in StranaDozvole.values) ...[
                    if (strana != StranaDozvole.values.first)
                      const SizedBox(height: Razmaci.m),
                    _Fotografija(
                      naslov: strana.naziv,
                      bajtovi: fotografije[strana],
                      ima: dozvola.imaStranu(strana),
                    ),
                  ],
                ],
              );

              final podaci = _Podaci(dozvola: dozvola, kategorije: kategorije);

              if (ogranicenja.maxWidth < 820) {
                return Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    fotografijaPrikaz,
                    const SizedBox(height: Razmaci.l),
                    podaci,
                  ],
                );
              }

              return Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(flex: 3, child: fotografijaPrikaz),
                  const SizedBox(width: Razmaci.xl),
                  Expanded(flex: 2, child: podaci),
                ],
              );
            },
          ),
        ],
      ),
    );
  }
}

class _Fotografija extends StatelessWidget {
  const _Fotografija({
    required this.naslov,
    required this.bajtovi,
    required this.ima,
  });

  final String naslov;
  final Uint8List? bajtovi;
  final bool ima;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.only(bottom: Razmaci.xs),
          child: Row(
            children: [
              Text(
                naslov,
                style: const TextStyle(
                  fontSize: 12.5,
                  fontWeight: FontWeight.w600,
                ),
              ),
              const SizedBox(width: Razmaci.s),
              if (!ima)
                const Text(
                  'nije priložena',
                  style: TextStyle(color: Boje.greskaTekst, fontSize: 11.5),
                ),
            ],
          ),
        ),
        _okvir(),
      ],
    );
  }

  Widget _okvir() {
    return Container(
      height: 230,
      decoration: BoxDecoration(
        color: Boje.platno,
        borderRadius: BorderRadius.circular(Zaobljenja.dugme),
        border: Border.all(color: Boje.ivica),
      ),
      clipBehavior: Clip.antiAlias,
      child: bajtovi != null
          ? InteractiveViewer(
              maxScale: 4,
              child: Center(child: Image.memory(bajtovi!, fit: BoxFit.contain)),
            )
          : Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(
                    ima
                        ? Icons.hourglass_empty
                        : Icons.image_not_supported_outlined,
                    size: 30,
                    color: Boje.ivicaJaca,
                  ),
                  const SizedBox(height: Razmaci.m),
                  Text(
                    ima
                        ? 'Fotografija se učitava…'
                        : 'Klijent nije priložio ovu stranu.',
                    style: const TextStyle(
                      color: Boje.tekstPrigusen,
                      fontSize: 12.5,
                    ),
                  ),
                ],
              ),
            ),
    );
  }
}

class _Podaci extends StatelessWidget {
  const _Podaci({required this.dozvola, required this.kategorije});

  final VozackaDozvola dozvola;
  final DozvoljeneKategorije? kategorije;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _Red('Broj dozvole', dozvola.brojDozvole),
        _Red('Izdata', Formati.datum(dozvola.datumIzdavanja)),
        _Red(
          'Vrijedi do',
          Formati.datum(dozvola.datumIsteka),
          upozorenje: dozvola.istekla,
        ),
        _Red('Datum rođenja', Formati.datum(dozvola.klijentDatumRodjenja)),
        _Red('Predano', Formati.datumIVrijeme(dozvola.datumKreiranja)),
        if (dozvola.datumVerifikacije != null)
          _Red(
            'Obrađeno',
            '${Formati.datumIVrijeme(dozvola.datumVerifikacije!)}'
                '${dozvola.verifikovaoKorisnikIme == null ? '' : ' · ${dozvola.verifikovaoKorisnikIme}'}',
          ),
        const SizedBox(height: Razmaci.l),
        const Text(
          'Kategorije na dozvoli',
          style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
        ),
        const SizedBox(height: Razmaci.s),
        if (dozvola.kategorije.isEmpty)
          const Text(
            'Nijedna kategorija nije upisana.',
            style: TextStyle(color: Boje.greskaTekst, fontSize: 12.5),
          )
        else
          Wrap(
            spacing: Razmaci.s,
            runSpacing: Razmaci.s,
            children: [
              for (final kategorija in dozvola.kategorije)
                StatusnaPilula(
                  tekst: kategorija,
                  pozadina: Boje.infoPozadina,
                  bojaTeksta: Boje.infoTekst,
                ),
            ],
          ),
        if (kategorije != null) ...[
          const SizedBox(height: Razmaci.l),
          _PravilaKategorija(kategorije: kategorije!),
        ],
      ],
    );
  }
}

/// Sta klijent smije voziti kad mu se dozvola odobri.
///
/// Uposlenik ovdje vidi i kategorije koje dozvola ne nosi upisane, a klijent ih
/// svejedno smije voziti - hijerarhija kategorija to daje. Bez tog objasnjenja
/// izgleda kao da sistem propusta nesto sto ne bi smio.
class _PravilaKategorija extends StatelessWidget {
  const _PravilaKategorija({required this.kategorije});

  final DozvoljeneKategorije kategorije;

  @override
  Widget build(BuildContext context) {
    final izvedene = kategorije.izvedene;

    return Container(
      padding: const EdgeInsets.all(Razmaci.m),
      decoration: BoxDecoration(
        color: kategorije.mozeRezervisati
            ? Boje.infoPozadina
            : Boje.upozorenjePozadina,
        borderRadius: BorderRadius.circular(Zaobljenja.dugme),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(
                kategorije.mozeRezervisati
                    ? Icons.info_outline
                    : Icons.warning_amber_outlined,
                size: 16,
                color: kategorije.mozeRezervisati
                    ? Boje.infoTekst
                    : Boje.upozorenjeTekst,
              ),
              const SizedBox(width: Razmaci.s),
              Text(
                'Šta smije voziti',
                style: TextStyle(
                  fontSize: 12.5,
                  fontWeight: FontWeight.w700,
                  color: kategorije.mozeRezervisati
                      ? Boje.infoTekst
                      : Boje.upozorenjeTekst,
                ),
              ),
            ],
          ),
          const SizedBox(height: Razmaci.s),
          Text(
            kategorije.obrazlozenje,
            style: TextStyle(
              fontSize: 12,
              height: 1.4,
              color: kategorije.mozeRezervisati
                  ? Boje.infoTekst
                  : Boje.upozorenjeTekst,
            ),
          ),
          if (izvedene.isNotEmpty) ...[
            const SizedBox(height: Razmaci.s),
            Text(
              'Kroz hijerarhiju kategorija dobija i: ${izvedene.join(', ')}.',
              style: TextStyle(
                fontSize: 12,
                height: 1.4,
                color: kategorije.mozeRezervisati
                    ? Boje.infoTekst
                    : Boje.upozorenjeTekst,
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _Red extends StatelessWidget {
  const _Red(this.natpis, this.vrijednost, {this.upozorenje = false});

  final String natpis;
  final String vrijednost;
  final bool upozorenje;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        children: [
          SizedBox(
            width: 120,
            child: Text(
              natpis,
              style: const TextStyle(color: Boje.tekstPrigusen, fontSize: 12.5),
            ),
          ),
          Expanded(
            child: Text(
              vrijednost,
              style: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w500,
                color: upozorenje ? Boje.greskaTekst : Boje.tekst,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
