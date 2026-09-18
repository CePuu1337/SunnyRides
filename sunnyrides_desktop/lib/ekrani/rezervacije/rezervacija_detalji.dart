import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/rezervacija.dart';
import '../../servisi/rezervacija_servis.dart';
import '../../widgeti/kartica.dart';
import '../../widgeti/sadrzaj.dart';
import '../../widgeti/slicica.dart';
import 'otkazivanje_dijalog.dart';

/// Sve o jednoj rezervaciji: vozilo, klijent, period, obracun i historija statusa.
class RezervacijaDetalji extends StatefulWidget {
  const RezervacijaDetalji({super.key, required this.rezervacijaId});

  final int rezervacijaId;

  @override
  State<RezervacijaDetalji> createState() => _RezervacijaDetaljiStanje();
}

class _RezervacijaDetaljiStanje extends State<RezervacijaDetalji> {
  late final RezervacijaServis _servis;

  Rezervacija? _rezervacija;
  bool _ucitavanje = true;
  bool _promijenjeno = false;
  String? _greska;

  @override
  void initState() {
    super.initState();

    _servis = RezervacijaServis(context.read<ApiKlijent>());
    _ucitaj();
  }

  Future<void> _ucitaj() async {
    setState(() {
      _ucitavanje = true;
      _greska = null;
    });

    try {
      final rezervacija = await _servis.detalji(widget.rezervacijaId);

      if (!mounted) {
        return;
      }

      setState(() {
        _rezervacija = rezervacija;
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

  Future<void> _otkazi() async {
    final otkazano = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (context) => OtkazivanjeDijalog(rezervacija: _rezervacija!),
    );

    if (otkazano == true) {
      _promijenjeno = true;
      await _ucitaj();
    }
  }

  @override
  Widget build(BuildContext context) {
    final rezervacija = _rezervacija;

    return Scaffold(
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _Traka(
            broj: rezervacija?.broj,
            status: rezervacija?.status,
            placeno: rezervacija?.isPaid,
            mozeOtkazati: rezervacija?.seMozeOtkazati ?? false,
            naPovratak: () => Navigator.of(context).pop(_promijenjeno),
            naOtkazivanje: _otkazi,
          ),
          Expanded(
            child: Sadrzaj(
              ucitavanje: _ucitavanje,
              greska: _greska,
              naPonovniPokusaj: _ucitaj,
              dijete: rezervacija == null
                  ? const SizedBox.shrink()
                  : _Prikaz(rezervacija: rezervacija),
            ),
          ),
        ],
      ),
    );
  }
}

class _Traka extends StatelessWidget {
  const _Traka({
    required this.broj,
    required this.status,
    required this.placeno,
    required this.mozeOtkazati,
    required this.naPovratak,
    required this.naOtkazivanje,
  });

  final String? broj;
  final StatusRezervacije? status;
  final bool? placeno;
  final bool mozeOtkazati;
  final VoidCallback naPovratak;
  final VoidCallback naOtkazivanje;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: Razmaci.ekranMargina,
        vertical: Razmaci.m,
      ),
      decoration: const BoxDecoration(
        color: Boje.povrsina,
        border: Border(bottom: BorderSide(color: Boje.ivica)),
      ),
      child: Row(
        children: [
          IconButton(
            tooltip: 'Nazad na listu',
            onPressed: naPovratak,
            icon: const Icon(Icons.arrow_back, size: 20),
          ),
          const SizedBox(width: Razmaci.s),
          Text(
            broj ?? 'Rezervacija',
            style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
          ),
          const SizedBox(width: Razmaci.m),
          if (status != null) StatusnaPilula.rezervacija(status),
          const SizedBox(width: Razmaci.s),
          if (placeno != null) StatusnaPilula.placeno(placeno!),
          const Spacer(),
          if (mozeOtkazati)
            OutlinedButton.icon(
              onPressed: naOtkazivanje,
              style: OutlinedButton.styleFrom(
                foregroundColor: Boje.greskaTekst,
                side: const BorderSide(color: Boje.greskaPozadina),
              ),
              icon: const Icon(Icons.cancel_outlined, size: 17),
              label: const Text('Otkaži rezervaciju'),
            ),
        ],
      ),
    );
  }
}

class _Prikaz extends StatelessWidget {
  const _Prikaz({required this.rezervacija});

  final Rezervacija rezervacija;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(Razmaci.ekranMargina),
      child: LayoutBuilder(
        builder: (context, ogranicenja) {
          final usko = ogranicenja.maxWidth < 1000;

          final lijevo = Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Kartica(
                naslov: 'Najam',
                podnaslov: 'Vozilo, period i mjesto preuzimanja',
                dijete: _Najam(rezervacija: rezervacija),
              ),
              const SizedBox(height: Razmaci.l),
              Kartica(
                naslov: 'Tok rezervacije',
                podnaslov: 'Svaka promjena statusa, redom kojim se desila',
                dijete: rezervacija.historijaStatusa.isEmpty
                    ? const PrazanPopis(poruka: 'Nema zabilježenih promjena.')
                    : _Historija(stavke: rezervacija.historijaStatusa),
              ),
            ],
          );

          final desno = Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Kartica(
                naslov: 'Klijent',
                dijete: _Klijent(rezervacija: rezervacija),
              ),
              const SizedBox(height: Razmaci.l),
              Kartica(
                naslov: 'Obračun',
                podnaslov: 'Iznos koji je rezervacija nosila',
                dijete: _Obracun(rezervacija: rezervacija),
              ),
              if (rezervacija.status == StatusRezervacije.otkazana) ...[
                const SizedBox(height: Razmaci.l),
                Kartica(
                  naslov: 'Otkazivanje',
                  dijete: _Otkazivanje(rezervacija: rezervacija),
                ),
              ],
            ],
          );

          if (usko) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                lijevo,
                const SizedBox(height: Razmaci.l),
                desno,
              ],
            );
          }

          return Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(flex: 3, child: lijevo),
              const SizedBox(width: Razmaci.l),
              Expanded(flex: 2, child: desno),
            ],
          );
        },
      ),
    );
  }
}

class _Najam extends StatelessWidget {
  const _Najam({required this.rezervacija});

  final Rezervacija rezervacija;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            Slicica(
              putanja: rezervacija.thumbnailUrl,
              sirina: 92,
              visina: 68,
              zamjenskaIkona: Icons.two_wheeler_outlined,
            ),
            const SizedBox(width: Razmaci.l),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    rezervacija.vozilo,
                    style: const TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    rezervacija.registarskaOznaka ?? '',
                    style: const TextStyle(
                      color: Boje.tekstPrigusen,
                      fontSize: 12.5,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
        const SizedBox(height: Razmaci.l),
        const Divider(height: 1),
        const SizedBox(height: Razmaci.l),
        _Stavka(
          'Preuzimanje',
          Formati.datumIVrijeme(rezervacija.datumOd),
          ikona: Icons.north_east,
        ),
        _Stavka(
          'Vraćanje',
          Formati.datumIVrijeme(rezervacija.datumDo),
          ikona: Icons.south_west,
        ),
        _Stavka(
          'Trajanje',
          Formati.trajanje(rezervacija.datumOd, rezervacija.datumDo),
          ikona: Icons.schedule,
        ),
        _Stavka(
          'Poslovnica',
          rezervacija.poslovnicaNaziv ?? '',
          ikona: Icons.store_outlined,
        ),
        if (rezervacija.paketOsiguranjaNaziv != null)
          _Stavka(
            'Osiguranje',
            rezervacija.paketOsiguranjaNaziv!,
            ikona: Icons.shield_outlined,
          ),
        if (rezervacija.stavkeOpreme.isNotEmpty) ...[
          const SizedBox(height: Razmaci.m),
          const Text(
            'Dodatna oprema',
            style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: Razmaci.s),
          for (final oprema in rezervacija.stavkeOpreme)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 2),
              child: Row(
                children: [
                  Expanded(
                    child: Text(
                      '${oprema.naziv} × ${oprema.kolicina}',
                      style: const TextStyle(
                        fontSize: 13,
                        color: Boje.tekstBlazi,
                      ),
                    ),
                  ),
                  Text(
                    Formati.novac(oprema.iznos),
                    style: const TextStyle(
                      fontSize: 13,
                      color: Boje.tekstBlazi,
                    ),
                  ),
                ],
              ),
            ),
        ],
      ],
    );
  }
}

class _Klijent extends StatelessWidget {
  const _Klijent({required this.rezervacija});

  final Rezervacija rezervacija;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _Stavka(
          'Ime i prezime',
          rezervacija.klijentImePrezime ?? '',
          ikona: Icons.person_outline,
        ),
        _Stavka(
          'Email',
          rezervacija.klijentEmail ?? '',
          ikona: Icons.mail_outline,
        ),
        _Stavka(
          'Rezervisano',
          Formati.datumIVrijeme(rezervacija.datumKreiranja),
          ikona: Icons.event_outlined,
        ),
        if (rezervacija.drziDo != null && !rezervacija.isPaid)
          _Stavka(
            'Termin se drži do',
            Formati.datumIVrijeme(rezervacija.drziDo!),
            ikona: Icons.hourglass_bottom,
          ),
      ],
    );
  }
}

class _Obracun extends StatelessWidget {
  const _Obracun({required this.rezervacija});

  final Rezervacija rezervacija;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        if (rezervacija.iznosPopusta > 0)
          _RedIznosa('Popust', '− ${Formati.novac(rezervacija.iznosPopusta)}'),
        _RedIznosa('Depozit', Formati.novac(rezervacija.iznosDepozita)),
        const Divider(height: Razmaci.xl),
        _RedIznosa(
          'Ukupno',
          Formati.novac(rezervacija.ukupanIznos),
          istaknuto: true,
        ),
      ],
    );
  }
}

class _Otkazivanje extends StatelessWidget {
  const _Otkazivanje({required this.rezervacija});

  final Rezervacija rezervacija;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (rezervacija.datumOtkazivanja != null)
          _Stavka(
            'Otkazano',
            Formati.datumIVrijeme(rezervacija.datumOtkazivanja!),
            ikona: Icons.event_busy_outlined,
          ),
        _Stavka(
          'Razlog',
          rezervacija.razlogOtkazivanjaNaziv ?? 'Otkazao sistem',
          ikona: Icons.help_outline,
        ),
        if (rezervacija.otkazaoKorisnikIme != null)
          _Stavka(
            'Otkazao',
            rezervacija.otkazaoKorisnikIme!,
            ikona: Icons.person_outline,
          ),
        if (rezervacija.napomenaOtkazivanja != null &&
            rezervacija.napomenaOtkazivanja!.isNotEmpty) ...[
          const SizedBox(height: Razmaci.s),
          Text(
            rezervacija.napomenaOtkazivanja!,
            style: const TextStyle(
              color: Boje.tekstPrigusen,
              fontSize: 12.5,
              height: 1.4,
            ),
          ),
        ],
      ],
    );
  }
}

class _Historija extends StatelessWidget {
  const _Historija({required this.stavke});

  final List<HistorijaStatusa> stavke;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        for (var i = 0; i < stavke.length; i++)
          _Korak(stavka: stavke[i], zadnji: i == stavke.length - 1),
      ],
    );
  }
}

class _Korak extends StatelessWidget {
  const _Korak({required this.stavka, required this.zadnji});

  final HistorijaStatusa stavka;
  final bool zadnji;

  @override
  Widget build(BuildContext context) {
    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Column(
            children: [
              Container(
                width: 9,
                height: 9,
                margin: const EdgeInsets.only(top: 5),
                decoration: const BoxDecoration(
                  color: Boje.primarna,
                  shape: BoxShape.circle,
                ),
              ),
              if (!zadnji)
                Expanded(child: Container(width: 1.5, color: Boje.ivica)),
            ],
          ),
          const SizedBox(width: Razmaci.m),
          Expanded(
            child: Padding(
              padding: EdgeInsets.only(bottom: zadnji ? 0 : Razmaci.l),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    stavka.opis,
                    style: const TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    [
                      Formati.datumIVrijeme(stavka.datumVrijeme),
                      if (stavka.izvrsioKorisnikIme != null)
                        stavka.izvrsioKorisnikIme!,
                    ].join(' · '),
                    style: const TextStyle(
                      color: Boje.tekstPrigusen,
                      fontSize: 11.5,
                    ),
                  ),
                  if (stavka.razlog != null && stavka.razlog!.isNotEmpty) ...[
                    const SizedBox(height: 2),
                    Text(
                      stavka.razlog!,
                      style: const TextStyle(
                        color: Boje.tekstPrigusen,
                        fontSize: 12,
                        fontStyle: FontStyle.italic,
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _Stavka extends StatelessWidget {
  const _Stavka(this.natpis, this.vrijednost, {required this.ikona});

  final String natpis;
  final String vrijednost;
  final IconData ikona;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: Razmaci.xs),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(ikona, size: 16, color: Boje.tekstPrigusen),
          const SizedBox(width: Razmaci.m),
          SizedBox(
            width: 130,
            child: Text(
              natpis,
              style: const TextStyle(color: Boje.tekstPrigusen, fontSize: 12.5),
            ),
          ),
          Expanded(
            child: Text(
              vrijednost,
              style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w500),
            ),
          ),
        ],
      ),
    );
  }
}

class _RedIznosa extends StatelessWidget {
  const _RedIznosa(this.natpis, this.vrijednost, {this.istaknuto = false});

  final String natpis;
  final String vrijednost;
  final bool istaknuto;

  @override
  Widget build(BuildContext context) {
    final stil = TextStyle(
      fontSize: istaknuto ? 15 : 13,
      fontWeight: istaknuto ? FontWeight.w700 : FontWeight.w400,
      color: istaknuto ? Boje.tekst : Boje.tekstBlazi,
    );

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        children: [
          Expanded(child: Text(natpis, style: stil)),
          Text(vrijednost, style: stil),
        ],
      ),
    );
  }
}
