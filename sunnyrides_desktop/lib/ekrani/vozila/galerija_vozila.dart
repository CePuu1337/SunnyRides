import 'package:file_selector/file_selector.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

import '../../modeli/vozilo.dart';
import '../../servisi/vozilo_servis.dart';

/// Fotografije vozila unutar forme za izmjenu.
///
/// Galerija postoji samo kod izmjene: vozilo prvo mora biti sacuvano da bi slika
/// imala uz sta stajati. Sadrzaj se salje kao multipart, nikad kao base64 u JSON-u.
class GalerijaVozila extends StatefulWidget {
  const GalerijaVozila({
    super.key,
    required this.voziloId,
    required this.servis,
  });

  final int voziloId;
  final VoziloServis servis;

  @override
  State<GalerijaVozila> createState() => _GalerijaVozilaStanje();
}

class _GalerijaVozilaStanje extends State<GalerijaVozila> {
  List<SlikaVozila> _slike = const [];
  bool _ucitavanje = true;
  bool _otprema = false;
  String? _greska;

  @override
  void initState() {
    super.initState();
    _ucitaj();
  }

  Future<void> _ucitaj() async {
    try {
      final slike = await widget.servis.slike(widget.voziloId);

      if (!mounted) {
        return;
      }

      setState(() {
        _slike = slike;
        _ucitavanje = false;
        _greska = null;
      });
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() {
        _ucitavanje = false;
        _greska = greska.poruka;
      });
    }
  }

  Future<void> _dodaj() async {
    const vrste = XTypeGroup(
      label: 'Slike',
      extensions: ['jpg', 'jpeg', 'png', 'webp'],
    );

    final fajl = await openFile(acceptedTypeGroups: [vrste]);

    if (fajl == null) {
      return;
    }

    setState(() {
      _otprema = true;
      _greska = null;
    });

    try {
      final sadrzaj = await fajl.readAsBytes();

      await widget.servis.dodajSliku(
        widget.voziloId,
        imeFajla: fajl.name,
        sadrzaj: sadrzaj,
      );

      await _ucitaj();
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() => _greska = greska.poruka);
    } finally {
      if (mounted) {
        setState(() => _otprema = false);
      }
    }
  }

  Future<void> _postaviGlavnu(SlikaVozila slika) async {
    try {
      await widget.servis.postaviGlavnu(widget.voziloId, slika.id);
      await _ucitaj();
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() => _greska = greska.poruka);
    }
  }

  Future<void> _obrisi(SlikaVozila slika) async {
    try {
      await widget.servis.obrisiSliku(widget.voziloId, slika.id);
      await _ucitaj();
    } on ApiGreska catch (greska) {
      if (!mounted) {
        return;
      }

      setState(() => _greska = greska.poruka);
    }
  }

  @override
  Widget build(BuildContext context) {
    final okruzenje = context.read<Okruzenje>();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            const Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Fotografije',
                    style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
                  ),
                  Text(
                    'Glavna fotografija se prikazuje u listama i u mobilnoj aplikaciji.',
                    style: TextStyle(color: Boje.tekstPrigusen, fontSize: 12),
                  ),
                ],
              ),
            ),
            OutlinedButton.icon(
              onPressed: _otprema ? null : _dodaj,
              icon: _otprema
                  ? const SizedBox(
                      width: 14,
                      height: 14,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.add_photo_alternate_outlined, size: 18),
              label: const Text('Dodaj sliku'),
            ),
          ],
        ),
        if (_greska != null) ...[
          const SizedBox(height: Razmaci.m),
          Text(
            _greska!,
            style: const TextStyle(color: Boje.greskaTekst, fontSize: 12.5),
          ),
        ],
        const SizedBox(height: Razmaci.l),
        if (_ucitavanje)
          const Center(
            child: Padding(
              padding: EdgeInsets.all(Razmaci.l),
              child: CircularProgressIndicator(),
            ),
          )
        else if (_slike.isEmpty)
          Container(
            padding: const EdgeInsets.all(Razmaci.xl),
            decoration: BoxDecoration(
              color: Boje.platno,
              borderRadius: BorderRadius.circular(Zaobljenja.dugme),
            ),
            child: const Center(
              child: Text(
                'Vozilo još nema nijednu fotografiju.',
                style: TextStyle(color: Boje.tekstPrigusen, fontSize: 12.5),
              ),
            ),
          )
        else
          Wrap(
            spacing: Razmaci.m,
            runSpacing: Razmaci.m,
            children: [
              for (final slika in _slike)
                _Stavka(
                  slika: slika,
                  adresa: okruzenje.apsolutnaSlika(slika.thumbnailUrl),
                  naGlavnu: () => _postaviGlavnu(slika),
                  naBrisanje: () => _obrisi(slika),
                ),
            ],
          ),
      ],
    );
  }
}

class _Stavka extends StatelessWidget {
  const _Stavka({
    required this.slika,
    required this.adresa,
    required this.naGlavnu,
    required this.naBrisanje,
  });

  final SlikaVozila slika;
  final String? adresa;
  final VoidCallback naGlavnu;
  final VoidCallback naBrisanje;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 148,
      decoration: BoxDecoration(
        border: Border.all(
          color: slika.jeGlavna ? Boje.primarna : Boje.ivica,
          width: slika.jeGlavna ? 2 : 1,
        ),
        borderRadius: BorderRadius.circular(Zaobljenja.dugme),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: [
          ClipRRect(
            borderRadius: const BorderRadius.vertical(top: Radius.circular(5)),
            child: SizedBox(
              height: 96,
              child: adresa == null
                  ? Container(color: Boje.platno)
                  : Image.network(adresa!, fit: BoxFit.cover),
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(
              horizontal: Razmaci.s,
              vertical: 2,
            ),
            child: Row(
              children: [
                Expanded(
                  child: slika.jeGlavna
                      ? const Text(
                          'Glavna',
                          style: TextStyle(
                            color: Boje.primarnaTamnija,
                            fontSize: 11.5,
                            fontWeight: FontWeight.w600,
                          ),
                        )
                      : TextButton(
                          style: TextButton.styleFrom(
                            padding: EdgeInsets.zero,
                            minimumSize: const Size(0, 32),
                            textStyle: const TextStyle(fontSize: 11.5),
                          ),
                          onPressed: naGlavnu,
                          child: const Text('Postavi glavnu'),
                        ),
                ),
                IconButton(
                  tooltip: 'Obriši',
                  onPressed: naBrisanje,
                  iconSize: 16,
                  visualDensity: VisualDensity.compact,
                  icon: const Icon(
                    Icons.delete_outline,
                    color: Boje.greskaTekst,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
