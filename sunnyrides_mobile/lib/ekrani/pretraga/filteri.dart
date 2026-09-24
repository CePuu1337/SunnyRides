import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/vozilo.dart';

/// Nacin poredanja rezultata pretrage.
///
/// Vrijednost je ono sto ide serveru kao orderBy. "Ocjena" nije kolona nego racun
/// nad recenzijama, pa je server posebno obradjuje.
enum Poredak {
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
    this.gradId,
    this.cijenaDo,
    this.datumOd,
    this.datumDo,
    this.poredak = Poredak.cijenaRastuce,
  });

  final String? tekst;
  final int? tipVozilaId;
  final int? gradId;
  final double? cijenaDo;
  final DateTime? datumOd;
  final DateTime? datumDo;
  final Poredak poredak;

  bool get imaTermin =>
      datumOd != null && datumDo != null && datumDo!.isAfter(datumOd!);

  int get brojAktivnih => [
    tipVozilaId,
    gradId,
    cijenaDo,
    imaTermin ? datumOd : null,
  ].where((x) => x != null).length;

  Filteri kopija({
    Object? tekst = _nepromijenjeno,
    Object? tipVozilaId = _nepromijenjeno,
    Object? gradId = _nepromijenjeno,
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
      gradId: gradId == _nepromijenjeno ? this.gradId : gradId as int?,
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
    required this.gradovi,
  });

  final Filteri pocetni;
  final List<Stavka> tipovi;
  final List<Stavka> gradovi;

  @override
  State<ListFiltera> createState() => _ListFilteraStanje();
}

class _ListFilteraStanje extends State<ListFiltera> {
  late Filteri _filteri = widget.pocetni;
  late final TextEditingController _cijena = TextEditingController(
    text: widget.pocetni.cijenaDo == null
        ? ''
        : widget.pocetni.cijenaDo!.toStringAsFixed(0),
  );

  @override
  void dispose() {
    _cijena.dispose();
    super.dispose();
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
    final unesena = double.tryParse(_cijena.text.replaceAll(',', '.'));

    Navigator.of(context).pop(_filteri.kopija(cijenaDo: unesena));
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
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Expanded(
                child: Text(
                  'Filteri',
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
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
                DropdownMenuItem<int?>(value: tip.id, child: Text(tip.naziv)),
            ],
            onChanged: (vrijednost) => setState(
              () => _filteri = _filteri.kopija(tipVozilaId: vrijednost),
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
                DropdownMenuItem<int?>(value: grad.id, child: Text(grad.naziv)),
            ],
            onChanged: (vrijednost) =>
                setState(() => _filteri = _filteri.kopija(gradId: vrijednost)),
          ),
          const SizedBox(height: Razmaci.m),
          TextField(
            controller: _cijena,
            keyboardType: TextInputType.number,
            decoration: const InputDecoration(
              labelText: 'Cijena po danu do',
              suffixText: '€',
            ),
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
                  () =>
                      _filteri = _filteri.kopija(datumOd: null, datumDo: null),
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
    );
  }
}
