import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/recenzija.dart';
import '../../servisi/moderacija_servis.dart';
import '../../widgeti/kartica.dart';
import '../../widgeti/paginator.dart';
import '../../widgeti/polja.dart';
import '../../widgeti/sadrzaj.dart';

/// Moderacija recenzija.
///
/// Recenzija se ne brise nego skriva: zapis ostaje, a prestaje ulaziti u prosjecnu
/// ocjenu i u preporuke. Brisanje bi uklonilo i trag da je uopste postojala, pa se
/// ne bi moglo provjeriti sta je i zasto uklonjeno.
class RecenzijeEkran extends StatefulWidget {
  const RecenzijeEkran({super.key});

  @override
  State<RecenzijeEkran> createState() => _RecenzijeEkranStanje();
}

class _RecenzijeEkranStanje extends State<RecenzijeEkran> {
  late final RecenzijaServis _servis;

  UpitRecenzija _upit = const UpitRecenzija();
  Strana<Recenzija> _strana = Strana.prazna();

  bool _ucitavanje = true;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = RecenzijaServis(context.read<ApiKlijent>());
    _ucitaj();
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

  void _promijeniUpit(UpitRecenzija noviUpit) {
    setState(() => _upit = noviUpit);
    _ucitaj();
  }

  Future<void> _promijeniVidljivost(Recenzija recenzija) async {
    try {
      if (recenzija.skrivena) {
        await _servis.prikazi(recenzija.id);
      } else {
        await _servis.sakrij(recenzija.id);
      }

      await _ucitaj();

      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            recenzija.skrivena
                ? 'Recenzija je ponovo vidljiva i ulazi u prosječnu ocjenu.'
                : 'Recenzija je skrivena i više ne ulazi u prosječnu ocjenu.',
          ),
        ),
      );
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
          naslov: 'Recenzije',
          podnaslov: 'Ocjene klijenata nakon završenog najma',
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
                          poruka: 'Nema recenzija po ovim filterima.',
                          ikona: Icons.star_outline,
                        )
                      : ListView.separated(
                          itemCount: _strana.stavke.length,
                          separatorBuilder: (context, indeks) =>
                              const Divider(height: 1),
                          itemBuilder: (context, indeks) => _Red(
                            recenzija: _strana.stavke[indeks],
                            naPromjenuVidljivosti: () =>
                                _promijeniVidljivost(_strana.stavke[indeks]),
                          ),
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
  const _Filteri({required this.upit, required this.naPromjenu});

  final UpitRecenzija upit;
  final ValueChanged<UpitRecenzija> naPromjenu;

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
            natpis: 'Klijent',
            sirina: 210,
            naPromjenu: (tekst) =>
                naPromjenu(upit.kopija(klijent: tekst, stranica: 0)),
          ),
          PoljePretrage(
            natpis: 'Tekst komentara',
            sirina: 230,
            naPromjenu: (tekst) =>
                naPromjenu(upit.kopija(komentar: tekst, stranica: 0)),
          ),
          SizedBox(
            width: 190,
            child: DropdownButtonFormField<int?>(
              key: ValueKey(upit.ocjenaDo),
              initialValue: upit.ocjenaDo,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'Ocjena'),
              items: const [
                DropdownMenuItem<int?>(value: null, child: Text('Sve ocjene')),
                DropdownMenuItem<int?>(value: 2, child: Text('2 i manje')),
                DropdownMenuItem<int?>(value: 3, child: Text('3 i manje')),
              ],
              onChanged: (vrijednost) => naPromjenu(
                vrijednost == null
                    ? upit.kopija(ocistiOcjenu: true, stranica: 0)
                    : upit.kopija(ocjenaDo: vrijednost, stranica: 0),
              ),
            ),
          ),
          SizedBox(
            width: 190,
            child: DropdownButtonFormField<bool?>(
              key: ValueKey(upit.skrivena),
              initialValue: upit.skrivena,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'Vidljivost'),
              items: const [
                DropdownMenuItem<bool?>(value: null, child: Text('Sve')),
                DropdownMenuItem<bool?>(value: false, child: Text('Vidljive')),
                DropdownMenuItem<bool?>(value: true, child: Text('Skrivene')),
              ],
              onChanged: (vrijednost) => naPromjenu(
                vrijednost == null
                    ? upit.kopija(ocistiSkrivene: true, stranica: 0)
                    : upit.kopija(skrivena: vrijednost, stranica: 0),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _Red extends StatelessWidget {
  const _Red({required this.recenzija, required this.naPromjenuVidljivosti});

  final Recenzija recenzija;
  final VoidCallback naPromjenuVidljivosti;

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
          _Zvjezdice(ocjena: recenzija.ocjena),
          const SizedBox(width: Razmaci.l),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Text(
                      recenzija.korisnikImePrezime ?? '',
                      style: const TextStyle(
                        fontSize: 13.5,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(width: Razmaci.s),
                    Text(
                      Formati.datum(recenzija.datumKreiranja),
                      style: const TextStyle(
                        color: Boje.tekstPrigusen,
                        fontSize: 11.5,
                      ),
                    ),
                    if (recenzija.skrivena) ...[
                      const SizedBox(width: Razmaci.s),
                      const StatusnaPilula(
                        tekst: 'Skrivena',
                        pozadina: Boje.neutralnoPozadina,
                        bojaTeksta: Boje.neutralnoTekst,
                        ikona: Icons.visibility_off_outlined,
                      ),
                    ],
                  ],
                ),
                const SizedBox(height: 2),
                Text(
                  '${recenzija.voziloOpis ?? ''} · ${recenzija.registarskaOznaka ?? ''}'
                  ' · ${recenzija.rezervacijaBroj ?? ''}',
                  style: const TextStyle(
                    color: Boje.tekstPrigusen,
                    fontSize: 11.5,
                  ),
                ),
                if (recenzija.komentar != null &&
                    recenzija.komentar!.isNotEmpty) ...[
                  const SizedBox(height: Razmaci.s),
                  Text(
                    recenzija.komentar!,
                    style: const TextStyle(fontSize: 13, height: 1.4),
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(width: Razmaci.l),
          TextButton.icon(
            onPressed: naPromjenuVidljivosti,
            icon: Icon(
              recenzija.skrivena
                  ? Icons.visibility_outlined
                  : Icons.visibility_off_outlined,
              size: 17,
            ),
            label: Text(recenzija.skrivena ? 'Prikaži' : 'Sakrij'),
          ),
        ],
      ),
    );
  }
}

class _Zvjezdice extends StatelessWidget {
  const _Zvjezdice({required this.ocjena});

  final int ocjena;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 92,
      child: Row(
        children: [
          for (var i = 1; i <= 5; i++)
            Icon(
              i <= ocjena ? Icons.star_rounded : Icons.star_outline_rounded,
              size: 17,
              color: i <= ocjena ? Boje.primarnaTamnija : Boje.ivicaJaca,
            ),
        ],
      ),
    );
  }
}
