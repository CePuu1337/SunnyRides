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

/// Povrat vozila, sa obracunom depozita i podacima sa izdavanja.
///
/// Iznos povrata se ne unosi - racuna ga server iz uplate, kasnjenja i stete.
/// Obracun se osvjezava dok uposlenik kuca iznos stete, da klijent cuje konacnu
/// brojku prije nego se bilo sta upise.
class PovratDijalog extends StatefulWidget {
  const PovratDijalog({super.key, required this.stavka});

  final RasporedStavka stavka;

  @override
  State<PovratDijalog> createState() => _PovratDijalogStanje();
}

class _PovratDijalogStanje extends State<PovratDijalog> {
  final _forma = GlobalKey<FormState>();
  final _kilometraza = TextEditingController();
  final _opisStete = TextEditingController();
  final _iznosStete = TextEditingController();
  final _napomena = TextEditingController();

  late final PrimopredajaServis _servis;

  int _gorivo = 100;
  bool _imaOstecenje = false;
  DateTime? _blokirajDo;
  DateTime? _stvarnoVrijeme;
  List<FajlZaSlanje> _fotografije = const [];

  ObracunPovrata? _obracun;
  bool _ucitavanje = true;
  bool _racuna = false;
  int _zadnjiObracun = 0;

  bool _snimanje = false;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = PrimopredajaServis(context.read<ApiKlijent>());
    _ucitajObracun(prvi: true);
  }

  @override
  void dispose() {
    _kilometraza.dispose();
    _opisStete.dispose();
    _iznosStete.dispose();
    _napomena.dispose();
    super.dispose();
  }

  double? get _steta {
    if (!_imaOstecenje) {
      return null;
    }

    return double.tryParse(_iznosStete.text.trim().replaceAll(',', '.'));
  }

  Future<void> _ucitajObracun({bool prvi = false}) async {
    final redniBroj = ++_zadnjiObracun;

    setState(() => _racuna = true);

    try {
      final obracun = await _servis.obracunPovrata(
        widget.stavka.rezervacijaId,
        iznosStete: _steta,
        datumPovrata: _stvarnoVrijeme,
      );

      if (!mounted || redniBroj != _zadnjiObracun) {
        return;
      }

      setState(() {
        _obracun = obracun;
        _racuna = false;
        _ucitavanje = false;

        // Kilometraza sa izdavanja je donja granica i ujedno korisna polazna
        // vrijednost - vozilo ne moze imati manje nego kad je izdato.
        if (prvi && obracun.kilometrazaPriIzdavanju != null) {
          _kilometraza.text = obracun.kilometrazaPriIzdavanju.toString();
        }
      });
    } on ApiGreska catch (greska) {
      if (!mounted || redniBroj != _zadnjiObracun) {
        return;
      }

      setState(() {
        _racuna = false;
        _ucitavanje = false;
        _greska = greska.poruka;
      });
    }
  }

  Future<void> _odaberiBlokadu() async {
    final datum = await showDatePicker(
      context: context,
      initialDate: _blokirajDo ?? DateTime.now().add(const Duration(days: 3)),
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 365)),
      helpText: 'Vozilo je nedostupno do',
    );

    if (datum != null) {
      setState(() => _blokirajDo = datum);
    }
  }

  Future<void> _sacuvaj() async {
    if (!_forma.currentState!.validate()) {
      return;
    }

    if (_imaOstecenje && _fotografije.isEmpty) {
      setState(
        () => _greska = 'Uz oštećenje je obavezna bar jedna fotografija.',
      );

      return;
    }

    setState(() {
      _snimanje = true;
      _greska = null;
    });

    try {
      await _servis.vrati(
        rezervacijaId: widget.stavka.rezervacijaId,
        kilometraza: int.parse(_kilometraza.text.trim()),
        nivoGoriva: _gorivo,
        imaOstecenje: _imaOstecenje,
        opisStete: _imaOstecenje ? _opisStete.text.trim() : null,
        iznosStete: _steta,
        napomena: _napomena.text.trim().isEmpty ? null : _napomena.text.trim(),
        blokirajVoziloDo: _blokirajDo,
        datumPovrata: _stvarnoVrijeme,
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

  /// Vrijeme koje se nudi kad uposlenik kaze da je vracanje bilo ranije.
  ///
  /// Ugovoreno vracanje ako je proslo, inace sada. Ne uzima se vrijeme izdavanja,
  /// jer bi tada obracun startao sa punim kasnjenjem unazad.
  DateTime _pocetnoVrijeme() {
    final ugovoreno = widget.stavka.datumDo.toLocal();
    final sada = DateTime.now();

    return ugovoreno.isBefore(sada) ? ugovoreno : sada;
  }

  @override
  Widget build(BuildContext context) {
    final stavka = widget.stavka;

    // Bez evidentiranog izdavanja povrat nema smisla i server ga odbija. To se zna
    // cim obracun stigne, pa nema razloga pustiti uposlenika da popuni cijelu formu.
    final izdato = _obracun?.jeIzdato ?? false;

    return DijalogForme(
      naslov: 'Povrat vozila',
      podnaslov:
          '${stavka.broj} · ${stavka.voziloNaziv ?? ''} '
          '${stavka.registarskaOznaka ?? ''} · ${stavka.klijentImePrezime ?? ''}',
      greska: _greska,
      uToku: _snimanje,
      potvrdaOmogucena: izdato,
      natpisPotvrde: 'Evidentiraj povrat',
      naSnimanje: _sacuvaj,
      sirina: 900,
      dijete: _ucitavanje
          ? const Padding(
              padding: EdgeInsets.all(Razmaci.xxl),
              child: Center(child: CircularProgressIndicator()),
            )
          : Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                if (!izdato) ...[
                  Obavjestenje.upozorenje(
                    'Za ovu rezervaciju nije evidentirano izdavanje vozila, pa se '
                    'povrat ne može upisati. Prvo evidentirajte izdavanje.',
                  ),
                  const SizedBox(height: Razmaci.l),
                ],
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(flex: 3, child: _unos()),
                    const SizedBox(width: Razmaci.xl),
                    Expanded(flex: 2, child: _bocno()),
                  ],
                ),
              ],
            ),
    );
  }

  Widget _unos() {
    final obracun = _obracun;

    return Form(
      key: _forma,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TextFormField(
            controller: _kilometraza,
            keyboardType: TextInputType.number,
            inputFormatters: [FilteringTextInputFormatter.digitsOnly],
            decoration: const InputDecoration(
              labelText: 'Kilometraža pri povratu',
              suffixText: 'km',
            ),
            validator: (vrijednost) {
              final km = int.tryParse(vrijednost?.trim() ?? '');

              if (km == null || km < 0 || km > 2000000) {
                return 'Unesite kilometražu između 0 i 2.000.000.';
              }

              final pri = obracun?.kilometrazaPriIzdavanju;

              // Vozilo ne moze imati manje kilometara nego kad je izdato. Greska se
              // hvata ovdje, dok uposlenik jos gleda u polje, a ne nakon slanja.
              if (pri != null && km < pri) {
                return 'Pri izdavanju je bilo $pri km, pa povrat ne može biti manji.';
              }

              return null;
            },
          ),
          const SizedBox(height: Razmaci.l),
          KlizacGoriva(
            vrijednost: _gorivo,
            jeElektricno: widget.stavka.jeElektricno,
            naPromjenu: (novo) => setState(() => _gorivo = novo),
          ),
          const SizedBox(height: Razmaci.s),
          NaknadniUnos(
            natpis: 'Vozilo je vraćeno ranije nego što ovo upisujem',
            pojasnjenje:
                'Iz ovog vremena se računa kašnjenje, pa i doplata koja se '
                'zadržava od depozita. Obračun desno se odmah osvježi.',
            vrijeme: _stvarnoVrijeme,
            podrazumijevano: _pocetnoVrijeme(),
            najranije: _obracun?.datumIzdavanja?.toLocal(),
            najkasnije: DateTime.now(),
            naPromjenu: (novo) {
              setState(() => _stvarnoVrijeme = novo);
              _ucitajObracun();
            },
          ),
          const SizedBox(height: Razmaci.s),
          CheckboxListTile(
            value: _imaOstecenje,
            onChanged: (novo) {
              setState(() {
                _imaOstecenje = novo ?? false;
                _greska = null;
              });

              _ucitajObracun();
            },
            contentPadding: EdgeInsets.zero,
            controlAffinity: ListTileControlAffinity.leading,
            title: const Text('Vozilo je vraćeno sa oštećenjem'),
          ),
          if (_imaOstecenje) ...[
            const SizedBox(height: Razmaci.s),
            TextFormField(
              controller: _opisStete,
              maxLines: 2,
              maxLength: 1000,
              decoration: const InputDecoration(
                labelText: 'Opis oštećenja',
                alignLabelWithHint: true,
              ),
              validator: (vrijednost) {
                if (!_imaOstecenje) {
                  return null;
                }

                return (vrijednost?.trim().isEmpty ?? true)
                    ? 'Opišite oštećenje.'
                    : null;
              },
            ),
            TextFormField(
              controller: _iznosStete,
              keyboardType: const TextInputType.numberWithOptions(
                decimal: true,
              ),
              decoration: const InputDecoration(
                labelText: 'Procijenjeni iznos štete',
                suffixText: '€',
              ),
              onChanged: (_) => _ucitajObracun(),
              validator: (vrijednost) {
                if (!_imaOstecenje) {
                  return null;
                }

                final iznos = double.tryParse(
                  (vrijednost ?? '').trim().replaceAll(',', '.'),
                );

                if (iznos == null || iznos <= 0 || iznos > 100000) {
                  return 'Unesite iznos između 0 i 100.000 €.';
                }

                return null;
              },
            ),
            const SizedBox(height: Razmaci.m),
            InkWell(
              onTap: _odaberiBlokadu,
              borderRadius: BorderRadius.circular(Zaobljenja.polje),
              child: InputDecorator(
                decoration: InputDecoration(
                  labelText: 'Vozilo nedostupno do (nije obavezno)',
                  suffixIcon: _blokirajDo == null
                      ? const Icon(Icons.event_outlined, size: 18)
                      : IconButton(
                          tooltip: 'Poništi',
                          icon: const Icon(Icons.close, size: 17),
                          onPressed: () => setState(() => _blokirajDo = null),
                        ),
                ),
                child: Text(
                  _blokirajDo == null
                      ? 'Vozilo ostaje u ponudi'
                      : Formati.datum(_blokirajDo!),
                ),
              ),
            ),
          ],
          const SizedBox(height: Razmaci.l),
          TextFormField(
            controller: _napomena,
            maxLines: 2,
            maxLength: 1000,
            decoration: const InputDecoration(
              labelText: 'Napomena (nije obavezna)',
              alignLabelWithHint: true,
            ),
          ),
          const SizedBox(height: Razmaci.s),
          Text(
            _imaOstecenje
                ? 'Fotografije oštećenja (obavezno)'
                : 'Fotografije pri povratu',
            style: const TextStyle(fontSize: 13.5, fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: Razmaci.m),
          BiracFotografija(
            fotografije: _fotografije,
            naPromjenu: (nove) => setState(() {
              _fotografije = nove;
              _greska = null;
            }),
          ),
        ],
      ),
    );
  }

  Widget _bocno() {
    final obracun = _obracun;

    if (obracun == null) {
      return const SizedBox.shrink();
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _Panel(
          naslov: 'Zapis o izdavanju',
          dijete: obracun.jeIzdato
              ? Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    _Red(
                      'Izdato',
                      Formati.datumIVrijeme(obracun.datumIzdavanja!),
                    ),
                    if (obracun.izdaoKorisnikIme != null)
                      _Red('Izdao', obracun.izdaoKorisnikIme!),
                    if (obracun.kilometrazaPriIzdavanju != null)
                      _Red(
                        'Kilometraža',
                        '${obracun.kilometrazaPriIzdavanju} km',
                      ),
                    if (obracun.nivoGorivaPriIzdavanju != null)
                      _Red(
                        widget.stavka.jeElektricno ? 'Baterija' : 'Gorivo',
                        '${obracun.nivoGorivaPriIzdavanju} %',
                      ),
                    _Red(
                      'Fotografije',
                      '${obracun.brojFotografijaPriIzdavanju}',
                    ),
                  ],
                )
              : const Text(
                  'Izdavanje nije evidentirano.',
                  style: TextStyle(color: Boje.upozorenjeTekst, fontSize: 12.5),
                ),
        ),
        const SizedBox(height: Razmaci.l),
        _Panel(
          naslov: 'Obračun depozita',
          uToku: _racuna,
          dijete: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              _Red(
                'Ugovoreno vraćanje',
                Formati.datumIVrijeme(obracun.ugovorenoVracanje),
              ),
              if (obracun.kasnjenjeMinuta > 0)
                _Red(
                  'Kašnjenje',
                  obracun.unutarTolerancije
                      ? '${obracun.kasnjenjeMinuta} min (ne naplaćuje se)'
                      : '${obracun.kasnjenjeMinuta} min',
                ),
              if (obracun.danaPrekoracenja > 0)
                _Red(
                  'Doplata, ${obracun.danaPrekoracenja} d × '
                  '${Formati.novac(obracun.dnevnaCijena)}',
                  Formati.novac(obracun.doplata),
                ),
              if (obracun.iznosStete > 0)
                _Red('Šteta', Formati.novac(obracun.iznosStete)),
              const Divider(height: Razmaci.xl),
              _Red('Uplaćeni depozit', Formati.novac(obracun.uplaceniDepozit)),
              _Red(
                'Zadržava agencija',
                Formati.novac(obracun.zadrzanoOdDepozita),
              ),
              _Red(
                'Povrat klijentu',
                Formati.novac(obracun.povratDepozita),
                istaknuto: true,
              ),
              if (obracun.nepokrivenoDepozitom > 0) ...[
                const SizedBox(height: Razmaci.m),
                Obavjestenje.upozorenje(
                  'Depozit ne pokriva ${Formati.novac(obracun.nepokrivenoDepozitom)}. '
                  'Tu razliku agencija naplaćuje van sistema.',
                ),
              ],
              const SizedBox(height: Razmaci.m),
              Text(
                obracun.obrazlozenje,
                style: const TextStyle(
                  color: Boje.tekstPrigusen,
                  fontSize: 11.5,
                  height: 1.4,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _Panel extends StatelessWidget {
  const _Panel({
    required this.naslov,
    required this.dijete,
    this.uToku = false,
  });

  final String naslov;
  final Widget dijete;
  final bool uToku;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(Razmaci.l),
      decoration: BoxDecoration(
        color: Boje.platno,
        borderRadius: BorderRadius.circular(Zaobljenja.kartica),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  naslov,
                  style: const TextStyle(
                    fontSize: 13.5,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
              if (uToku)
                const SizedBox(
                  width: 14,
                  height: 14,
                  child: CircularProgressIndicator(strokeWidth: 2),
                ),
            ],
          ),
          const SizedBox(height: Razmaci.m),
          dijete,
        ],
      ),
    );
  }
}

class _Red extends StatelessWidget {
  const _Red(this.natpis, this.vrijednost, {this.istaknuto = false});

  final String natpis;
  final String vrijednost;
  final bool istaknuto;

  @override
  Widget build(BuildContext context) {
    final stil = TextStyle(
      fontSize: istaknuto ? 14.5 : 12.5,
      fontWeight: istaknuto ? FontWeight.w700 : FontWeight.w400,
      color: istaknuto ? Boje.tekst : Boje.tekstBlazi,
    );

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(child: Text(natpis, style: stil)),
          const SizedBox(width: Razmaci.s),
          Text(vrijednost, style: stil),
        ],
      ),
    );
  }
}
