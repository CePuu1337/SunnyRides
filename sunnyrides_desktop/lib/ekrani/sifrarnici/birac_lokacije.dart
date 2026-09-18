import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Odabir koordinata klikom na kartu.
///
/// Koordinate se ne kucaju u polje. Niko ne zna napamet geografsku sirinu svoje
/// poslovnice, a i kad bi znao, zamjena dvije cifre daje tacku u moru bez ijedne
/// poruke o gresci. Klik na kartu tu gresku onemogucava.
class BiracLokacije extends StatefulWidget {
  const BiracLokacije({
    super.key,
    required this.naOdabir,
    this.pocetna,
    this.visina = 260,
  });

  final ValueChanged<LatLng> naOdabir;
  final LatLng? pocetna;
  final double visina;

  /// Mostar. Sredina podrucja u kojem agencija radi, pa karta ne krece od okeana.
  static const pocetniPogled = LatLng(43.3438, 17.8078);

  @override
  State<BiracLokacije> createState() => _BiracLokacijeStanje();
}

class _BiracLokacijeStanje extends State<BiracLokacije> {
  LatLng? _odabrana;

  @override
  void initState() {
    super.initState();
    _odabrana = widget.pocetna;
  }

  void _postavi(LatLng tacka) {
    setState(() => _odabrana = tacka);
    widget.naOdabir(tacka);
  }

  @override
  Widget build(BuildContext context) {
    final tacka = _odabrana;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(Zaobljenja.dugme),
          child: SizedBox(
            height: widget.visina,
            child: Stack(
              children: [
                FlutterMap(
                  options: MapOptions(
                    initialCenter: tacka ?? BiracLokacije.pocetniPogled,
                    initialZoom: tacka == null ? 11 : 15,
                    onTap: (dodir, mjesto) => _postavi(mjesto),
                  ),
                  children: [
                    TileLayer(
                      urlTemplate:
                          'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                      userAgentPackageName: 'ba.edu.fit.sunnyrides_desktop',
                    ),
                    if (tacka != null)
                      MarkerLayer(
                        markers: [
                          Marker(
                            point: tacka,
                            width: 36,
                            height: 36,
                            alignment: Alignment.topCenter,
                            child: const Icon(
                              Icons.location_on,
                              size: 34,
                              color: Boje.greska,
                            ),
                          ),
                        ],
                      ),
                  ],
                ),
                Positioned(
                  left: Razmaci.s,
                  top: Razmaci.s,
                  child: Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: Razmaci.m,
                      vertical: Razmaci.xs,
                    ),
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.92),
                      borderRadius: BorderRadius.circular(Zaobljenja.dugme),
                    ),
                    child: const Text(
                      'Kliknite na kartu da postavite lokaciju',
                      style: TextStyle(fontSize: 11.5, color: Boje.tekstBlazi),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: Razmaci.s),
        Row(
          children: [
            Expanded(
              child: Text(
                tacka == null
                    ? 'Lokacija nije postavljena.'
                    : 'Širina ${tacka.latitude.toStringAsFixed(5)}, '
                          'dužina ${tacka.longitude.toStringAsFixed(5)}',
                style: const TextStyle(color: Boje.tekstPrigusen, fontSize: 12),
              ),
            ),
            const Text(
              '© OpenStreetMap',
              style: TextStyle(color: Boje.tekstPrigusen, fontSize: 10.5),
            ),
          ],
        ),
      ],
    );
  }
}
