import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Mala slika u listi, sa zamjenom kad je nema ili se ne ucita.
///
/// Fotografije vozila i obavijesti se posluzuju kao obicni staticki fajlovi, pa
/// idu direktno kroz Image.network - bez tokena, jer su to katalog i oglasi.
class Slicica extends StatelessWidget {
  const Slicica({
    super.key,
    required this.putanja,
    this.sirina = 52,
    this.visina = 38,
    this.zamjenskaIkona = Icons.image_outlined,
  });

  final String? putanja;
  final double sirina;
  final double visina;
  final IconData zamjenskaIkona;

  @override
  Widget build(BuildContext context) {
    final adresa = context.read<Okruzenje>().apsolutnaSlika(putanja);

    return ClipRRect(
      borderRadius: BorderRadius.circular(Zaobljenja.dugme),
      child: SizedBox(
        width: sirina,
        height: visina,
        child: adresa == null
            ? _Zamjena(ikona: zamjenskaIkona)
            : Image.network(
                adresa,
                fit: BoxFit.cover,
                errorBuilder: (context, greska, trag) =>
                    _Zamjena(ikona: zamjenskaIkona),
              ),
      ),
    );
  }
}

class _Zamjena extends StatelessWidget {
  const _Zamjena({required this.ikona});

  final IconData ikona;

  @override
  Widget build(BuildContext context) {
    return Container(
      color: Boje.platno,
      child: Icon(ikona, size: 18, color: Boje.ivicaJaca),
    );
  }
}
