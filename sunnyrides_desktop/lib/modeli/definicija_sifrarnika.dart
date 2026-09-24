import '../servisi/sifrarnik_servis.dart';

/// Vrsta polja u formi sifrarnika. Po njoj se bira kontrola i nacin provjere.
enum VrstaPolja { tekst, cijelBroj, decimalniBroj, prekidac, veza }

/// Jedno polje u formi sifrarnika.
class PoljeSifrarnika {
  const PoljeSifrarnika({
    required this.kljuc,
    required this.natpis,
    this.vrsta = VrstaPolja.tekst,
    this.obavezno = true,
    this.najmanje,
    this.najvise,
    this.najmanjaVrijednost,
    this.najvecaVrijednost,
    this.veza,
    this.pojasnjenje,
    this.sufiks,
    this.uPolaReda = false,
  });

  /// Naziv polja u JSON-u, isti u zahtjevu i u odgovoru.
  final String kljuc;

  final String natpis;
  final VrstaPolja vrsta;
  final bool obavezno;

  /// Granice duzine teksta.
  final int? najmanje;
  final int? najvise;

  /// Granice broja.
  final num? najmanjaVrijednost;
  final num? najvecaVrijednost;

  /// Putanja sifrarnika iz kojeg se bira vrijednost, za polja vrste veza.
  final String? veza;

  final String? pojasnjenje;
  final String? sufiks;

  /// Polje zauzima pola reda, pa se slaze uz susjedno.
  final bool uPolaReda;
}

/// Jedna kolona u listi sifrarnika.
class KolonaSifrarnika {
  const KolonaSifrarnika({
    required this.kljuc,
    required this.natpis,
    this.sirina,
    this.jeZastavica = false,
  });

  final String kljuc;
  final String natpis;
  final double? sirina;

  /// Vrijednost je tacno/netacno, pa se crta kvacicom umjesto tekstom.
  final bool jeZastavica;
}

/// Opis jednog sifrarnika: gdje zivi, sta se prikazuje i sta se unosi.
///
/// Sifrarnici se razlikuju samo po tome, pa nema razloga da svaki ima svoj ekran.
/// Jedan ekran cita ovaj opis - a onaj ko dodaje novi sifrarnik dopisuje nekoliko
/// redova ovdje umjesto da kopira cijeli fajl.
class DefinicijaSifrarnika {
  const DefinicijaSifrarnika({
    required this.naziv,
    required this.putanja,
    required this.opis,
    required this.kolone,
    required this.polja,
    this.jednina,
  });

  final String naziv;
  final String? jednina;
  final String putanja;
  final String opis;
  final List<KolonaSifrarnika> kolone;
  final List<PoljeSifrarnika> polja;

  String get nazivJednine => jednina ?? naziv;

  /// Svi sifrarnici koje administrator odrzava, redom kojim se prikazuju.
  static const sve = <DefinicijaSifrarnika>[
    DefinicijaSifrarnika(
      naziv: 'Države',
      jednina: 'državu',
      putanja: SifrarnikServis.drzave,
      opis: 'Države u kojima agencija posluje',
      kolone: [
        KolonaSifrarnika(kljuc: 'naziv', natpis: 'NAZIV'),
        KolonaSifrarnika(
          kljuc: 'skracenica',
          natpis: 'SKRAĆENICA',
          sirina: 140,
        ),
      ],
      polja: [
        PoljeSifrarnika(
          kljuc: 'naziv',
          natpis: 'Naziv',
          najmanje: 2,
          najvise: 100,
        ),
        PoljeSifrarnika(
          kljuc: 'skracenica',
          natpis: 'Skraćenica',
          najmanje: 2,
          najvise: 10,
        ),
      ],
    ),
    DefinicijaSifrarnika(
      naziv: 'Gradovi',
      jednina: 'grad',
      putanja: SifrarnikServis.gradovi,
      opis: 'Gradovi u kojima se nalaze poslovnice',
      kolone: [
        KolonaSifrarnika(kljuc: 'naziv', natpis: 'NAZIV'),
        KolonaSifrarnika(kljuc: 'drzavaNaziv', natpis: 'DRŽAVA', sirina: 180),
        KolonaSifrarnika(
          kljuc: 'postanskiBroj',
          natpis: 'POŠTANSKI BROJ',
          sirina: 160,
        ),
      ],
      polja: [
        PoljeSifrarnika(
          kljuc: 'naziv',
          natpis: 'Naziv',
          najmanje: 2,
          najvise: 100,
        ),
        PoljeSifrarnika(
          kljuc: 'drzavaId',
          natpis: 'Država',
          vrsta: VrstaPolja.veza,
          veza: SifrarnikServis.drzave,
        ),
        PoljeSifrarnika(
          kljuc: 'postanskiBroj',
          natpis: 'Poštanski broj',
          obavezno: false,
          najvise: 20,
        ),
      ],
    ),
    DefinicijaSifrarnika(
      naziv: 'Marke',
      jednina: 'marku',
      putanja: SifrarnikServis.marke,
      opis: 'Proizvođači vozila',
      kolone: [KolonaSifrarnika(kljuc: 'naziv', natpis: 'NAZIV')],
      polja: [
        PoljeSifrarnika(
          kljuc: 'naziv',
          natpis: 'Naziv',
          najmanje: 2,
          najvise: 100,
        ),
      ],
    ),
    DefinicijaSifrarnika(
      naziv: 'Tipovi vozila',
      jednina: 'tip vozila',
      putanja: SifrarnikServis.tipoviVozila,
      opis: 'Skuter, motocikl, kvad',
      kolone: [KolonaSifrarnika(kljuc: 'naziv', natpis: 'NAZIV')],
      polja: [
        PoljeSifrarnika(
          kljuc: 'naziv',
          natpis: 'Naziv',
          najmanje: 2,
          najvise: 50,
        ),
      ],
    ),
    DefinicijaSifrarnika(
      naziv: 'Tipovi goriva',
      jednina: 'tip goriva',
      putanja: SifrarnikServis.tipoviGoriva,
      opis: 'Pogon vozila',
      kolone: [
        KolonaSifrarnika(kljuc: 'naziv', natpis: 'NAZIV'),
        KolonaSifrarnika(
          kljuc: 'jeElektricni',
          natpis: 'NA STRUJU',
          sirina: 140,
          jeZastavica: true,
        ),
      ],
      polja: [
        PoljeSifrarnika(
          kljuc: 'naziv',
          natpis: 'Naziv',
          najmanje: 2,
          najvise: 50,
        ),
        PoljeSifrarnika(
          kljuc: 'jeElektricni',
          natpis: 'Vozila se pune strujom',
          vrsta: VrstaPolja.prekidac,
          pojasnjenje:
              'Takvim vozilima se prati napunjenost baterije umjesto nivoa goriva, '
              'a umjesto kubikaže im se prikazuje snaga.',
        ),
      ],
    ),
    DefinicijaSifrarnika(
      naziv: 'Modeli vozila',
      jednina: 'model',
      putanja: SifrarnikServis.modeliVozila,
      opis: 'Model nosi kubikažu, snagu i kategoriju dozvole',
      kolone: [
        KolonaSifrarnika(kljuc: 'naziv', natpis: 'NAZIV'),
        KolonaSifrarnika(kljuc: 'markaNaziv', natpis: 'MARKA', sirina: 150),
        KolonaSifrarnika(kljuc: 'tipVozilaNaziv', natpis: 'TIP', sirina: 130),
        KolonaSifrarnika(kljuc: 'kubikaza', natpis: 'CCM', sirina: 90),
        KolonaSifrarnika(kljuc: 'snagaKw', natpis: 'KW', sirina: 90),
        KolonaSifrarnika(
          kljuc: 'kategorijaDozvoleOznaka',
          natpis: 'KATEGORIJA',
          sirina: 130,
        ),
      ],
      polja: [
        PoljeSifrarnika(
          kljuc: 'naziv',
          natpis: 'Naziv',
          najmanje: 1,
          najvise: 100,
        ),
        PoljeSifrarnika(
          kljuc: 'markaId',
          natpis: 'Marka',
          vrsta: VrstaPolja.veza,
          veza: SifrarnikServis.marke,
          uPolaReda: true,
        ),
        PoljeSifrarnika(
          kljuc: 'tipVozilaId',
          natpis: 'Tip vozila',
          vrsta: VrstaPolja.veza,
          veza: SifrarnikServis.tipoviVozila,
          uPolaReda: true,
        ),
        PoljeSifrarnika(
          kljuc: 'tipGorivaId',
          natpis: 'Tip goriva',
          vrsta: VrstaPolja.veza,
          veza: SifrarnikServis.tipoviGoriva,
          uPolaReda: true,
        ),
        PoljeSifrarnika(
          kljuc: 'kategorijaDozvoleId',
          natpis: 'Kategorija dozvole',
          vrsta: VrstaPolja.veza,
          veza: SifrarnikServis.kategorijeDozvola,
          uPolaReda: true,
          pojasnjenje:
              'Kategorija je svojstvo modela, ne pojedinačnog vozila — dva primjerka '
              'istog modela ne mogu tražiti različite kategorije.',
        ),
        PoljeSifrarnika(
          kljuc: 'kubikaza',
          natpis: 'Kubikaža',
          vrsta: VrstaPolja.cijelBroj,
          najmanjaVrijednost: 0,
          najvecaVrijednost: 3000,
          sufiks: 'ccm',
          uPolaReda: true,
          pojasnjenje: 'Vozila na struju imaju nulu.',
        ),
        PoljeSifrarnika(
          kljuc: 'snagaKw',
          natpis: 'Snaga',
          vrsta: VrstaPolja.decimalniBroj,
          najmanjaVrijednost: 0.1,
          najvecaVrijednost: 300,
          sufiks: 'kW',
          uPolaReda: true,
        ),
      ],
    ),
    DefinicijaSifrarnika(
      naziv: 'Kategorije dozvola',
      jednina: 'kategoriju',
      putanja: SifrarnikServis.kategorijeDozvola,
      opis: 'Oznake kategorija vozačkih dozvola',
      kolone: [
        KolonaSifrarnika(kljuc: 'oznaka', natpis: 'OZNAKA', sirina: 120),
        KolonaSifrarnika(kljuc: 'opis', natpis: 'OPIS'),
      ],
      polja: [
        PoljeSifrarnika(
          kljuc: 'oznaka',
          natpis: 'Oznaka',
          najmanje: 1,
          najvise: 10,
        ),
        PoljeSifrarnika(
          kljuc: 'opis',
          natpis: 'Opis',
          obavezno: false,
          najvise: 200,
        ),
      ],
    ),
    DefinicijaSifrarnika(
      naziv: 'Pravila kategorija',
      jednina: 'pravilo',
      putanja: SifrarnikServis.pravilaKategorija,
      opis: 'Šta koja kategorija smije voziti i od koliko godina',
      kolone: [
        KolonaSifrarnika(
          kljuc: 'kategorijaDozvoleOznaka',
          natpis: 'KATEGORIJA',
          sirina: 130,
        ),
        KolonaSifrarnika(kljuc: 'tipVozilaNaziv', natpis: 'TIP VOZILA'),
        KolonaSifrarnika(kljuc: 'maxKubikaza', natpis: 'MAX CCM', sirina: 120),
        KolonaSifrarnika(kljuc: 'maxSnagaKw', natpis: 'MAX KW', sirina: 120),
        KolonaSifrarnika(kljuc: 'minGodine', natpis: 'OD GODINA', sirina: 120),
      ],
      polja: [
        PoljeSifrarnika(
          kljuc: 'kategorijaDozvoleId',
          natpis: 'Kategorija dozvole',
          vrsta: VrstaPolja.veza,
          veza: SifrarnikServis.kategorijeDozvola,
          uPolaReda: true,
        ),
        PoljeSifrarnika(
          kljuc: 'tipVozilaId',
          natpis: 'Tip vozila',
          vrsta: VrstaPolja.veza,
          veza: SifrarnikServis.tipoviVozila,
          uPolaReda: true,
        ),
        PoljeSifrarnika(
          kljuc: 'maxKubikaza',
          natpis: 'Najveća kubikaža',
          vrsta: VrstaPolja.cijelBroj,
          obavezno: false,
          najmanjaVrijednost: 1,
          najvecaVrijednost: 3000,
          sufiks: 'ccm',
          uPolaReda: true,
          pojasnjenje: 'Prazno znači bez ograničenja.',
        ),
        PoljeSifrarnika(
          kljuc: 'maxSnagaKw',
          natpis: 'Najveća snaga',
          vrsta: VrstaPolja.decimalniBroj,
          obavezno: false,
          najmanjaVrijednost: 0.1,
          najvecaVrijednost: 300,
          sufiks: 'kW',
          uPolaReda: true,
        ),
        PoljeSifrarnika(
          kljuc: 'minGodine',
          natpis: 'Najmanje godina',
          vrsta: VrstaPolja.cijelBroj,
          najmanjaVrijednost: 14,
          najvecaVrijednost: 99,
        ),
      ],
    ),
    DefinicijaSifrarnika(
      naziv: 'Paketi osiguranja',
      jednina: 'paket',
      putanja: SifrarnikServis.paketiOsiguranja,
      opis: 'Osiguranje koje klijent bira uz rezervaciju',
      kolone: [
        KolonaSifrarnika(kljuc: 'naziv', natpis: 'NAZIV'),
        KolonaSifrarnika(kljuc: 'cijenaPoDanu', natpis: 'PO DANU', sirina: 140),
        KolonaSifrarnika(kljuc: 'iznosUcesca', natpis: 'UČEŠĆE', sirina: 140),
      ],
      polja: [
        PoljeSifrarnika(
          kljuc: 'naziv',
          natpis: 'Naziv',
          najmanje: 2,
          najvise: 100,
        ),
        PoljeSifrarnika(
          kljuc: 'cijenaPoDanu',
          natpis: 'Cijena po danu',
          vrsta: VrstaPolja.decimalniBroj,
          najmanjaVrijednost: 0,
          najvecaVrijednost: 10000,
          sufiks: '€',
          uPolaReda: true,
        ),
        PoljeSifrarnika(
          kljuc: 'iznosUcesca',
          natpis: 'Učešće u šteti',
          vrsta: VrstaPolja.decimalniBroj,
          najmanjaVrijednost: 0,
          najvecaVrijednost: 100000,
          sufiks: '€',
          uPolaReda: true,
          pojasnjenje: 'Koliko klijent plaća sam u slučaju štete.',
        ),
      ],
    ),
    DefinicijaSifrarnika(
      naziv: 'Vrste opreme',
      jednina: 'vrstu opreme',
      putanja: SifrarnikServis.vrsteOpreme,
      opis: 'Kaciga, GPS, kofer i ostalo što se iznajmljuje uz vozilo',
      kolone: [
        KolonaSifrarnika(kljuc: 'naziv', natpis: 'NAZIV'),
        KolonaSifrarnika(kljuc: 'cijenaPoDanu', natpis: 'PO DANU', sirina: 140),
        KolonaSifrarnika(
          kljuc: 'fiksnaCijena',
          natpis: 'JEDNOKRATNO',
          sirina: 150,
        ),
      ],
      polja: [
        PoljeSifrarnika(
          kljuc: 'naziv',
          natpis: 'Naziv',
          najmanje: 2,
          najvise: 100,
        ),
        PoljeSifrarnika(
          kljuc: 'cijenaPoDanu',
          natpis: 'Cijena po danu',
          vrsta: VrstaPolja.decimalniBroj,
          obavezno: false,
          najmanjaVrijednost: 0.01,
          najvecaVrijednost: 10000,
          sufiks: '€',
          uPolaReda: true,
        ),
        PoljeSifrarnika(
          kljuc: 'fiksnaCijena',
          natpis: 'Jednokratna cijena',
          vrsta: VrstaPolja.decimalniBroj,
          obavezno: false,
          najmanjaVrijednost: 0.01,
          najvecaVrijednost: 10000,
          sufiks: '€',
          uPolaReda: true,
          pojasnjenje: 'Popunjava se jedno od dva — po danu ili jednokratno.',
        ),
      ],
    ),
    DefinicijaSifrarnika(
      naziv: 'Razlozi otkazivanja',
      jednina: 'razlog',
      putanja: SifrarnikServis.razloziOtkazivanja,
      opis: 'Ponuđeni razlozi pri otkazivanju rezervacije',
      kolone: [
        KolonaSifrarnika(kljuc: 'naziv', natpis: 'NAZIV'),
        KolonaSifrarnika(
          kljuc: 'zaKlijenta',
          natpis: 'KLIJENT',
          sirina: 110,
          jeZastavica: true,
        ),
        KolonaSifrarnika(
          kljuc: 'zaAgenciju',
          natpis: 'AGENCIJA',
          sirina: 120,
          jeZastavica: true,
        ),
        KolonaSifrarnika(
          kljuc: 'traziNapomenu',
          natpis: 'TRAŽI NAPOMENU',
          sirina: 170,
          jeZastavica: true,
        ),
        KolonaSifrarnika(
          kljuc: 'aktivan',
          natpis: 'AKTIVAN',
          sirina: 110,
          jeZastavica: true,
        ),
      ],
      polja: [
        PoljeSifrarnika(
          kljuc: 'naziv',
          natpis: 'Naziv',
          najmanje: 3,
          najvise: 100,
        ),
        PoljeSifrarnika(
          kljuc: 'zaKlijenta',
          natpis: 'Nudi se klijentu',
          vrsta: VrstaPolja.prekidac,
        ),
        PoljeSifrarnika(
          kljuc: 'zaAgenciju',
          natpis: 'Nudi se osoblju',
          vrsta: VrstaPolja.prekidac,
        ),
        PoljeSifrarnika(
          kljuc: 'traziNapomenu',
          natpis: 'Traži napomenu uz razlog',
          vrsta: VrstaPolja.prekidac,
        ),
        PoljeSifrarnika(
          kljuc: 'aktivan',
          natpis: 'Aktivan',
          vrsta: VrstaPolja.prekidac,
          pojasnjenje: 'Neaktivan razlog se više ne nudi, a stare rezervacije ga zadrže.',
        ),
      ],
    ),
  ];
}
