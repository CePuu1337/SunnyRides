import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/definicija_sifrarnika.dart';
import '../../modeli/stavka_sifrarnika.dart';
import '../../servisi/sifrarnik_servis.dart';
import '../../widgeti/dijalog_forme.dart';

/// Forma bilo kojeg sifrarnika, sastavljena iz njegove definicije.
///
/// Umjesto dvanaest gotovo istih dijaloga, jedan koji zna citati opis polja. Kad se
/// doda novi sifrarnik, dopise se nekoliko redova u definiciji - forma i provjere
/// nastanu same.
class SifrarnikForma extends StatefulWidget {
  const SifrarnikForma({super.key, required this.definicija, this.zapis});

  final DefinicijaSifrarnika definicija;

  /// Postojeci zapis pri izmjeni, prazno pri unosu.
  final Map<String, dynamic>? zapis;

  @override
  State<SifrarnikForma> createState() => _SifrarnikFormaStanje();
}

class _SifrarnikFormaStanje extends State<SifrarnikForma> {
  final _forma = GlobalKey<FormState>();
  final _kontroleri = <String, TextEditingController>{};
  final _zastavice = <String, bool>{};
  final _veze = <String, int?>{};
  final _ucitaneVeze = <String, List<StavkaSifrarnika>>{};

  late final SifrarnikCrudServis _servis;
  late final SifrarnikServis _sifrarnici;

  bool _ucitavanje = true;
  bool _snimanje = false;
  String? _greska;

  bool get _jeIzmjena => widget.zapis != null;

  @override
  void initState() {
    super.initState();

    final klijent = context.read<ApiKlijent>();
    _servis = SifrarnikCrudServis(klijent);
    _sifrarnici = SifrarnikServis(klijent);

    for (final polje in widget.definicija.polja) {
      final vrijednost = widget.zapis?[polje.kljuc];

      switch (polje.vrsta) {
        case VrstaPolja.prekidac:
          _zastavice[polje.kljuc] = citajBool(vrijednost);
        case VrstaPolja.veza:
          _veze[polje.kljuc] = vrijednost == null ? null : citajInt(vrijednost);
        default:
          _kontroleri[polje.kljuc] = TextEditingController(
            text: vrijednost?.toString() ?? '',
          );
      }
    }

    _ucitajVeze();
  }

  @override
  void dispose() {
    for (final kontroler in _kontroleri.values) {
      kontroler.dispose();
    }

    super.dispose();
  }

  Future<void> _ucitajVeze() async {
    final putanje = widget.definicija.polja
        .where((x) => x.vrsta == VrstaPolja.veza && x.veza != null)
        .map((x) => x.veza!)
        .toSet();

    try {
      for (final putanja in putanje) {
        _ucitaneVeze[putanja] = await _sifrarnici.ucitaj(putanja);
      }

      if (mounted) {
        setState(() => _ucitavanje = false);
      }
    } on ApiGreska catch (greska) {
      if (mounted) {
        setState(() {
          _ucitavanje = false;
          _greska = greska.poruka;
        });
      }
    }
  }

  Map<String, dynamic> _sastaviZahtjev() {
    final zahtjev = <String, dynamic>{};

    for (final polje in widget.definicija.polja) {
      switch (polje.vrsta) {
        case VrstaPolja.prekidac:
          zahtjev[polje.kljuc] = _zastavice[polje.kljuc] ?? false;
        case VrstaPolja.veza:
          zahtjev[polje.kljuc] = _veze[polje.kljuc];
        case VrstaPolja.cijelBroj:
          final tekst = _kontroleri[polje.kljuc]!.text.trim();
          zahtjev[polje.kljuc] = tekst.isEmpty ? null : int.tryParse(tekst);
        case VrstaPolja.decimalniBroj:
          final tekst = _kontroleri[polje.kljuc]!.text.trim().replaceAll(
            ',',
            '.',
          );
          zahtjev[polje.kljuc] = tekst.isEmpty ? null : double.tryParse(tekst);
        case VrstaPolja.tekst:
          final tekst = _kontroleri[polje.kljuc]!.text.trim();
          zahtjev[polje.kljuc] = tekst.isEmpty && !polje.obavezno
              ? null
              : tekst;
      }
    }

    return zahtjev;
  }

  Future<void> _sacuvaj() async {
    if (!_forma.currentState!.validate()) {
      return;
    }

    setState(() {
      _snimanje = true;
      _greska = null;
    });

    try {
      final zahtjev = _sastaviZahtjev();

      if (_jeIzmjena) {
        await _servis.izmijeni(
          widget.definicija.putanja,
          citajInt(widget.zapis!['id']),
          zahtjev,
        );
      } else {
        await _servis.dodaj(widget.definicija.putanja, zahtjev);
      }

      // Sifrarnik je promijenjen, pa zapamcene liste vise ne vrijede - inace bi
      // padajuce liste na drugim ekranima jos pokazivale staro stanje.
      _sifrarnici.zaboravi(widget.definicija.putanja);

      if (!mounted) {
        return;
      }

      Navigator.of(context).pop(true);
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() {
        _snimanje = false;
        _greska = greska.poruka;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return DijalogForme(
      naslov: _jeIzmjena
          ? 'Izmjena: ${widget.definicija.nazivJednine}'
          : 'Novi unos: ${widget.definicija.nazivJednine}',
      podnaslov: widget.definicija.opis,
      greska: _greska,
      uToku: _snimanje,
      sirina: 620,
      naSnimanje: _sacuvaj,
      dijete: _ucitavanje
          ? const Padding(
              padding: EdgeInsets.all(Razmaci.xxl),
              child: Center(child: CircularProgressIndicator()),
            )
          : Form(
              key: _forma,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: _redovi(),
              ),
            ),
    );
  }

  /// Polja oznacena kao polovicna se slazu u parovima, ostala idu punom sirinom.
  List<Widget> _redovi() {
    final redovi = <Widget>[];
    final polja = widget.definicija.polja;

    for (var i = 0; i < polja.length; i++) {
      final polje = polja[i];
      final sljedece = i + 1 < polja.length ? polja[i + 1] : null;

      if (redovi.isNotEmpty) {
        redovi.add(const SizedBox(height: Razmaci.l));
      }

      if (polje.uPolaReda && sljedece != null && sljedece.uPolaReda) {
        redovi.add(RedPolja(lijevo: _polje(polje), desno: _polje(sljedece)));
        i++;
      } else {
        redovi.add(_polje(polje));
      }
    }

    return redovi;
  }

  Widget _polje(PoljeSifrarnika polje) {
    switch (polje.vrsta) {
      case VrstaPolja.prekidac:
        return SwitchListTile(
          value: _zastavice[polje.kljuc] ?? false,
          onChanged: (vrijednost) =>
              setState(() => _zastavice[polje.kljuc] = vrijednost),
          contentPadding: EdgeInsets.zero,
          title: Text(polje.natpis),
          subtitle: polje.pojasnjenje == null
              ? null
              : Text(
                  polje.pojasnjenje!,
                  style: const TextStyle(fontSize: 11.5),
                ),
        );

      case VrstaPolja.veza:
        final stavke = _ucitaneVeze[polje.veza] ?? const <StavkaSifrarnika>[];

        return DropdownButtonFormField<int>(
          initialValue: stavke.any((x) => x.id == _veze[polje.kljuc])
              ? _veze[polje.kljuc]
              : null,
          isExpanded: true,
          decoration: InputDecoration(
            labelText: polje.natpis,
            helperText: polje.pojasnjenje,
            helperMaxLines: 3,
          ),
          items: [
            for (final stavka in stavke)
              DropdownMenuItem(value: stavka.id, child: Text(stavka.naziv)),
          ],
          onChanged: (vrijednost) =>
              setState(() => _veze[polje.kljuc] = vrijednost),
          validator: (vrijednost) => polje.obavezno && vrijednost == null
              ? 'Odaberite ${polje.natpis.toLowerCase()}.'
              : null,
        );

      default:
        final jeBroj =
            polje.vrsta == VrstaPolja.cijelBroj ||
            polje.vrsta == VrstaPolja.decimalniBroj;

        return TextFormField(
          controller: _kontroleri[polje.kljuc],
          keyboardType: jeBroj
              ? TextInputType.numberWithOptions(
                  decimal: polje.vrsta == VrstaPolja.decimalniBroj,
                )
              : null,
          inputFormatters: polje.vrsta == VrstaPolja.cijelBroj
              ? [FilteringTextInputFormatter.digitsOnly]
              : null,
          decoration: InputDecoration(
            labelText: polje.natpis,
            suffixText: polje.sufiks,
            helperText: polje.pojasnjenje,
            helperMaxLines: 3,
          ),
          validator: (vrijednost) => _provjeri(polje, vrijednost),
        );
    }
  }

  String? _provjeri(PoljeSifrarnika polje, String? vrijednost) {
    final tekst = (vrijednost ?? '').trim();

    if (tekst.isEmpty) {
      return polje.obavezno
          ? 'Polje ${polje.natpis.toLowerCase()} je obavezno.'
          : null;
    }

    if (polje.vrsta == VrstaPolja.tekst) {
      if (polje.najmanje != null && tekst.length < polje.najmanje!) {
        return 'Najmanje ${polje.najmanje} znakova.';
      }

      if (polje.najvise != null && tekst.length > polje.najvise!) {
        return 'Najviše ${polje.najvise} znakova.';
      }

      return null;
    }

    final broj = double.tryParse(tekst.replaceAll(',', '.'));

    if (broj == null) {
      return 'Unesite broj.';
    }

    if (polje.najmanjaVrijednost != null && broj < polje.najmanjaVrijednost!) {
      return 'Najmanje ${polje.najmanjaVrijednost}.';
    }

    if (polje.najvecaVrijednost != null && broj > polje.najvecaVrijednost!) {
      return 'Najviše ${polje.najvecaVrijednost}.';
    }

    return null;
  }
}
