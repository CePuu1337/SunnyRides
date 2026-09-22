import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/primopredaja.dart';
import '../../servisi/primopredaja_servis.dart';
import '../../widgeti/birac_fotografija.dart';
import '../../widgeti/dijalog_forme.dart';
import '../../widgeti/naknadni_unos.dart';
import '../../widgeti/obavjestenje.dart';

/// Izdavanje vozila klijentu.
///
/// Vrijeme izdavanja i ime uposlenika se ne unose - server ih upisuje sam, iz svog
/// sata i iz tokena. Uposlenik unosi samo ono sto je ocitao na vozilu.
class IzdavanjeDijalog extends StatefulWidget {
  const IzdavanjeDijalog({super.key, required this.stavka});

  final RasporedStavka stavka;

  @override
  State<IzdavanjeDijalog> createState() => _IzdavanjeDijalogStanje();
}

class _IzdavanjeDijalogStanje extends State<IzdavanjeDijalog> {
  final _forma = GlobalKey<FormState>();
  final _kilometraza = TextEditingController();
  final _napomena = TextEditingController();

  late final PrimopredajaServis _servis;

  int _gorivo = 100;
  bool _kontrolnaLista = false;
  DateTime? _stvarnoVrijeme;
  List<FajlZaSlanje> _fotografije = const [];

  bool _snimanje = false;
  String? _greska;

  @override
  void initState() {
    super.initState();
    _servis = PrimopredajaServis(context.read<ApiKlijent>());
  }

  @override
  void dispose() {
    _kilometraza.dispose();
    _napomena.dispose();
    super.dispose();
  }

  Future<void> _sacuvaj() async {
    if (!_forma.currentState!.validate()) {
      return;
    }

    if (!_kontrolnaLista) {
      setState(() => _greska = 'Potvrdite da je kontrolna lista prođena.');

      return;
    }

    setState(() {
      _snimanje = true;
      _greska = null;
    });

    try {
      await _servis.izdaj(
        rezervacijaId: widget.stavka.rezervacijaId,
        kilometraza: int.parse(_kilometraza.text.trim()),
        nivoGoriva: _gorivo,
        kontrolnaListaProdjena: _kontrolnaLista,
        napomena: _napomena.text.trim().isEmpty ? null : _napomena.text.trim(),
        datumIzdavanja: _stvarnoVrijeme,
        fotografije: _fotografije,
      );

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

  /// Vrijeme koje se nudi kad uposlenik kaze da je preuzimanje bilo ranije.
  ///
  /// Ugovoreno preuzimanje ako je proslo, inace sada - to je u praksi najbliza
  /// pretpostavka onome sto se desilo.
  DateTime _pocetnoVrijeme() {
    final ugovoreno = widget.stavka.datumOd.toLocal();
    final sada = DateTime.now();

    return ugovoreno.isBefore(sada) ? ugovoreno : sada;
  }

  @override
  Widget build(BuildContext context) {
    final stavka = widget.stavka;

    return DijalogForme(
      naslov: 'Izdavanje vozila',
      podnaslov:
          '${stavka.broj} · ${stavka.voziloNaziv ?? ''} '
          '${stavka.registarskaOznaka ?? ''} · ${stavka.klijentImePrezime ?? ''}',
      greska: _greska,
      uToku: _snimanje,
      natpisPotvrde: 'Evidentiraj izdavanje',
      naSnimanje: _sacuvaj,
      sirina: 640,
      dijete: Form(
        key: _forma,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Obavjestenje.info(
              'Ugovoreni termin: ${Formati.datumIVrijeme(stavka.datumOd)} – '
              '${Formati.datumIVrijeme(stavka.datumDo)}.',
            ),
            const SizedBox(height: Razmaci.l),
            NaknadniUnos(
              natpis: 'Vozilo je preuzeto ranije nego što ovo upisujem',
              pojasnjenje:
                  'Bez ovoga se kao vrijeme izdavanja upisuje trenutak potvrde. '
                  'Mora biti unutar ugovorenog termina.',
              vrijeme: _stvarnoVrijeme,
              podrazumijevano: _pocetnoVrijeme(),
              najranije: stavka.datumOd.toLocal().subtract(
                const Duration(hours: 2),
              ),
              najkasnije: DateTime.now(),
              naPromjenu: (novo) => setState(() => _stvarnoVrijeme = novo),
            ),
            const SizedBox(height: Razmaci.l),
            TextFormField(
              controller: _kilometraza,
              keyboardType: TextInputType.number,
              inputFormatters: [FilteringTextInputFormatter.digitsOnly],
              autofocus: true,
              decoration: const InputDecoration(
                labelText: 'Kilometraža pri izdavanju',
                suffixText: 'km',
              ),
              validator: (vrijednost) {
                final km = int.tryParse(vrijednost?.trim() ?? '');

                if (km == null || km < 0 || km > 2000000) {
                  return 'Unesite kilometražu između 0 i 2.000.000.';
                }

                return null;
              },
            ),
            const SizedBox(height: Razmaci.l),
            KlizacGoriva(
              vrijednost: _gorivo,
              jeElektricno: stavka.jeElektricno,
              naPromjenu: (novo) => setState(() => _gorivo = novo),
            ),
            const SizedBox(height: Razmaci.s),
            CheckboxListTile(
              value: _kontrolnaLista,
              onChanged: (novo) => setState(() {
                _kontrolnaLista = novo ?? false;
                _greska = null;
              }),
              contentPadding: EdgeInsets.zero,
              controlAffinity: ListTileControlAffinity.leading,
              title: const Text('Kontrolna lista stanja vozila je prođena'),
              subtitle: const Text(
                'Gume, kočnice, svjetla, ogledala i vidljiva oštećenja.',
                style: TextStyle(fontSize: 12),
              ),
            ),
            const SizedBox(height: Razmaci.l),
            TextFormField(
              controller: _napomena,
              maxLines: 3,
              maxLength: 1000,
              decoration: const InputDecoration(
                labelText: 'Napomena (nije obavezna)',
                alignLabelWithHint: true,
              ),
            ),
            const SizedBox(height: Razmaci.s),
            const Text(
              'Fotografije stanja pri izdavanju',
              style: TextStyle(fontSize: 13.5, fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: Razmaci.xs),
            const Text(
              'Nisu obavezne, ali su jedini dokaz u kakvom je stanju vozilo otišlo.',
              style: TextStyle(color: Boje.tekstPrigusen, fontSize: 12),
            ),
            const SizedBox(height: Razmaci.m),
            BiracFotografija(
              fotografije: _fotografije,
              naPromjenu: (nove) => setState(() => _fotografije = nove),
            ),
          ],
        ),
      ),
    );
  }
}
