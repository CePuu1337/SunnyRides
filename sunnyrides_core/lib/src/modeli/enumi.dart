/// Enumi koje API salje kao brojeve.
///
/// Vrijednosti moraju odgovarati onima u SunnyRides.Model.Enums. Zato je broj
/// upisan uz svaku stavku umjesto da se oslanja na redoslijed - da premjestanje
/// stavke u jednom od dva projekta ne bi tiho pomjerilo znacenje.
library;

enum StatusRezervacije {
  naCekanju(1, 'Na čekanju'),
  potvrdjena(2, 'Potvrđena'),
  otkazana(3, 'Otkazana'),
  zavrsena(4, 'Završena');

  const StatusRezervacije(this.vrijednost, this.naziv);

  final int vrijednost;
  final String naziv;

  static StatusRezervacije? izBroja(int? broj) {
    for (final status in StatusRezervacije.values) {
      if (status.vrijednost == broj) {
        return status;
      }
    }

    return null;
  }
}

enum StatusPlacanja {
  kreirano(1, 'Kreirano'),
  uObradi(2, 'U obradi'),
  uspjesno(3, 'Uspješno'),
  neuspjesno(4, 'Neuspješno'),
  ponisteno(5, 'Poništeno');

  const StatusPlacanja(this.vrijednost, this.naziv);

  final int vrijednost;
  final String naziv;

  static StatusPlacanja? izBroja(int? broj) {
    for (final status in StatusPlacanja.values) {
      if (status.vrijednost == broj) {
        return status;
      }
    }

    return null;
  }
}

enum StatusDozvole {
  naCekanju(1, 'Čeka verifikaciju'),
  odobrena(2, 'Odobrena'),
  odbijena(3, 'Odbijena');

  const StatusDozvole(this.vrijednost, this.naziv);

  final int vrijednost;
  final String naziv;

  static StatusDozvole? izBroja(int? broj) {
    for (final status in StatusDozvole.values) {
      if (status.vrijednost == broj) {
        return status;
      }
    }

    return null;
  }
}

enum TipPrimopredaje {
  izdavanje(1, 'Izdavanje'),
  povrat(2, 'Povrat');

  const TipPrimopredaje(this.vrijednost, this.naziv);

  final int vrijednost;
  final String naziv;

  static TipPrimopredaje? izBroja(int? broj) {
    for (final tip in TipPrimopredaje.values) {
      if (tip.vrijednost == broj) {
        return tip;
      }
    }

    return null;
  }
}

enum VrstaBlokaKalendara {
  rezervacija(1, 'Rezervacija'),
  blokada(2, 'Blokada');

  const VrstaBlokaKalendara(this.vrijednost, this.naziv);

  final int vrijednost;
  final String naziv;

  static VrstaBlokaKalendara? izBroja(int? broj) {
    for (final vrsta in VrstaBlokaKalendara.values) {
      if (vrsta.vrijednost == broj) {
        return vrsta;
      }
    }

    return null;
  }
}

enum MetodaPreporuke {
  matricnaFaktorizacija(1, 'Model naučen iz ocjena'),
  rezervnaHeuristika(2, 'Slična vozila i popularnost');

  const MetodaPreporuke(this.vrijednost, this.naziv);

  final int vrijednost;
  final String naziv;

  static MetodaPreporuke? izBroja(int? broj) {
    for (final metoda in MetodaPreporuke.values) {
      if (metoda.vrijednost == broj) {
        return metoda;
      }
    }

    return null;
  }
}

enum TipNotifikacije {
  rezervacijaKreirana(1),
  placanjeUspjesno(2),
  rezervacijaPotvrdjena(3),
  rezervacijaOtkazana(4),
  povratIzvrsen(5),
  dozvolaOdobrena(6),
  dozvolaOdbijena(7),
  podsjetnikPreuzimanje(8),
  voziloVraceno(9),
  resetLozinke(10);

  const TipNotifikacije(this.vrijednost);

  final int vrijednost;

  static TipNotifikacije? izBroja(int? broj) {
    for (final tip in TipNotifikacije.values) {
      if (tip.vrijednost == broj) {
        return tip;
      }
    }

    return null;
  }
}
