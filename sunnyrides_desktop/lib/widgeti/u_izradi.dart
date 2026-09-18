import 'package:flutter/material.dart';
import 'package:sunnyrides_core/sunnyrides_core.dart';

/// Privremeni sadrzaj ekrana koji jos nije napisan, da ljuska radi u cjelini.
class UIzradi extends StatelessWidget {
  const UIzradi({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: const [
            Icon(Icons.construction_outlined, size: 40, color: Boje.ivicaJaca),
            SizedBox(height: Razmaci.m),
            Text(
              'Ovaj ekran je u izradi.',
              style: TextStyle(color: Boje.tekstPrigusen, fontSize: 14),
            ),
          ],
        ),
      ),
    );
  }
}
