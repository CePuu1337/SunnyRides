import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/kalendar.dart';
import '../../modeli/stavka_sifrarnika.dart';
import '../../servisi/kalendar_servis.dart';
import '../../servisi/sifrarnik_servis.dart';
import '../../widgeti/kartica.dart';
import '../../widgeti/polja.dart';
import '../../widgeti/sadrzaj.dart';
import '../rezervacije/rezervacija_detalji.dart';
import 'mreza_kalendara.dart';
import 'rucni_unos_dijalog.dart';

class KalendarEkran extends StatefulWidget {
  const KalendarEkran({super.key});

  @override
  State<KalendarEkran> createState() => _KalendarEkranStanje();
}

class _KalendarEkranStanje extends State<KalendarEkran> {
  static const _brojDana = 7;

  late final KalendarServis _servis;
  late final SifrarnikServis _sifrarnici;

  late DateTime _pocetak;
  int? _poslovnicaId;
  int? _tipVozilaId;
  String _vozilo = '';

  KalendarFlote? _kalendar;
  List<StavkaSifrarnika> _poslovnice = const [];
  List<StavkaSifrarnika> _tipovi = const [];

  bool _ucitavanje = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    final klijent = context.read<ApiKlijent>();
    _servis = KalendarServis(klijent);
    _sifrarnici = SifrarnikServis(klijent);

    _pocetak = _ponedjeljak(DateTime.now());

    _ucitajSifrarnike();
    _ucitaj();
  }

  /// Ponoc ponedjeljka u sedmici kojoj pripada zadani dan, po lokalnom vremenu.
  static DateTime _ponedjeljak(DateTime dan) {
    final ponoc = DateTime(dan.year, dan.month, dan.day);

    return ponoc.subtract(Duration(days: ponoc.weekday - 1));
  }

  Future<void> _ucitajSifrarnike() async {
    try {
      final poslovnice = await _sifrarnici.ucitaj(SifrarnikServis.poslovnice);
      final tipovi = await _sifrarnici.ucitaj(SifrarnikServis.tipoviVozila);

      if (mounted) {
        setState(() {
          _poslovnice = poslovnice;
          _tipovi = tipovi;
        });
      }
    } on ApiGreska {
      // Filteri ostaju prazni, kalendar i bez njih radi.
    }
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      // Granice sedmice se racunaju po lokalnom vremenu, a salju u UTC-u. Tako
      // ponedjeljak u kalendaru pocinje u ponoc ovdje, a ne u dva ujutro.
      final kalendar = await _servis.zauzetost(
        od: _pocetak,
        doDatuma: _pocetak.add(const Duration(days: _brojDana)),
        poslovnicaId: _poslovnicaId,
        tipVozilaId: _tipVozilaId,
        vozilo: _vozilo.isEmpty ? null : _vozilo,
      );

      if (!mounted) {
        return;
      }

      setState(() {
        _kalendar = kalendar;
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

  void _pomjeri(int sedmica) {
    setState(() => _pocetak = _pocetak.add(Duration(days: 7 * sedmica)));
    _ucitaj();
  }

  void _ovaSedmica() {
    setState(() => _pocetak = _ponedjeljak(DateTime.now()));
    _ucitaj();
  }

  Future<void> _otvoriRezervaciju(int rezervacijaId) async {
    final promijenjeno = await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (context) => RezervacijaDetalji(rezervacijaId: rezervacijaId),
      ),
    );

    if (promijenjeno == true) {
      _ucitaj();
    }
  }

  Future<void> _rucniUnos(RedKalendara red, DateTime dan) async {
    final kalendar = _kalendar;

    // Dan u proslosti nema sta nuditi - rezervacija ne moze poceti juce.
    final danas = DateTime.now();
    final krajDana = DateTime(dan.year, dan.month, dan.day, 23, 59);

    if (krajDana.isBefore(danas)) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Za prošle dane se rezervacija ne može unijeti.'),
        ),
      );

      return;
    }

    final ishod = await showDialog<IshodUnosa>(
      context: context,
      barrierDismissible: false,
      builder: (context) => RucniUnosDijalog(
        vozilo: red,
        dan: dan,
        bufferSati: kalendar?.bufferSati ?? 0,
      ),
    );

    if (ishod == null) {
      return;
    }

    _ucitaj();

    if (!mounted) {
      return;
    }

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          ishod == IshodUnosa.rezervacija
              ? 'Rezervacija je unesena i drži termin dok klijent ne plati.'
              : 'Vozilo je blokirano i više se ne nudi za taj period.',
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final kraj = _pocetak.add(const Duration(days: _brojDana - 1));

    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(Razmaci.ekranMargina),
        child: Kartica(
          naslov: '${Formati.danIMjesec(_pocetak)} – ${Formati.datum(kraj)}',
          podnaslov: 'Zauzeće po satu. Pređite mišem preko bloka za detalje.',
          bezUnutrasnjegRazmaka: true,
          akcija: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              IconButton(
                tooltip: 'Prethodna sedmica',
                onPressed: () => _pomjeri(-1),
                icon: const Icon(Icons.chevron_left),
              ),
              OutlinedButton(
                onPressed: _ovaSedmica,
                child: const Text('Ova sedmica'),
              ),
              IconButton(
                tooltip: 'Sljedeća sedmica',
                onPressed: () => _pomjeri(1),
                icon: const Icon(Icons.chevron_right),
              ),
            ],
          ),
          dijete: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            mainAxisSize: MainAxisSize.min,
            children: [
              Padding(
                padding: const EdgeInsets.all(Razmaci.karticaUnutra),
                child: Wrap(
                  spacing: Razmaci.m,
                  runSpacing: Razmaci.m,
                  children: [
                    PoljePretrage(
                      natpis: 'Model ili registracija',
                      naPromjenu: (tekst) {
                        _vozilo = tekst;
                        _ucitaj();
                      },
                    ),
                    PadajuciSifrarnik(
                      natpis: 'Poslovnica',
                      stavke: _poslovnice,
                      odabrano: _poslovnicaId,
                      naPromjenu: (id) {
                        setState(() => _poslovnicaId = id);
                        _ucitaj();
                      },
                    ),
                    PadajuciSifrarnik(
                      natpis: 'Tip vozila',
                      stavke: _tipovi,
                      odabrano: _tipVozilaId,
                      naPromjenu: (id) {
                        setState(() => _tipVozilaId = id);
                        _ucitaj();
                      },
                    ),
                  ],
                ),
              ),
              const Divider(height: 1),
              SizedBox(
                height: 520,
                child: Sadrzaj(
                  ucitavanje: _ucitavanje,
                  greska: _greska,
                  naPonovniPokusaj: _ucitaj,
                  dijete: (_kalendar?.vozila.isEmpty ?? true)
                      ? const PrazanPopis(
                          poruka: 'Nema vozila koja odgovaraju filterima.',
                          ikona: Icons.calendar_month_outlined,
                        )
                      : MrezaKalendara(
                          pocetak: _pocetak,
                          brojDana: _brojDana,
                          bufferSati: _kalendar!.bufferSati,
                          redovi: _kalendar!.vozila,
                          naRezervaciju: _otvoriRezervaciju,
                          naSlobodanDan: _rucniUnos,
                        ),
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: Razmaci.karticaUnutra,
                  vertical: Razmaci.m,
                ),
                decoration: const BoxDecoration(
                  border: Border(top: BorderSide(color: Boje.ivica)),
                ),
                child: const Row(
                  children: [
                    Expanded(child: LegendaKalendara()),
                    SizedBox(width: Razmaci.l),
                    Icon(
                      Icons.touch_app_outlined,
                      size: 16,
                      color: Boje.tekstPrigusen,
                    ),
                    SizedBox(width: Razmaci.xs),
                    Text(
                      'Klik na slobodan dan otvara ručni unos rezervacije',
                      style: TextStyle(color: Boje.tekstPrigusen, fontSize: 12),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
