import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Korijen kao u aplikaciji: pocetni ekran zavisi od stanja.
class _Korijen extends StatelessWidget {
  const _Korijen({required this.stanje});

  final ValueNotifier<bool> stanje;

  @override
  Widget build(BuildContext context) {
    return ValueListenableBuilder<bool>(
      valueListenable: stanje,
      builder: (context, prijavljen, _) => ZatvoriEkraneNaPromjenu(
        kljuc: prijavljen,
        child: Scaffold(body: Text(prijavljen ? 'Pocetna' : 'Prijava')),
      ),
    );
  }
}

void main() {
  testWidgets('prijava zatvara ekran registracije otvoren preko prijave', (
    tester,
  ) async {
    final stanje = ValueNotifier(false);
    final navigator = GlobalKey<NavigatorState>();

    await tester.pumpWidget(
      MaterialApp(navigatorKey: navigator, home: _Korijen(stanje: stanje)),
    );

    navigator.currentState!.push(
      MaterialPageRoute<void>(
        builder: (_) => const Scaffold(body: Text('Registracija')),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Registracija'), findsOneWidget);

    stanje.value = true;
    await tester.pumpAndSettle();

    expect(find.text('Registracija'), findsNothing);
    expect(find.text('Pocetna'), findsOneWidget);
  });

  testWidgets('istek sesije zatvara otvoreni dijalog', (tester) async {
    final stanje = ValueNotifier(true);
    final navigator = GlobalKey<NavigatorState>();

    await tester.pumpWidget(
      MaterialApp(navigatorKey: navigator, home: _Korijen(stanje: stanje)),
    );

    showDialog<void>(
      context: navigator.currentContext!,
      builder: (_) => const AlertDialog(content: Text('Potvrda')),
    );
    await tester.pumpAndSettle();
    expect(find.text('Potvrda'), findsOneWidget);

    stanje.value = false;
    await tester.pumpAndSettle();

    expect(find.text('Potvrda'), findsNothing);
    expect(find.text('Prijava'), findsOneWidget);
  });
}
