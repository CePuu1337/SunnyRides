import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Jedan unos sifrarnika sveden na ono sto padajuca lista treba - broj i natpis.
///
/// Sifrarnici imaju razlicita polja (marka ima naziv, kategorija dozvole oznaku),
/// pa se natpis bira pri ucitavanju. Ekranu je svejedno odakle je dosao.
class StavkaSifrarnika {
  const StavkaSifrarnika({required this.id, required this.naziv});

  final int id;
  final String naziv;

  factory StavkaSifrarnika.izJsona(Map<String, dynamic> json) {
    final naziv = json['naziv'] ?? json['oznaka'] ?? json['broj'] ?? '';

    return StavkaSifrarnika(id: citajInt(json['id']), naziv: naziv.toString());
  }

  @override
  bool operator ==(Object other) => other is StavkaSifrarnika && other.id == id;

  @override
  int get hashCode => id;
}
