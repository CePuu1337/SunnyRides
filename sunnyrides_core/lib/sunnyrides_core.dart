/// Zajednicki sloj desktop i mobilne aplikacije.
///
/// Ovdje zivi sve sto obje aplikacije rade isto: razgovor sa API-jem, cuvanje
/// tokena, modeli koje API vraca i dizajn iz mockupa. Ekrani su jedino sto se
/// razlikuje, i oni ostaju u svojoj aplikaciji.
library;

export 'src/api/api_greska.dart';
export 'src/api/api_klijent.dart';
export 'src/api/okruzenje.dart';
export 'src/api/strana.dart';

export 'src/auth/auth_servis.dart';
export 'src/auth/pohrana_tokena.dart';

export 'src/format.dart';

export 'src/dizajn/boje.dart';
export 'src/dizajn/razmaci.dart';
export 'src/dizajn/statusna_pilula.dart';
export 'src/dizajn/tema.dart';

export 'src/modeli/cijena.dart';
export 'src/modeli/dozvola.dart';
export 'src/modeli/enumi.dart';
export 'src/modeli/korisnik.dart';
export 'src/modeli/pretvaranje.dart';
export 'src/modeli/rezervacija.dart';

export 'src/realtime/veza_notifikacija.dart';

export 'src/stanje/notifikacije_stanje.dart';
