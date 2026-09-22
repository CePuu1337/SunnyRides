import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Prekidac i birac vremena za primopredaju koja se evidentira naknadno.
///
/// Podrazumijevano se podrazumijeva da se primopredaja desava sada. Kad uposlenik
/// kaze da se desila ranije, upisuje kada - i to vrijeme, a ne trenutak unosa, ulazi
/// u obracun. Zato je prazno stanje isto sto i "sada", a ne neki datum koji je neko
/// zaboravio promijeniti.
class NaknadniUnos extends StatelessWidget {
  const NaknadniUnos({
    super.key,
    required this.natpis,
    required this.pojasnjenje,
    required this.vrijeme,
    required this.naPromjenu,
    required this.podrazumijevano,
    this.najranije,
    this.najkasnije,
  });

  /// Natpis uz kvadratic, na primjer "Vozilo je preuzeto ranije".
  final String natpis;

  final String pojasnjenje;

  /// Odabrano vrijeme, ili prazno kad primopredaja ide kao "sada".
  final DateTime? vrijeme;

  final ValueChanged<DateTime?> naPromjenu;

  /// Vrijeme koje se ponudi kad se prekidac ukljuci.
  final DateTime podrazumijevano;

  final DateTime? najranije;
  final DateTime? najkasnije;

  Future<void> _odaberi(BuildContext context) async {
    final polazno = vrijeme ?? podrazumijevano;
    final sada = DateTime.now();

    final datum = await showDatePicker(
      context: context,
      initialDate: polazno,
      firstDate: najranije ?? sada.subtract(const Duration(days: 60)),
      lastDate: najkasnije ?? sada,
      helpText: 'Datum kad se to desilo',
    );

    if (datum == null || !context.mounted) {
      return;
    }

    final odabrano = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(polazno),
      helpText: 'Vrijeme kad se to desilo',
      builder: (context, dijete) => MediaQuery(
        data: MediaQuery.of(context).copyWith(alwaysUse24HourFormat: true),
        child: dijete!,
      ),
    );

    if (odabrano == null) {
      return;
    }

    naPromjenu(
      DateTime(
        datum.year,
        datum.month,
        datum.day,
        odabrano.hour,
        odabrano.minute,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final ukljuceno = vrijeme != null;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        CheckboxListTile(
          value: ukljuceno,
          onChanged: (novo) =>
              naPromjenu((novo ?? false) ? podrazumijevano : null),
          contentPadding: EdgeInsets.zero,
          controlAffinity: ListTileControlAffinity.leading,
          title: Text(natpis),
          subtitle: Text(pojasnjenje, style: const TextStyle(fontSize: 12)),
        ),
        if (ukljuceno) ...[
          const SizedBox(height: Razmaci.s),
          InkWell(
            onTap: () => _odaberi(context),
            borderRadius: BorderRadius.circular(Zaobljenja.polje),
            child: InputDecorator(
              decoration: const InputDecoration(
                labelText: 'Stvarno vrijeme',
                suffixIcon: Icon(Icons.schedule, size: 18),
              ),
              child: Text(Formati.datumIVrijeme(vrijeme!)),
            ),
          ),
        ],
      ],
    );
  }
}
