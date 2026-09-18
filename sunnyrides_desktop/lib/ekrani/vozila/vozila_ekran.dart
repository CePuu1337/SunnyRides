import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/stavka_sifrarnika.dart';
import '../../modeli/vozilo.dart';
import '../../servisi/sifrarnik_servis.dart';
import '../../servisi/vozilo_servis.dart';
import '../../stanje/sesija.dart';
import '../../widgeti/kartica.dart';
import '../../widgeti/paginator.dart';
import '../../widgeti/polja.dart';
import '../../widgeti/sadrzaj.dart';
import '../../widgeti/slicica.dart';
import 'vozilo_forma.dart';

class VozilaEkran extends StatefulWidget {
  const VozilaEkran({super.key});

  @override
  State<VozilaEkran> createState() => _VozilaEkranStanje();
}

class _VozilaEkranStanje extends State<VozilaEkran> {
  late final VoziloServis _servis;
  late final SifrarnikServis _sifrarnici;

  UpitVozila _upit = const UpitVozila();
  Strana<Vozilo> _strana = Strana.prazna();

  List<StavkaSifrarnika> _marke = const [];
  List<StavkaSifrarnika> _tipovi = const [];
  List<StavkaSifrarnika> _poslovnice = const [];

  bool _ucitavanje = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    final klijent = context.read<ApiKlijent>();
    _servis = VoziloServis(klijent);
    _sifrarnici = SifrarnikServis(klijent);

    _ucitajSve();
  }

  Future<void> _ucitajSve() async {
    await _ucitajSifrarnike();
    await _ucitaj();
  }

  Future<void> _ucitajSifrarnike() async {
    try {
      final marke = await _sifrarnici.ucitaj(SifrarnikServis.marke);
      final tipovi = await _sifrarnici.ucitaj(SifrarnikServis.tipoviVozila);
      final poslovnice = await _sifrarnici.ucitaj(SifrarnikServis.poslovnice);

      if (!mounted) {
        return;
      }

      setState(() {
        _marke = marke;
        _tipovi = tipovi;
        _poslovnice = poslovnice;
      });
    } on ApiGreska {
      // Filteri ostaju prazni. Lista i bez njih radi, pa se ekran ne obara zbog toga.
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

  /// Svaka promjena filtera vraca na prvu stranicu. Inace bi se desilo da uzi
  /// filter ostavi korisnika na stranici koja vise ne postoji, pa lista ispadne
  /// prazna iako rezultata ima.
  void _promijeniUpit(UpitVozila noviUpit) {
    setState(() => _upit = noviUpit);
    _ucitaj();
  }

  Future<void> _otvoriFormu({Vozilo? vozilo}) async {
    final sacuvano = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (context) => VoziloForma(vozilo: vozilo),
    );

    if (sacuvano == true) {
      _ucitaj();
    }
  }

  Future<void> _deaktiviraj(Vozilo vozilo) async {
    final potvrda = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Deaktivacija vozila'),
        content: Text(
          '${vozilo.puniNaziv} (${vozilo.registarskaOznaka}) više neće biti dostupno '
          'za nove rezervacije. Postojeće rezervacije ostaju netaknute.',
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
      await _servis.obrisi(vozilo.id);
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
    final jeAdministrator =
        context.read<Sesija>().korisnik?.jeAdministrator ?? false;

    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(Razmaci.ekranMargina),
        child: Kartica(
          naslov: 'Flota',
          podnaslov: 'Skuteri, motocikli i kvadovi sa tarifama i poslovnicom',
          bezUnutrasnjegRazmaka: true,
          akcija: ElevatedButton.icon(
            onPressed: () => _otvoriFormu(),
            icon: const Icon(Icons.add, size: 18),
            label: const Text('Novo vozilo'),
          ),
          dijete: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            mainAxisSize: MainAxisSize.min,
            children: [
              _Filteri(
                upit: _upit,
                marke: _marke,
                tipovi: _tipovi,
                poslovnice: _poslovnice,
                naPromjenu: _promijeniUpit,
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
                          poruka: 'Nema vozila koja odgovaraju filterima.',
                          ikona: Icons.two_wheeler_outlined,
                        )
                      : _Tabela(
                          vozila: _strana.stavke,
                          jeAdministrator: jeAdministrator,
                          naIzmjenu: (vozilo) => _otvoriFormu(vozilo: vozilo),
                          naDeaktivaciju: _deaktiviraj,
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
    required this.marke,
    required this.tipovi,
    required this.poslovnice,
    required this.naPromjenu,
  });

  final UpitVozila upit;
  final List<StavkaSifrarnika> marke;
  final List<StavkaSifrarnika> tipovi;
  final List<StavkaSifrarnika> poslovnice;
  final ValueChanged<UpitVozila> naPromjenu;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(Razmaci.karticaUnutra),
      child: Wrap(
        spacing: Razmaci.m,
        runSpacing: Razmaci.m,
        crossAxisAlignment: WrapCrossAlignment.center,
        children: [
          PoljePretrage(
            natpis: 'Pretraga po modelu',
            naPromjenu: (tekst) =>
                naPromjenu(upit.kopija(pretraga: tekst, stranica: 0)),
          ),
          PadajuciSifrarnik(
            natpis: 'Marka',
            stavke: marke,
            odabrano: upit.markaId,
            naPromjenu: (id) => naPromjenu(
              id == null
                  ? upit.kopija(ocistiMarku: true, stranica: 0)
                  : upit.kopija(markaId: id, stranica: 0),
            ),
          ),
          PadajuciSifrarnik(
            natpis: 'Tip vozila',
            stavke: tipovi,
            odabrano: upit.tipVozilaId,
            naPromjenu: (id) => naPromjenu(
              id == null
                  ? upit.kopija(ocistiTip: true, stranica: 0)
                  : upit.kopija(tipVozilaId: id, stranica: 0),
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
            width: 170,
            child: DropdownButtonFormField<bool?>(
              key: ValueKey(upit.aktivno),
              initialValue: upit.aktivno,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'Stanje'),
              items: const [
                DropdownMenuItem<bool?>(value: null, child: Text('Sva')),
                DropdownMenuItem<bool?>(value: true, child: Text('Aktivna')),
                DropdownMenuItem<bool?>(
                  value: false,
                  child: Text('Deaktivirana'),
                ),
              ],
              onChanged: (vrijednost) => naPromjenu(
                vrijednost == null
                    ? upit.kopija(ocistiAktivno: true, stranica: 0)
                    : upit.kopija(aktivno: vrijednost, stranica: 0),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _Tabela extends StatelessWidget {
  const _Tabela({
    required this.vozila,
    required this.jeAdministrator,
    required this.naIzmjenu,
    required this.naDeaktivaciju,
  });

  final List<Vozilo> vozila;
  final bool jeAdministrator;
  final ValueChanged<Vozilo> naIzmjenu;
  final ValueChanged<Vozilo> naDeaktivaciju;

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
                DataColumn(label: Text('VOZILO')),
                DataColumn(label: Text('REGISTRACIJA')),
                DataColumn(label: Text('TIP')),
                DataColumn(label: Text('KATEGORIJA')),
                DataColumn(label: Text('POSLOVNICA')),
                DataColumn(label: Text('DNEVNO')),
                DataColumn(label: Text('DEPOZIT')),
                DataColumn(label: Text('STANJE')),
                DataColumn(label: Text('')),
              ],
              rows: [
                for (final vozilo in vozila)
                  DataRow(
                    cells: [
                      DataCell(
                        Row(
                          children: [
                            Slicica(
                              putanja: vozilo.thumbnailUrl,
                              zamjenskaIkona: Icons.two_wheeler_outlined,
                            ),
                            const SizedBox(width: Razmaci.m),
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Text(
                                  vozilo.puniNaziv,
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                                Text(
                                  '${vozilo.godinaProizvodnje}. · ${vozilo.pogon}',
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
                      DataCell(Text(vozilo.registarskaOznaka)),
                      DataCell(Text(vozilo.tipVozilaNaziv ?? '')),
                      DataCell(Text(vozilo.kategorijaDozvoleOznaka ?? '')),
                      DataCell(
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Text(vozilo.poslovnicaNaziv ?? ''),
                            Text(
                              vozilo.gradNaziv ?? '',
                              style: const TextStyle(
                                color: Boje.tekstPrigusen,
                                fontSize: 11.5,
                              ),
                            ),
                          ],
                        ),
                      ),
                      DataCell(Text(Formati.novac(vozilo.dnevnaTarifa))),
                      DataCell(Text(Formati.novac(vozilo.iznosDepozita))),
                      DataCell(
                        vozilo.aktivno
                            ? const StatusnaPilula(
                                tekst: 'Aktivno',
                                pozadina: Boje.uspjehPozadina,
                                bojaTeksta: Boje.uspjehTekst,
                              )
                            : const StatusnaPilula(
                                tekst: 'Deaktivirano',
                                pozadina: Boje.neutralnoPozadina,
                                bojaTeksta: Boje.neutralnoTekst,
                              ),
                      ),
                      DataCell(
                        Row(
                          children: [
                            IconButton(
                              tooltip: 'Izmijeni',
                              onPressed: () => naIzmjenu(vozilo),
                              icon: const Icon(Icons.edit_outlined, size: 18),
                            ),
                            if (jeAdministrator && vozilo.aktivno)
                              IconButton(
                                tooltip: 'Deaktiviraj',
                                onPressed: () => naDeaktivaciju(vozilo),
                                icon: const Icon(
                                  Icons.block_outlined,
                                  size: 18,
                                  color: Boje.greskaTekst,
                                ),
                              ),
                          ],
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
