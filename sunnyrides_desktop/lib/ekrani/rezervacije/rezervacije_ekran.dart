import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/rezervacija.dart';
import '../../modeli/stavka_sifrarnika.dart';
import '../../servisi/rezervacija_servis.dart';
import '../../servisi/sifrarnik_servis.dart';
import '../../widgeti/kartica.dart';
import '../../widgeti/paginator.dart';
import '../../widgeti/polja.dart';
import '../../widgeti/sadrzaj.dart';
import '../../widgeti/slicica.dart';
import 'rezervacija_detalji.dart';

class RezervacijeEkran extends StatefulWidget {
  const RezervacijeEkran({super.key});

  @override
  State<RezervacijeEkran> createState() => _RezervacijeEkranStanje();
}

class _RezervacijeEkranStanje extends State<RezervacijeEkran> {
  late final RezervacijaServis _servis;
  late final SifrarnikServis _sifrarnici;

  UpitRezervacija _upit = const UpitRezervacija();
  Strana<Rezervacija> _strana = Strana.prazna();
  List<StavkaSifrarnika> _poslovnice = const [];

  bool _ucitavanje = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    final klijent = context.read<ApiKlijent>();
    _servis = RezervacijaServis(klijent);
    _sifrarnici = SifrarnikServis(klijent);

    _ucitajPoslovnice();
    _ucitaj();
  }

  Future<void> _ucitajPoslovnice() async {
    try {
      final poslovnice = await _sifrarnici.ucitaj(SifrarnikServis.poslovnice);

      if (mounted) {
        setState(() => _poslovnice = poslovnice);
      }
    } on ApiGreska {
      // Filter po poslovnici ostaje prazan, lista i bez njega radi.
    }
  }

  Future<void> _ucitaj() async {
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

  void _promijeniUpit(UpitRezervacija noviUpit) {
    setState(() => _upit = noviUpit);
    _ucitaj();
  }

  Future<void> _otvoriDetalje(Rezervacija rezervacija) async {
    final promijenjeno = await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (context) => RezervacijaDetalji(rezervacijaId: rezervacija.id),
      ),
    );

    if (promijenjeno == true) {
      _ucitaj();
    }
  }

  Future<void> _odaberiPeriod() async {
    final raspon = await showDateRangePicker(
      context: context,
      firstDate: DateTime(2020),
      lastDate: DateTime(DateTime.now().year + 2, 12, 31),
      currentDate: DateTime.now(),
      helpText: 'Period preklapanja rezervacije',
      saveText: 'Primijeni',
    );

    if (raspon == null) {
      return;
    }

    _promijeniUpit(
      _upit.kopija(
        periodOd: raspon.start,
        // Kraj dana, da rezervacija koja pocinje tog datuma popodne ne ispadne.
        periodDo: DateTime(
          raspon.end.year,
          raspon.end.month,
          raspon.end.day,
          23,
          59,
        ),
        stranica: 0,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(Razmaci.ekranMargina),
        child: Kartica(
          naslov: 'Rezervacije',
          podnaslov: 'Sve rezervacije agencije, najnovije prvo',
          bezUnutrasnjegRazmaka: true,
          dijete: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            mainAxisSize: MainAxisSize.min,
            children: [
              _Filteri(
                upit: _upit,
                poslovnice: _poslovnice,
                naPromjenu: _promijeniUpit,
                naPeriod: _odaberiPeriod,
              ),
              const Divider(height: 1),
              SizedBox(
                height: 460,
                child: Sadrzaj(
                  ucitavanje: _ucitavanje,
                  greska: _greska,
                  naPonovniPokusaj: _ucitaj,
                  dijete: _strana.jePrazna
                      ? const PrazanPopis(
                          poruka: 'Nema rezervacija koje odgovaraju filterima.',
                          ikona: Icons.receipt_long_outlined,
                        )
                      : _Tabela(
                          rezervacije: _strana.stavke,
                          naDetalje: _otvoriDetalje,
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
    );
  }
}

class _Filteri extends StatelessWidget {
  const _Filteri({
    required this.upit,
    required this.poslovnice,
    required this.naPromjenu,
    required this.naPeriod,
  });

  final UpitRezervacija upit;
  final List<StavkaSifrarnika> poslovnice;
  final ValueChanged<UpitRezervacija> naPromjenu;
  final VoidCallback naPeriod;

  @override
  Widget build(BuildContext context) {
    final imaPeriod = upit.periodOd != null;

    return Padding(
      padding: const EdgeInsets.all(Razmaci.karticaUnutra),
      child: Wrap(
        spacing: Razmaci.m,
        runSpacing: Razmaci.m,
        crossAxisAlignment: WrapCrossAlignment.center,
        children: [
          PoljePretrage(
            natpis: 'Broj rezervacije',
            sirina: 200,
            naPromjenu: (tekst) =>
                naPromjenu(upit.kopija(broj: tekst, stranica: 0)),
          ),
          PoljePretrage(
            natpis: 'Klijent (ime ili email)',
            sirina: 230,
            naPromjenu: (tekst) =>
                naPromjenu(upit.kopija(klijent: tekst, stranica: 0)),
          ),
          SizedBox(
            width: 180,
            child: DropdownButtonFormField<StatusRezervacije?>(
              key: ValueKey(upit.status),
              initialValue: upit.status,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'Status'),
              items: [
                const DropdownMenuItem<StatusRezervacije?>(
                  value: null,
                  child: Text('Svi'),
                ),
                for (final status in StatusRezervacije.values)
                  DropdownMenuItem<StatusRezervacije?>(
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
          PadajuciSifrarnik(
            natpis: 'Poslovnica',
            stavke: poslovnice,
            odabrano: upit.poslovnicaId,
            naPromjenu: (id) => naPromjenu(
              id == null
                  ? upit.kopija(ocistiPoslovnicu: true, stranica: 0)
                  : upit.kopija(poslovnicaId: id, stranica: 0),
            ),
          ),
          SizedBox(
            width: 160,
            child: DropdownButtonFormField<bool?>(
              key: ValueKey(upit.isPaid),
              initialValue: upit.isPaid,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'Plaćanje'),
              items: const [
                DropdownMenuItem<bool?>(value: null, child: Text('Sve')),
                DropdownMenuItem<bool?>(value: true, child: Text('Plaćeno')),
                DropdownMenuItem<bool?>(value: false, child: Text('Neplaćeno')),
              ],
              onChanged: (vrijednost) => naPromjenu(
                vrijednost == null
                    ? upit.kopija(ocistiPlaceno: true, stranica: 0)
                    : upit.kopija(isPaid: vrijednost, stranica: 0),
              ),
            ),
          ),
          OutlinedButton.icon(
            onPressed: naPeriod,
            icon: const Icon(Icons.date_range_outlined, size: 17),
            label: Text(
              imaPeriod
                  ? '${Formati.datum(upit.periodOd!)} – ${Formati.datum(upit.periodDo!)}'
                  : 'Period',
            ),
          ),
          if (imaPeriod)
            TextButton(
              onPressed: () =>
                  naPromjenu(upit.kopija(ocistiPeriod: true, stranica: 0)),
              child: const Text('Poništi period'),
            ),
        ],
      ),
    );
  }
}

class _Tabela extends StatelessWidget {
  const _Tabela({required this.rezervacije, required this.naDetalje});

  final List<Rezervacija> rezervacije;
  final ValueChanged<Rezervacija> naDetalje;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, ogranicenja) => SingleChildScrollView(
        child: SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: ConstrainedBox(
            constraints: BoxConstraints(minWidth: ogranicenja.maxWidth),
            child: DataTable(
              columnSpacing: Razmaci.l,
              horizontalMargin: Razmaci.karticaUnutra,
              headingRowHeight: 40,
              dataRowMinHeight: 56,
              dataRowMaxHeight: 62,
              columns: const [
                DataColumn(label: Text('BROJ')),
                DataColumn(label: Text('KLIJENT')),
                DataColumn(label: Text('VOZILO')),
                DataColumn(label: Text('PERIOD')),
                DataColumn(label: Text('POSLOVNICA')),
                DataColumn(label: Text('IZNOS')),
                DataColumn(label: Text('PLAĆANJE')),
                DataColumn(label: Text('STATUS')),
                DataColumn(label: Text('')),
              ],
              rows: [
                for (final rezervacija in rezervacije)
                  DataRow(
                    cells: [
                      DataCell(
                        Text(
                          rezervacija.broj,
                          style: const TextStyle(fontWeight: FontWeight.w600),
                        ),
                      ),
                      DataCell(
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Text(rezervacija.klijentImePrezime ?? ''),
                            Text(
                              rezervacija.klijentEmail ?? '',
                              style: const TextStyle(
                                color: Boje.tekstPrigusen,
                                fontSize: 11.5,
                              ),
                            ),
                          ],
                        ),
                      ),
                      DataCell(
                        Row(
                          children: [
                            Slicica(
                              putanja: rezervacija.thumbnailUrl,
                              zamjenskaIkona: Icons.two_wheeler_outlined,
                            ),
                            const SizedBox(width: Razmaci.m),
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Text(rezervacija.vozilo),
                                Text(
                                  rezervacija.registarskaOznaka ?? '',
                                  style: const TextStyle(
                                    color: Boje.tekstPrigusen,
                                    fontSize: 11.5,
                                  ),
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),
                      DataCell(
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Text(
                              '${Formati.danIMjesec(rezervacija.datumOd)} – '
                              '${Formati.datum(rezervacija.datumDo)}',
                            ),
                            Text(
                              Formati.trajanje(
                                rezervacija.datumOd,
                                rezervacija.datumDo,
                              ),
                              style: const TextStyle(
                                color: Boje.tekstPrigusen,
                                fontSize: 11.5,
                              ),
                            ),
                          ],
                        ),
                      ),
                      DataCell(Text(rezervacija.poslovnicaNaziv ?? '')),
                      DataCell(Text(Formati.novac(rezervacija.ukupanIznos))),
                      DataCell(StatusnaPilula.placeno(rezervacija.isPaid)),
                      DataCell(StatusnaPilula.rezervacija(rezervacija.status)),
                      DataCell(
                        TextButton(
                          onPressed: () => naDetalje(rezervacija),
                          child: const Text('Detalji'),
                        ),
                      ),
                    ],
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
