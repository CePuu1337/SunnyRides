import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Ono sto PaymentSheet treba da otvori naplatu.
///
/// Iznos je ovdje samo za prikaz - stvarni iznos je upisan u intent na serveru, pa
/// se ne moze promijeniti iz aplikacije.
class PlatniIntent {
  const PlatniIntent({
    required this.placanjeId,
    required this.rezervacijaId,
    required this.iznos,
    required this.valuta,
    required this.isPaid,
    this.clientSecret,
    this.publishableKey,
    this.status,
    this.rezervacijaBroj,
    this.preostaloSekundiDrzanja,
  });

  final int placanjeId;
  final int rezervacijaId;
  final String? rezervacijaBroj;

  /// Prazno kad je placanje vec zavrseno - tada nema sta potvrdjivati.
  final String? clientSecret;

  /// Javni kljuc dolazi sa servera, da ne stoji upisan u aplikaciji.
  final String? publishableKey;

  final double iznos;
  final String valuta;
  final StatusPlacanja? status;
  final bool isPaid;
  final int? preostaloSekundiDrzanja;

  factory PlatniIntent.izJsona(Map<String, dynamic> json) {
    return PlatniIntent(
      placanjeId: citajInt(json['placanjeId']),
      rezervacijaId: citajInt(json['rezervacijaId']),
      rezervacijaBroj: json['rezervacijaBroj']?.toString(),
      clientSecret: json['clientSecret']?.toString(),
      publishableKey: json['publishableKey']?.toString(),
      iznos: citajDouble(json['iznos']),
      valuta: json['valuta']?.toString() ?? 'eur',
      status: StatusPlacanja.izBroja(citajInt(json['status'])),
      isPaid: citajBool(json['isPaid']),
      preostaloSekundiDrzanja: json['preostaloSekundiDrzanja'] == null
          ? null
          : citajInt(json['preostaloSekundiDrzanja']),
    );
  }
}
