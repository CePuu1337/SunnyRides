import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/dozvola.dart';
import '../../servisi/dozvola_servis.dart';
import '../../widgeti/kartica.dart';
import '../../widgeti/paginator.dart';
import '../../widgeti/polja.dart';
import '../../widgeti/sadrzaj.dart';
import 'dozvola_detalji.dart';

/// Verifikacija vozackih dozvola: lista zahtjeva lijevo, odabrani zahtjev desno.
///
/// Master-detail umjesto liste sa dijalogom, jer uposlenik ovdje radi jedno za
/// drugim - odobri, pa sljedeci. Dijalog bi se otvarao i zatvarao na svaki zahtjev.
class DozvoleEkran extends StatefulWidget {
  const DozvoleEkran({super.key});

  @override
  State<DozvoleEkran> createState() => _DozvoleEkranStanje();
}

class _DozvoleEkranStanje extends State<DozvoleEkran> {
  late final DozvolaServis _servis;

  UpitDozvola _upit = const UpitDozvola();
  Strana<VozackaDozvola> _strana = Strana.prazna();
  int? _odabrana;

  bool _ucitavanje = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = DozvolaServis(context.read<ApiKlijent>());
    _ucitaj();
  }

  Future<void> _ucitaj({bool zadrziOdabir = false}) async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      final strana = await _servis.lista(_upit);

      if (!mounted) {
        return;
      }

      setState(() {
        _strana = strana;
        _ucitavanje = false;

        // Nakon obrade odabrana dozvola cesto ispadne iz liste, jer filter po
        // podrazumijevanom prikazuje samo one koje cekaju. Tada se otvara sljedeca,
        // da uposlenik nastavi bez dodatnog klika.
        if (!zadrziOdabir || !strana.stavke.any((x) => x.id == _odabrana)) {
          _odabrana = strana.stavke.isEmpty ? null : strana.stavke.first.id;
        }
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

  void _promijeniUpit(UpitDozvola noviUpit) {
    setState(() => _upit = noviUpit);
    _ucitaj();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(Razmaci.ekranMargina),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            SizedBox(
              width: 420,
              child: Kartica(
                naslov: 'Zahtjevi',
                podnaslov: _strana.ukupno == null
                    ? 'Dozvole predane na verifikaciju'
                    : '${_strana.ukupno} po trenutnim filterima',
                bezUnutrasnjegRazmaka: true,
                rastegni: true,
                dijete: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    _Filteri(upit: _upit, naPromjenu: _promijeniUpit),
                    const Divider(height: 1),
                    Expanded(
                      child: Sadrzaj(
                        ucitavanje: _ucitavanje,
                        greska: _greska,
                        naPonovniPokusaj: _ucitaj,
                        dijete: _strana.jePrazna
                            ? const PrazanPopis(
                                poruka: 'Nema dozvola po ovim filterima.',
                                ikona: Icons.badge_outlined,
                              )
                            : ListView.separated(
                                itemCount: _strana.stavke.length,
                                separatorBuilder: (context, indeks) =>
                                    const Divider(height: 1),
                                itemBuilder: (context, indeks) {
                                  final dozvola = _strana.stavke[indeks];

                                  return _Red(
                                    dozvola: dozvola,
                                    odabrana: dozvola.id == _odabrana,
                                    naPritisak: () =>
                                        setState(() => _odabrana = dozvola.id),
                                  );
                                },
                              ),
                      ),
                    ),
                    Paginator(
                      stranica: _upit.stranica,
                      velicinaStranice: _upit.velicinaStranice,
                      prikazano: _strana.stavke.length,
                      ukupno: _strana.ukupno,
                      naStranicu: (stranica) =>
                          _promijeniUpit(_upit.kopija(stranica: stranica)),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(width: Razmaci.l),
            Expanded(
              child: _odabrana == null
                  ? const Kartica(
                      rastegni: true,
                      naslov: 'Detalji zahtjeva',
                      dijete: PrazanPopis(
                        poruka: 'Odaberite zahtjev iz liste.',
                        ikona: Icons.badge_outlined,
                      ),
                    )
                  : DozvolaDetalji(
                      key: ValueKey(_odabrana),
                      dozvolaId: _odabrana!,
                      servis: _servis,
                      naObradu: () => _ucitaj(zadrziOdabir: false),
                    ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Filteri extends StatelessWidget {
  const _Filteri({required this.upit, required this.naPromjenu});

  final UpitDozvola upit;
  final ValueChanged<UpitDozvola> naPromjenu;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(Razmaci.karticaUnutra),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          PoljePretrage(
            natpis: 'Klijent ili email',
            sirina: double.infinity,
            naPromjenu: (tekst) =>
                naPromjenu(upit.kopija(klijent: tekst, stranica: 0)),
          ),
          const SizedBox(height: Razmaci.m),
          Row(
            children: [
              Expanded(
                child: DropdownButtonFormField<StatusDozvole?>(
                  key: ValueKey(upit.status),
                  initialValue: upit.status,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Status'),
                  items: [
                    const DropdownMenuItem<StatusDozvole?>(
                      value: null,
                      child: Text('Sve'),
                    ),
                    for (final status in StatusDozvole.values)
                      DropdownMenuItem<StatusDozvole?>(
                        value: status,
                        child: Text(status.naziv),
                      ),
                  ],
                  onChanged: (status) => naPromjenu(
                    status == null
                        ? upit.kopija(ocistiStatus: true, stranica: 0)
                        : upit.kopija(status: status, stranica: 0),
                  ),
                ),
              ),
              const SizedBox(width: Razmaci.m),
              Expanded(
                child: DropdownButtonFormField<bool?>(
                  key: ValueKey(upit.samoIstekle),
                  initialValue: upit.samoIstekle,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Rok'),
                  items: const [
                    DropdownMenuItem<bool?>(
                      value: null,
                      child: Text('Svejedno'),
                    ),
                    DropdownMenuItem<bool?>(
                      value: true,
                      child: Text('Istekle'),
                    ),
                  ],
                  onChanged: (vrijednost) => naPromjenu(
                    vrijednost == null
                        ? upit.kopija(ocistiIstekle: true, stranica: 0)
                        : upit.kopija(samoIstekle: vrijednost, stranica: 0),
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _Red extends StatelessWidget {
  const _Red({
    required this.dozvola,
    required this.odabrana,
    required this.naPritisak,
  });

  final VozackaDozvola dozvola;
  final bool odabrana;
  final VoidCallback naPritisak;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: odabrana ? Boje.primarnaSvijetla : Colors.transparent,
      child: InkWell(
        onTap: naPritisak,
        child: Padding(
          padding: const EdgeInsets.symmetric(
            horizontal: Razmaci.karticaUnutra,
            vertical: Razmaci.m,
          ),
          child: Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      dozvola.klijentImePrezime ?? '',
                      style: const TextStyle(
                        fontSize: 13.5,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      '${dozvola.brojDozvole} · predano '
                      '${Formati.datum(dozvola.datumKreiranja)}',
                      style: const TextStyle(
                        color: Boje.tekstPrigusen,
                        fontSize: 11.5,
                      ),
                    ),
                  ],
                ),
              ),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  StatusnaPilula.dozvola(dozvola.status),
                  if (dozvola.istekla) ...[
                    const SizedBox(height: 4),
                    const Text(
                      'rok istekao',
                      style: TextStyle(color: Boje.greskaTekst, fontSize: 11),
                    ),
                  ],
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
