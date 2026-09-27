import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/vozilo.dart';

/// Nacin poredanja rezultata pretrage.
///
/// Vrijednost je ono sto ide serveru kao orderBy. "Preporuka" i "Ocjena" nisu kolone
/// nego racun - prva kroz sistem preporuke, druga nad recenzijama - pa ih server
/// obradjuje posebno.
enum Poredak {
  preporuka('Preporučeno za vas', 'Preporuka'),
  cijenaRastuce('Cijena: niža prvo', 'DnevnaTarifa asc'),
  cijenaOpadajuce('Cijena: viša prvo', 'DnevnaTarifa desc'),
  ocjena('Najbolje ocijenjeno', 'ProsjecnaOcjena desc'),
  godiste('Novija vozila', 'GodinaProizvodnje desc');

  const Poredak(this.naziv, this.vrijednost);

  final String naziv;
  final String vrijednost;
}

/// Sve po cemu se ponuda suzava. Termin ide zajedno - jedan datum ne opisuje period.
class Filteri {
  const Filteri({
    this.tekst,
    this.tipVozilaId,
    this.markaId,
    this.gradId,
    this.poslovnicaId,
    this.cijenaOd,
    this.cijenaDo,
    this.datumOd,
    this.datumDo,
    this.poredak = Poredak.preporuka,
  });

  final String? tekst;
  final int? tipVozilaId;
  final int? markaId;
  final int? gradId;
  final int? poslovnicaId;
  final double? cijenaOd;
  final double? cijenaDo;
  final DateTime? datumOd;
  final DateTime? datumDo;
  final Poredak poredak;

  bool get imaTermin =>
      datumOd != null && datumDo != null && datumDo!.isAfter(datumOd!);

  /// Cjenovni raspon se broji kao jedan filter, i kad je zadana samo jedna granica.
  int get brojAktivnih => [
    tipVozilaId,
    markaId,
    gradId,
    poslovnicaId,
    cijenaOd ?? cijenaDo,
    imaTermin ? datumOd : null,
  ].where((x) => x != null).length;

  Filteri kopija({
    Object? tekst = _nepromijenjeno,
    Object? tipVozilaId = _nepromijenjeno,
    Object? markaId = _nepromijenjeno,
    Object? gradId = _nepromijenjeno,
    Object? poslovnicaId = _nepromijenjeno,
    Object? cijenaOd = _nepromijenjeno,
    Object? cijenaDo = _nepromijenjeno,
    Object? datumOd = _nepromijenjeno,
    Object? datumDo = _nepromijenjeno,
    Poredak? poredak,
  }) {
    return Filteri(
      tekst: tekst == _nepromijenjeno ? this.tekst : tekst as String?,
      tipVozilaId: tipVozilaId == _nepromijenjeno
          ? this.tipVozilaId
          : tipVozilaId as int?,
      markaId: markaId == _nepromijenjeno ? this.markaId : markaId as int?,
      gradId: gradId == _nepromijenjeno ? this.gradId : gradId as int?,
      poslovnicaId: poslovnicaId == _nepromijenjeno
          ? this.poslovnicaId
          : poslovnicaId as int?,
      cijenaOd: cijenaOd == _nepromijenjeno
          ? this.cijenaOd
          : cijenaOd as double?,
      cijenaDo: cijenaDo == _nepromijenjeno
          ? this.cijenaDo
          : cijenaDo as double?,
      datumOd: datumOd == _nepromijenjeno ? this.datumOd : datumOd as DateTime?,
      datumDo: datumDo == _nepromijenjeno ? this.datumDo : datumDo as DateTime?,
      poredak: poredak ?? this.poredak,
    );
  }

  /// Razlikuje "nije proslijedjeno" od "proslijedjeno kao prazno" - bez toga se
  /// filter ne bi mogao ukloniti kroz kopiju.
  static const _nepromijenjeno = Object();
}

/// Donji list sa filterima. Vraca nove filtere, ili nista ako korisnik odustane.
class ListFiltera extends StatefulWidget {
  const ListFiltera({
    super.key,
    required this.pocetni,
    required this.tipovi,
    required this.marke,
    required this.gradovi,
    required this.poslovnice,
  });

  final Filteri pocetni;
  final List<Stavka> tipovi;
  final List<Stavka> marke;
  final List<Stavka> gradovi;
  final List<Stavka> poslovnice;

  @override
  State<ListFiltera> createState() => _ListFilteraStanje();
}

class _ListFilteraStanje extends State<ListFiltera> {
  final _forma = GlobalKey<FormState>();
  late Filteri _filteri = widget.pocetni;
  late final _cijenaOd = TextEditingController(
    text: _uTekst(widget.pocetni.cijenaOd),
  );
  late final _cijenaDo = TextEditingController(
    text: _uTekst(widget.pocetni.cijenaDo),
  );

  @override
  void dispose() {
    _cijenaOd.dispose();
    _cijenaDo.dispose();
    super.dispose();
  }

  static String _uTekst(double? cijena) =>
      cijena == null ? '' : cijena.toStringAsFixed(0);

  /// Poslovnice odabranog grada, ili sve kad grad nije odabran.
  List<Stavka> get _ponudjenePoslovnice => _filteri.gradId == null
      ? widget.poslovnice
      : widget.poslovnice.where((x) => x.gradId == _filteri.gradId).toList();

  void _odaberiGrad(int? gradId) {
    setState(() {
      _filteri = _filteri.kopija(gradId: gradId);

      // Poslovnica iz drugog grada vise ne pripada odabiru - ostala bi skriven
      // uslov koji vraca praznu listu.
      final poslovnica = _filteri.poslovnicaId;

      if (poslovnica != null &&
          !_ponudjenePoslovnice.any((x) => x.id == poslovnica)) {
        _filteri = _filteri.kopija(poslovnicaId: null);
      }
    });
  }

  Future<void> _odaberiTermin() async {
    final sada = DateTime.now();

    final raspon = await showDateRangePicker(
      context: context,
      firstDate: DateTime(sada.year, sada.month, sada.day),
      lastDate: DateTime(sada.year + 1, 12, 31),
      initialDateRange: _filteri.imaTermin
          ? DateTimeRange(start: _filteri.datumOd!, end: _filteri.datumDo!)
          : null,
      helpText: 'Termin najma',
      saveText: 'Potvrdi',
    );

    if (raspon == null || !mounted) {
      return;
    }

    // Preuzimanje u 9, povrat u 18 - prosjecan radni dan poslovnice. Tacan sat se
    // bira u rezervaciji, ovdje sluzi samo da pretraga ima period.
    setState(() {
      _filteri = _filteri.kopija(
        datumOd: DateTime(
          raspon.start.year,
          raspon.start.month,
          raspon.start.day,
          9,
        ),
        datumDo: DateTime(
          raspon.end.year,
          raspon.end.month,
          raspon.end.day,
          18,
        ),
      );
    });
  }

  void _primijeni() {
    if (!(_forma.currentState?.validate() ?? false)) {
      return;
    }

    Navigator.of(context).pop(
      _filteri.kopija(
        cijenaOd: ValidacijaCijene.procitaj(_cijenaOd.text),
        cijenaDo: ValidacijaCijene.procitaj(_cijenaDo.text),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(
        left: Razmaci.l,
        right: Razmaci.l,
        top: Razmaci.l,
        bottom: MediaQuery.of(context).viewInsets.bottom + Razmaci.l,
      ),
      child: Form(
        key: _forma,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Expanded(
                    child: Text(
                      'Filteri',
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                  TextButton(
                    onPressed: () => Navigator.of(context).pop(
                      Filteri(tekst: _filteri.tekst, poredak: _filteri.poredak),
                    ),
                    child: const Text('Poništi'),
                  ),
                ],
              ),
              const SizedBox(height: Razmaci.m),
              DropdownButtonFormField<int?>(
                initialValue: _filteri.tipVozilaId,
                decoration: const InputDecoration(labelText: 'Tip vozila'),
                items: [
                  const DropdownMenuItem<int?>(
                    value: null,
                    child: Text('Svi tipovi'),
                  ),
                  for (final tip in widget.tipovi)
                    DropdownMenuItem<int?>(
                      value: tip.id,
                      child: Text(tip.naziv),
                    ),
                ],
                onChanged: (vrijednost) => setState(
                  () => _filteri = _filteri.kopija(tipVozilaId: vrijednost),
                ),
              ),
              const SizedBox(height: Razmaci.m),
              DropdownButtonFormField<int?>(
                initialValue: _filteri.markaId,
                decoration: const InputDecoration(labelText: 'Marka'),
                items: [
                  const DropdownMenuItem<int?>(
                    value: null,
                    child: Text('Sve marke'),
                  ),
                  for (final marka in widget.marke)
                    DropdownMenuItem<int?>(
                      value: marka.id,
                      child: Text(marka.naziv),
                    ),
                ],
                onChanged: (vrijednost) => setState(
                  () => _filteri = _filteri.kopija(markaId: vrijednost),
                ),
              ),
              const SizedBox(height: Razmaci.m),
              DropdownButtonFormField<int?>(
                initialValue: _filteri.gradId,
                decoration: const InputDecoration(labelText: 'Grad'),
                items: [
                  const DropdownMenuItem<int?>(
                    value: null,
                    child: Text('Svi gradovi'),
                  ),
                  for (final grad in widget.gradovi)
                    DropdownMenuItem<int?>(
                      value: grad.id,
                      child: Text(grad.naziv),
                    ),
                ],
                onChanged: _odaberiGrad,
              ),
              const SizedBox(height: Razmaci.m),
              DropdownButtonFormField<int?>(
                // Kljuc prati grad: kad se grad promijeni, lista poslovnica je druga i
                // polje se gradi iznova, sa odabirom koji odgovara novoj listi.
                key: ValueKey('poslovnica-${_filteri.gradId}'),
                initialValue: _filteri.poslovnicaId,
                decoration: const InputDecoration(labelText: 'Poslovnica'),
                items: [
                  const DropdownMenuItem<int?>(
                    value: null,
                    child: Text('Sve poslovnice'),
                  ),
                  for (final poslovnica in _ponudjenePoslovnice)
                    DropdownMenuItem<int?>(
                      value: poslovnica.id,
                      child: Text(poslovnica.naziv),
                    ),
                ],
                onChanged: (vrijednost) => setState(
                  () => _filteri = _filteri.kopija(poslovnicaId: vrijednost),
                ),
              ),
              const SizedBox(height: Razmaci.m),
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: TextFormField(
                      controller: _cijenaOd,
                      keyboardType: const TextInputType.numberWithOptions(
                        decimal: true,
                      ),
                      decoration: const InputDecoration(
                        labelText: 'Cijena po danu od',
                        suffixText: '€',
                      ),
                      validator: ValidacijaCijene.iznos,
                    ),
                  ),
                  const SizedBox(width: Razmaci.m),
                  Expanded(
                    child: TextFormField(
                      controller: _cijenaDo,
                      keyboardType: const TextInputType.numberWithOptions(
                        decimal: true,
                      ),
                      decoration: const InputDecoration(
                        labelText: 'do',
                        suffixText: '€',
                      ),
                      validator: (vrijednost) =>
                          ValidacijaCijene.iznos(vrijednost) ??
                          ValidacijaCijene.raspon(_cijenaOd.text, vrijednost),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: Razmaci.m),
              OutlinedButton.icon(
                onPressed: _odaberiTermin,
                icon: const Icon(Icons.date_range_outlined, size: 18),
                label: Text(
                  _filteri.imaTermin
                      ? '${Formati.datum(_filteri.datumOd!)} - '
                            '${Formati.datum(_filteri.datumDo!)}'
                      : 'Odaberi termin',
                ),
              ),
              if (_filteri.imaTermin)
                Align(
                  alignment: Alignment.centerLeft,
                  child: TextButton(
                    onPressed: () => setState(
                      () => _filteri = _filteri.kopija(
                        datumOd: null,
                        datumDo: null,
                      ),
                    ),
                    child: const Text('Ukloni termin'),
                  ),
                ),
              const SizedBox(height: Razmaci.l),
              SizedBox(
                width: double.infinity,
                child: FilledButton(
                  onPressed: _primijeni,
                  child: const Text('Prikaži rezultate'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Provjera cjenovnog raspona u filterima. Pogresan unos se prikaze ispod polja,
/// umjesto da se tiho zanemari.
class ValidacijaCijene {
  const ValidacijaCijene._();

  /// Prazno polje znaci "bez granice". Zarez i tacka se prihvataju kao decimalni znak.
  static double? procitaj(String? tekst) {
    final ociscen = (tekst ?? '').trim().replaceAll(',', '.');

    return ociscen.isEmpty ? null : double.tryParse(ociscen);
  }

  static String? iznos(String? tekst) {
    if ((tekst ?? '').trim().isEmpty) {
      return null;
    }

    final cijena = procitaj(tekst);

    if (cijena == null || cijena < 0) {
      return 'Unesite iznos u eurima, npr. 40.';
    }

    return null;
  }

  static String? raspon(String? od, String? doIznosa) {
    final donja = procitaj(od);
    final gornja = procitaj(doIznosa);

    if (donja != null && gornja != null && gornja < donja) {
      return 'Gornja granica mora biti veća od donje.';
    }

    return null;
  }
}
