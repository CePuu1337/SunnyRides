/// Racunanje u kalendarskim danima, po lokalnom vremenu.
///
/// Dan nema uvijek 24 sata: na dan prelaska na zimsko vrijeme ima 25, a na ljetno 23.
/// Zato se "sutra" i "sljedeca sedmica" ne racunaju kao plus 24 ili 168 sati, nego kao
/// ponoc dana ciji je redni broj veci za toliko. Sa satima bi sedmica koja pocinje u
/// ponoc ponedjeljka poslije jesenje promjene sata pocinjala u nedjelju u 23:00.
class KalendarskiDani {
  const KalendarskiDani._();

  /// Ponoc dana kojem pripada trenutak, po lokalnom vremenu.
  static DateTime ponoc(DateTime trenutak) {
    final lokalno = trenutak.toLocal();

    return DateTime(lokalno.year, lokalno.month, lokalno.day);
  }

  /// Ponoc dana koji je [dana] kalendarskih dana poslije zadanog (ili prije, kad je
  /// broj negativan). DateTime sam prenosi visak u sljedeci mjesec i godinu.
  static DateTime pomjeri(DateTime dan, int dana) {
    final lokalno = dan.toLocal();

    return DateTime(lokalno.year, lokalno.month, lokalno.day + dana);
  }

  /// Ponoc ponedjeljka u sedmici kojoj dan pripada.
  static DateTime ponedjeljak(DateTime dan) {
    final pocetak = ponoc(dan);

    return pomjeri(pocetak, -(pocetak.weekday - 1));
  }

  /// Koliko je kalendarskih dana od [od] do [doDana], bez obzira na sate u njima.
  static int razlika(DateTime od, DateTime doDana) {
    final a = ponoc(od);
    final b = ponoc(doDana);

    // Ponoci istih datuma u UTC-u su razmaknute tacno 24 sata po danu.
    return DateTime.utc(b.year, b.month, b.day)
        .difference(DateTime.utc(a.year, a.month, a.day))
        .inDays;
  }

  /// Polozaj trenutka u nizu dana koji pocinje danom [pocetak]: cijeli dio je redni
  /// broj dana, a decimalni koliki je dio tog dana prosao. Tako dan od 25 sati u
  /// mrezi zauzima istu sirinu kolone kao obican, a 09:00 je uvijek na istom mjestu.
  static double polozaj(DateTime pocetak, DateTime trenutak) {
    final lokalno = trenutak.toLocal();
    final dan = ponoc(lokalno);
    final duzinaDana = pomjeri(dan, 1).difference(dan).inMinutes;

    return razlika(pocetak, dan) + lokalno.difference(dan).inMinutes / duzinaDana;
  }
}
