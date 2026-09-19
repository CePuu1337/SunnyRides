import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/kalendar.dart';
import '../../widgeti/slicica.dart';

/// Sedmica flote: vozila u redovima, dani u kolonama, zauzeca kao blokovi.
///
/// Blokovi se ne lijepe za cijele dane nego se crtaju po satu. Najam od utorka u
/// podne do cetvrtka ujutro zauzima tacno toliko, pa se vidi da je u utorak ujutro
/// vozilo jos slobodno.
class MrezaKalendara extends StatelessWidget {
  const MrezaKalendara({
    super.key,
    required this.pocetak,
    required this.brojDana,
    required this.bufferSati,
    required this.redovi,
    required this.naRezervaciju,
    required this.naSlobodanDan,
  });

  /// Ponoc prvog dana, po lokalnom vremenu.
  final DateTime pocetak;
  final int brojDana;
  final double bufferSati;
  final List<RedKalendara> redovi;
  final ValueChanged<int> naRezervaciju;
  final void Function(RedKalendara red, DateTime dan) naSlobodanDan;

  static const sirinaVozila = 250.0;
  static const visinaReda = 58.0;

  static const _daniUSedmici = [
    'Pon',
    'Uto',
    'Sri',
    'Čet',
    'Pet',
    'Sub',
    'Ned',
  ];

  DateTime get _kraj => pocetak.add(Duration(days: brojDana));

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _zaglavlje(),
        const Divider(height: 1),
        Expanded(
          child: ListView.separated(
            itemCount: redovi.length,
            separatorBuilder: (context, indeks) => const Divider(height: 1),
            itemBuilder: (context, indeks) => _red(context, redovi[indeks]),
          ),
        ),
      ],
    );
  }

  Widget _zaglavlje() {
    final danas = DateTime.now();

    return SizedBox(
      height: 44,
      child: Row(
        children: [
          const SizedBox(
            width: sirinaVozila,
            child: Padding(
              padding: EdgeInsets.symmetric(horizontal: Razmaci.karticaUnutra),
              child: Text(
                'VOZILO',
                style: TextStyle(
                  color: Boje.tekstPrigusen,
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                  letterSpacing: 0.4,
                ),
              ),
            ),
          ),
          for (var i = 0; i < brojDana; i++)
            Expanded(
              child: Builder(
                builder: (context) {
                  final dan = pocetak.add(Duration(days: i));
                  final jeDanas =
                      dan.year == danas.year &&
                      dan.month == danas.month &&
                      dan.day == danas.day;

                  return Container(
                    alignment: Alignment.center,
                    decoration: BoxDecoration(
                      color: jeDanas ? Boje.primarnaSvijetla : null,
                      border: const Border(left: BorderSide(color: Boje.ivica)),
                    ),
                    child: Text(
                      '${_daniUSedmici[dan.weekday - 1]} ${dan.day}.${dan.month}.',
                      style: TextStyle(
                        fontSize: 12.5,
                        fontWeight: jeDanas ? FontWeight.w700 : FontWeight.w600,
                        color: jeDanas ? Boje.primarnaTamnija : Boje.tekstBlazi,
                      ),
                    ),
                  );
                },
              ),
            ),
        ],
      ),
    );
  }

  Widget _red(BuildContext context, RedKalendara red) {
    return SizedBox(
      height: visinaReda,
      child: Row(
        children: [
          SizedBox(
            width: sirinaVozila,
            child: Padding(
              padding: const EdgeInsets.symmetric(
                horizontal: Razmaci.karticaUnutra,
              ),
              child: Row(
                children: [
                  Slicica(
                    putanja: red.thumbnailUrl,
                    sirina: 44,
                    visina: 32,
                    zamjenskaIkona: Icons.two_wheeler_outlined,
                  ),
                  const SizedBox(width: Razmaci.m),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Text(
                          red.vozilo,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        Text(
                          '${red.registarskaOznaka} · ${red.poslovnica}',
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            color: Boje.tekstPrigusen,
                            fontSize: 11,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
          Expanded(
            child: LayoutBuilder(
              builder: (context, ogranicenja) =>
                  _vremenskaTraka(red, ogranicenja.maxWidth),
            ),
          ),
        ],
      ),
    );
  }

  Widget _vremenskaTraka(RedKalendara red, double sirina) {
    final ukupnoMinuta = _kraj.difference(pocetak).inMinutes.toDouble();

    double x(DateTime trenutak) {
      final minuta = trenutak
          .toLocal()
          .difference(pocetak)
          .inMinutes
          .toDouble();

      return (minuta / ukupnoMinuta * sirina).clamp(0, sirina);
    }

    final sirinaDana = sirina / brojDana;

    return GestureDetector(
      behavior: HitTestBehavior.opaque,
      // Klik na prazno mjesto otvara rucni unos za taj dan. Klik na blok ne dolazi
      // dovde, jer blok sam hvata dodir.
      onTapUp: (detalji) {
        final indeksDana = (detalji.localPosition.dx / sirinaDana)
            .floor()
            .clamp(0, brojDana - 1);

        naSlobodanDan(red, pocetak.add(Duration(days: indeksDana)));
      },
      child: Stack(
        children: [
          Row(
            children: [
              for (var i = 0; i < brojDana; i++)
                Expanded(
                  child: Container(
                    decoration: BoxDecoration(
                      color: i >= 5 ? Boje.platno.withValues(alpha: 0.6) : null,
                      border: const Border(left: BorderSide(color: Boje.ivica)),
                    ),
                  ),
                ),
            ],
          ),
          for (final blok in red.blokovi) ...[
            if (!blok.jeBlokada && bufferSati > 0)
              _buffer(
                blok,
                x(blok.doDatuma),
                x(
                  blok.doDatuma.add(
                    Duration(minutes: (bufferSati * 60).round()),
                  ),
                ),
              ),
            _blok(blok, x(blok.od), x(blok.doDatuma)),
          ],
        ],
      ),
    );
  }

  Widget _buffer(BlokKalendara blok, double lijevo, double desno) {
    if (desno - lijevo < 1) {
      return const SizedBox.shrink();
    }

    return Positioned(
      left: lijevo,
      width: desno - lijevo,
      top: 14,
      bottom: 14,
      child: Tooltip(
        message: 'Priprema vozila nakon najma (${Formati.broj(bufferSati)} h)',
        child: Container(
          decoration: BoxDecoration(
            color: Boje.ivica.withValues(alpha: 0.7),
            borderRadius: const BorderRadius.horizontal(
              right: Radius.circular(4),
            ),
          ),
        ),
      ),
    );
  }

  Widget _blok(BlokKalendara blok, double lijevo, double desno) {
    final sirina = desno - lijevo;

    if (sirina < 1) {
      return const SizedBox.shrink();
    }

    final (pozadina, tekst, ivica) = _boje(blok);
    final natpis = blok.jeBlokada
        ? (blok.razlog?.isNotEmpty == true ? blok.razlog! : 'Blokada')
        : (blok.klijent ?? blok.broj ?? '');

    return Positioned(
      left: lijevo,
      width: sirina,
      top: 9,
      bottom: 9,
      child: Tooltip(
        message: _opis(blok),
        waitDuration: const Duration(milliseconds: 300),
        child: MouseRegion(
          cursor: blok.jeBlokada ? MouseCursor.defer : SystemMouseCursors.click,
          child: GestureDetector(
            onTap: blok.rezervacijaId == null
                ? null
                : () => naRezervaciju(blok.rezervacijaId!),
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: Razmaci.s),
              alignment: Alignment.centerLeft,
              decoration: BoxDecoration(
                color: pozadina,
                borderRadius: BorderRadius.circular(4),
                border: Border(left: BorderSide(color: ivica, width: 3)),
              ),
              child: sirina < 36
                  ? null
                  : Text(
                      natpis,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: tekst,
                        fontSize: 11.5,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
            ),
          ),
        ),
      ),
    );
  }

  static (Color, Color, Color) _boje(BlokKalendara blok) {
    if (blok.jeBlokada) {
      return (Boje.greskaPozadina, Boje.greskaTekst, Boje.greska);
    }

    switch (blok.status) {
      case StatusRezervacije.naCekanju:
        return (Boje.upozorenjePozadina, Boje.upozorenjeTekst, Boje.upozorenje);
      case StatusRezervacije.zavrsena:
        return (Boje.infoPozadina, Boje.infoTekst, Boje.info);
      case StatusRezervacije.potvrdjena:
      default:
        return (Boje.uspjehPozadina, Boje.uspjehTekst, Boje.uspjeh);
    }
  }

  static String _opis(BlokKalendara blok) {
    final period =
        '${Formati.datumIVrijeme(blok.od)} – ${Formati.datumIVrijeme(blok.doDatuma)}';

    if (blok.jeBlokada) {
      return 'Blokada vozila\n$period${blok.razlog == null ? '' : '\n${blok.razlog}'}';
    }

    final redovi = <String>[
      '${blok.broj ?? ''} · ${blok.status?.naziv ?? ''}',
      if (blok.klijent != null) blok.klijent!,
      period,
      if (blok.status == StatusRezervacije.naCekanju && blok.drziDo != null)
        'Termin se drži do ${Formati.datumIVrijeme(blok.drziDo!)}',
    ];

    return redovi.join('\n');
  }
}

/// Objasnjenje boja ispod kalendara.
class LegendaKalendara extends StatelessWidget {
  const LegendaKalendara({super.key});

  @override
  Widget build(BuildContext context) {
    return const Wrap(
      spacing: Razmaci.l,
      runSpacing: Razmaci.s,
      children: [
        _Stavka(boja: Boje.uspjeh, natpis: 'Potvrđena'),
        _Stavka(boja: Boje.upozorenje, natpis: 'Čeka plaćanje'),
        _Stavka(boja: Boje.info, natpis: 'Završena'),
        _Stavka(boja: Boje.greska, natpis: 'Blokada vozila'),
        _Stavka(boja: Boje.ivicaJaca, natpis: 'Priprema između najmova'),
      ],
    );
  }
}

class _Stavka extends StatelessWidget {
  const _Stavka({required this.boja, required this.natpis});

  final Color boja;
  final String natpis;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 12,
          height: 12,
          decoration: BoxDecoration(
            color: boja,
            borderRadius: BorderRadius.circular(3),
          ),
        ),
        const SizedBox(width: Razmaci.s),
        Text(
          natpis,
          style: const TextStyle(fontSize: 12, color: Boje.tekstBlazi),
        ),
      ],
    );
  }
}
