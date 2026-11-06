import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/stavka_sifrarnika.dart';
import '../../modeli/zaliha_opreme.dart';
import '../../servisi/sifrarnik_servis.dart';
import '../../servisi/zalihe_opreme_servis.dart';
import '../../stanje/opseg_poslovnice.dart';
import '../../stanje/sesija.dart';
import '../../widgeti/kartica.dart';
import '../../widgeti/paginator.dart';
import '../../widgeti/polja.dart';
import '../../widgeti/potvrda.dart';
import '../../widgeti/sadrzaj.dart';
import 'zaliha_forma.dart';

/// Zalihe opreme po poslovnici. Administrator vidi i mijenja sve poslovnice,
/// uposlenik samo svoju.
///
/// Od ovih količina se pri rezervaciji oduzima oprema koju u istom terminu drže
/// druge rezervacije, pa klijent dobija ponudu samo onoga što je stvarno slobodno.
class ZaliheEkran extends StatefulWidget {
  const ZaliheEkran({super.key});

  @override
  State<ZaliheEkran> createState() => _ZaliheEkranStanje();
}

class _ZaliheEkranStanje extends State<ZaliheEkran> {
  late final ZaliheOpremeServis _servis;
  late final SifrarnikServis _sifrarnici;

  Strana<ZalihaOpreme> _strana = Strana.prazna();
  List<StavkaSifrarnika> _vrsteOpreme = const [];
  List<StavkaSifrarnika> _poslovnice = const [];

  String _naziv = '';
  int? _poslovnicaId;
  int _stranica = 0;

  static const _velicinaStranice = 15;

  bool _ucitavanje = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    final klijent = context.read<ApiKlijent>();
    _servis = ZaliheOpremeServis(klijent);
    _sifrarnici = SifrarnikServis(klijent);

    // Uposlenik i tako vidi samo svoju poslovnicu, pa je filter odmah na njoj.
    final korisnik = context.read<Sesija>().korisnik;
    if (korisnik != null && korisnik.vezanZaPoslovnicu) {
      _poslovnicaId = korisnik.poslovnicaId;
    }

    _ucitajSifrarnike();
    _ucitaj();
  }

  Future<void> _ucitajSifrarnike() async {
    try {
      final rezultati = await Future.wait([
        _sifrarnici.ucitaj(SifrarnikServis.vrsteOpreme),
        _sifrarnici.ucitaj(SifrarnikServis.poslovnice),
      ]);

      if (!mounted) {
        return;
      }

      setState(() {
        _vrsteOpreme = rezultati[0];
        _poslovnice = dozvoljenePoslovnice(context, rezultati[1]);
      });
    } on ApiGreska {
      // Bez šifarnika filter ostaje prazan, a dugme za unos onemogućeno uz
      // objašnjenje. Lista i bez njih radi.
    }
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      final strana = await _servis.lista(
        naziv: _naziv.isEmpty ? null : _naziv,
        poslovnicaId: _poslovnicaId,
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

  Future<void> _otvoriFormu({ZalihaOpreme? zaliha}) async {
    final sacuvano = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (context) => ZalihaForma(
        zaliha: zaliha,
        vrsteOpreme: _vrsteOpreme,
        poslovnice: _poslovnice,
      ),
    );

    if (sacuvano != true || !mounted) {
      return;
    }

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          zaliha == null
              ? 'Zalihe opreme su uspješno evidentirane.'
              : 'Zalihe za „${zaliha.vrstaOpremeNaziv}" su uspješno izmijenjene.',
        ),
      ),
    );

    // Novi zapis se vidi na prvoj stranici.
    if (zaliha == null) {
      _stranica = 0;
    }

    _ucitaj();
  }

  Future<void> _obrisi(ZalihaOpreme zaliha) async {
    final potvrdjeno = await potvrdiRadnju(
      context,
      naslov: 'Ukloniti zalihe?',
      tekst:
          '„${zaliha.vrstaOpremeNaziv}" u poslovnici ${zaliha.poslovnicaNaziv} '
          'više se neće nuditi uz rezervacije. Ako rezervacije već drže ovu opremu, '
          'uklanjanje se odbija.',
      potvrda: 'Da, ukloni',
      opasna: true,
    );

    if (!potvrdjeno) {
      return;
    }

    try {
      await _servis.obrisi(zaliha.id);
      _ucitaj();
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(greska.poruka)));
    }
  }

  /// Forma se ne otvara bez opreme u katalogu ili bez poslovnice - nema šta birati.
  String? get _zastoNemaUnosa {
    if (_vrsteOpreme.isEmpty) {
      return 'Prvo dodajte vrstu opreme u Šifarnicima.';
    }

    if (_poslovnice.isEmpty) {
      return 'Nema poslovnice u koju bi se oprema unijela.';
    }

    return null;
  }

  @override
  Widget build(BuildContext context) {
    final zastoNemaUnosa = _zastoNemaUnosa;

    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(Razmaci.ekranMargina),
        child: Kartica(
          naslov: 'Zalihe opreme',
          podnaslov:
              'Koliko kaciga, GPS uređaja i ostale opreme ima svaka poslovnica',
          bezUnutrasnjegRazmaka: true,
          rastegni: true,
          akcija: Tooltip(
            message: zastoNemaUnosa ?? '',
            child: ElevatedButton.icon(
              onPressed: zastoNemaUnosa == null ? () => _otvoriFormu() : null,
              icon: const Icon(Icons.add, size: 18),
              label: const Text('Nove zalihe'),
            ),
          ),
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
                      natpis: 'Naziv opreme',
                      naPromjenu: (tekst) {
                        _naziv = tekst;
                        _stranica = 0;
                        _ucitaj();
                      },
                    ),
                    PadajuciSifrarnik(
                      natpis: 'Poslovnica',
                      sirina: 240,
                      stavke: _poslovnice,
                      odabrano: _poslovnicaId,
                      svePoljeNatpis: 'Sve poslovnice',
                      naPromjenu: (id) {
                        setState(() {
                          _poslovnicaId = id;
                          _stranica = 0;
                        });
                        _ucitaj();
                      },
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
                          poruka:
                              'Nema evidentiranih zaliha po ovim filterima.',
                          ikona: Icons.inventory_2_outlined,
                        )
                      : _Tabela(
                          zalihe: _strana.stavke,
                          naIzmjenu: (zaliha) => _otvoriFormu(zaliha: zaliha),
                          naBrisanje: _obrisi,
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

class _Tabela extends StatelessWidget {
  const _Tabela({
    required this.zalihe,
    required this.naIzmjenu,
    required this.naBrisanje,
  });

  final List<ZalihaOpreme> zalihe;
  final ValueChanged<ZalihaOpreme> naIzmjenu;
  final ValueChanged<ZalihaOpreme> naBrisanje;

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
              dataRowMinHeight: 48,
              dataRowMaxHeight: 52,
              columns: const [
                DataColumn(label: Text('OPREMA')),
                DataColumn(label: Text('POSLOVNICA')),
                DataColumn(label: Text('KOLIČINA'), numeric: true),
                DataColumn(label: Text('')),
              ],
              rows: [
                for (final zaliha in zalihe)
                  DataRow(
                    cells: [
                      DataCell(
                        Text(
                          zaliha.vrstaOpremeNaziv,
                          style: const TextStyle(fontWeight: FontWeight.w600),
                        ),
                      ),
                      DataCell(Text(zaliha.poslovnicaNaziv)),
                      DataCell(
                        Text(
                          '${zaliha.kolicina} kom.',
                          style: TextStyle(
                            color: zaliha.kolicina == 0
                                ? Boje.greskaTekst
                                : Boje.tekst,
                          ),
                        ),
                      ),
                      DataCell(
                        Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            IconButton(
                              tooltip: 'Izmijeni količinu',
                              onPressed: () => naIzmjenu(zaliha),
                              icon: const Icon(Icons.edit_outlined, size: 18),
                            ),
                            IconButton(
                              tooltip: 'Ukloni',
                              onPressed: () => naBrisanje(zaliha),
                              icon: const Icon(
                                Icons.delete_outline,
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
