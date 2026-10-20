# Offene Datenquellen für Slipways (Stand 20.10.2026)

## Woher die Seed-Daten vermutlich stammen

| Datei | Anzahl | Vermutete Herkunft |
|---|---|---|
| `stations.json` | 674 | **PEGELONLINE (WSV)**: Felder `Number`, `Shortname` in Großbuchstaben, `Km`, `Agency` ("WSA WILHELMSHAVEN") entsprechen 1:1 der PEGELONLINE-API |
| `waters.json` | 109 | ebenfalls PEGELONLINE (Gewässerliste der Pegel, z.B. "OSTE") |
| `slipways.json` | 106 | händisch erfasst (Kommentare wie "Parkplatz (Google Maps)"), Stand 2019/2020 |
| `marinas.json`, `services.json` | 21 / 3 | händisch erfasst (Adressen, Telefon, Webseiten) |

Abgeleitet aus den Feldnamen und Inhalten der Dateien in `backend/src/Slipways.Infrastructure/Seed/`; eine Quellenangabe gibt es im Repo nicht. Die Dateien kamen mit dem ersten Commit (14.04.2020).

## Slipanlagen

**OpenStreetMap, `leisure=slipway`** ist die einzige große offene Quelle.
- Weltweit rund 47.000 Objekte (Diary-Auswertung Jan. 2026: ca. 28.000 nur Knoten, 16.800 nur Wege, 2.800 beides). Overpass-Zählung vom 20.10.2026 (`nwr`, Knoten/Wege/Relationen): DE 3.342 (2.589/750/3), AT 391, CH 447, NL 916; Marinas DE 2.727, AT 199, CH 446, NL 1.224.
- Nützliche Zusatz-Tags: `name`, `fee`, `opening_hours`, `access`, `surface`, `trailer`, `boat`, `operator`, `website`. Häufig ist nur der Punkt ohne weitere Attribute erfasst.
- Lizenz: **ODbL 1.0**. Kommerzielle Nutzung ist erlaubt, Pflicht ist die Namensnennung "© OpenStreetMap-Mitwirkende". Eine *Datenbank*, die OSM-Daten mit eigenen Daten vermischt, muss bei Weitergabe ebenfalls unter ODbL stehen (Share-Alike). Für die App heißt das: OSM-Slips getrennt halten (eigene Tabelle oder `Source`-Spalte mit OSM-ID) oder akzeptieren, dass die Slip-Datenbank als Ganzes ODbL wird.
- Zugriff:
  - Overpass API (live, gut für Updates), z.B. `area["ISO3166-1"="DE"][admin_level=2]->.a; nwr[leisure=slipway](area.a); out center tags;`
  - Geofabrik-Extrakte (`.osm.pbf` pro Land, täglich) plus `osmium tags-filter` für einen regelmäßigen Import-Job.
- Bonus: Nutzer der App könnten Fehler direkt in OSM korrigieren, das verbessert die Quelle für alle.

**Weitere Quellen ohne offene Lizenz** (nur als Hinweis, nicht übernehmen):
- ELWIS / WSV veröffentlicht keine Slip-Liste; die Inland-ENC-Karten (kostenlos) enthalten Häfen, aber keine verlässlichen Slipanlagen.
- Wassersportführer, Verbands- und Tourismusportale (z.B. Landestourismus, Kanu-/Motorbootverbände) haben Listen, aber unter Urheberrecht. Eine Kooperation wäre möglich, ein Scraping nicht.

## Gewässer

- **OSM** `waterway=river|canal`, `natural=water` mit `name` (ODbL). Gut für Geometrie und Namen.
- **WSV/BfG**: Bundeswasserstraßen-Netz und Kilometrierung als Geodaten (GDWS-Geodienste, DL-DE). Passt zu den Pegel-Kilometern.
- **EU-Hydro / Copernicus** (europaweites Gewässernetz, frei nutzbar mit Quellenangabe) für Europa.
- Die bisherigen `waters.json` sind faktisch die Gewässerliste von PEGELONLINE; die liefert die API ohnehin mit.

## Marinas / Häfen

- **OSM** `leisure=marina`, dazu OpenSeaMap-Tags (`seamark:type=harbour`, `seamark:harbour:category=marina`). Lizenz ODbL wie oben.
- Kontaktdaten (Telefon, E-Mail) sind in OSM oft lückenhaft; die händisch erfassten Daten bleiben wertvoll.

## Pegel / Wasserstände

| Land | Quelle | Zugriff | Lizenz |
|---|---|---|---|
| DE (Bundeswasserstraßen) | **PEGELONLINE (WSV)** | REST-API `https://www.pegelonline.wsv.de/webservices/rest-api/v2/stations.json`, Messwerte pro Station, ohne Registrierung | **DL-DE Zero 2.0** (auf pegelonline.wsv.de/webservice/ueberblick bestätigt), keine Pflichten. Rohdaten ungeprüft, nur letzte ~30 Tage. Stand 20.10.2026: 786 Pegel an 102 Gewässern (Seed: 674 / 109, veraltet) |
| DE (Landesgewässer) | Hochwasserportale der Länder, z.B. NLWKN-PEGELONLINE, LHP | je Land unterschiedlich, teils API | meist DL-DE BY 2.0 (Namensnennung) |
| AT | **eHYD** (BML + Länder) | WFS/GeoPackage-Download aktueller Pegelstände | **CC BY 4.0**, Quellenhinweis "ehyd.gv.at" |
| AT (Donau) | viadonau | Webportal / Pegelhub | prüfen |
| CH | **BAFU Hydrodaten** | LINDAS (Linked Data/SPARQL, 10-min-Werte); inoffiziell hydroweb.xml bzw. api.existenz.ch | BAFU-Bedingungen, Quellenangabe BAFU Pflicht |
| Europa | keine zentrale offene Pegel-API. EFAS/Copernicus nur für Partner. Nationale Dienste z.B. UK Environment Agency (OGL), NL Rijkswaterstaat (Waterinfo), FR Hub'Eau/Vigicrues (Etalab) | je Land | meist offen mit Namensnennung |

## Empfehlung

1. Slipanlagen und Marinas aus OSM importieren und in einer getrennten Quelle mit OSM-ID halten, damit die ODbL-Pflichten sauber bleiben. Eigene, von Nutzern erfasste Bewertungen/Kommentare als separate Daten daneben. **Umgesetzt** über Overpass statt Geofabrik (`OsmImportWorker`, alle 30 Tage, DE/AT/CH/NL, `Source`-Spalte mit OSM-Element; siehe README).
2. Pegel und Gewässer per Backend-Job aus PEGELONLINE holen (DE, **umgesetzt in #57**: `GaugeSyncWorker`, alle 15 Minuten), danach eHYD (AT) und BAFU (CH) ergänzen.
3. Attribution in Web und App einbauen (Web-Seitenfuß und README-Abschnitt seit #57) ("© OpenStreetMap-Mitwirkende", "PEGELONLINE/WSV", "ehyd.gv.at", "BAFU").

## Quellen

- OSM-Wiki [Tag:leisure=slipway](https://wiki.openstreetmap.org/wiki/Tag:leisure=slipway), [DE](https://wiki.openstreetmap.org/wiki/DE:Tag:leisure%3Dslipway)
- OSM-Zählung Jan. 2026: [Diary-Kommentare](https://api.openstreetmap.org/user/scarapella/diary_comments)
- [OpenSeaMap/Harbours](https://wiki.openstreetmap.org/wiki/OpenSeaMap/Harbours), [DE:OpenSeaMap (Lizenz)](https://wiki.openstreetmap.org/wiki/DE:OpenSeaMap)
- PEGELONLINE-Lizenz: [Open-Data-Katalog Hessen](https://opendata.hessen.de/dataset/pegelonline/resource/d44fbba8-dcab-44c5-b7b9-be21c32aea41/view/baee3201-86ff-4f4d-a74d-5dc5f9c35331), [bundesAPI/pegel-online-api](https://github.com/bundesAPI/pegel-online-api)
- eHYD CC BY 4.0: [INSPIRE-Metadaten](https://inspire-geoportal.ec.europa.eu/srv/api/records/6550800f-b5cd-4bc8-8525-9069a950e191)
- BAFU: [Aktuelle hydrologische Daten beziehen](https://www.bafu.admin.ch/de/aktuelle-hydrologische-daten-beziehen), [parse-hydrodaten](https://github.com/cstuder/parse-hydrodaten)
- ELWIS: [Inland-ENC](https://elwis.de/DE/dynamisch/IENC)
- EFAS: [Hydrological data collection FAQ](https://european-flood.emergency.copernicus.eu/sites/default/files/2023-09/FAQ%20CEMS%20EFAS%20hydrological%20data%20collection_September2023.pdf)

Hinweis: PEGELONLINE-Lizenz direkt beim Anbieter bestätigt. eHYD und BAFU stammen aus Katalog-Einträgen (die Seiten sind JavaScript-Apps und ließen sich nicht auslesen). Die Zählung über Overpass ist inzwischen nachgeholt (siehe oben).

## Nachtrag: slipanlage.info (geprüft 20.10.2026)

- Betreiber: Allactive Medien GmbH, Ratingen (Impressum), werbefinanziert (AdSense/Amazon).
- Technik: keine öffentliche API und kein Export. Die Karte lädt intern `getObjects.php?do=all` (JSON mit ca. 1.819 Markern, nur `id`, `lat`, `lng`), Details per `do=one&id=…`, dazu Listen nach Stadt, Fluss und Hafen. Technisch wäre das leicht auslesbar.
- Recht: keine Lizenzangabe, keine offenen Daten. Laut Impressum liegen die Urheberrechte bei den Autoren bzw. Quellen. Eine systematische Sammlung von ~1.800 Slipstellen fällt in Deutschland unter das Datenbankherstellerrecht (§ 87a ff. UrhG). Wesentliche Teile zu übernehmen oder wiederholt systematisch auszulesen ist ohne Zustimmung unzulässig, auch wenn einzelne Fakten (Koordinaten) selbst nicht geschützt sind.
- Empfehlung: nicht scrapen. Wenn gewünscht, den Betreiber (office@slipanlage.info) um eine Nutzungserlaubnis oder Kooperation bitten. Ansonsten OSM als Datenbasis.

## Nachtrag: weitere mögliche Quellen für Slipanlagen (20.10.2026)

- **Niederlande, Rijkswaterstaat „Trailerhellingen“** (FIS/VNDS, auch auf data.overheid.nl, Stand 2026): amtlicher Datensatz der Slipanlagen an Reichswasserstraßen. RWS veröffentlicht nach „open, tenzij“, Weiterverwendung meist ohne Namensnennung. Konkrete Lizenz noch prüfen. https://nationaalgeoregister.nl/geonetwork/srv/metadata/8c5a7ca3-4c67-40af-82b6-a4f52dfac496
- **Kommunale Open Data**, vereinzelt, z.B. Gelsenkirchen „Infrastrukturdaten Bootsanleger (RVR)“ unter DL-DE Zero (CSV/KML). Lückenhaft, eher Anleger als Slips. Weitere per GovData-Suche nach Stadt/Kreis.
- **WSV WInD** (Wasserstraßen-Infrastruktur-Daten, WFS/WMS): beschreibt Objekte an Bundeswasserstraßen. Ob Slipanlagen enthalten sind, ist unklar, da hilft nur ein Blick in GetCapabilities.
- **Tourismus-Datenbanken der Länder** (z.B. Reiseland Brandenburg, Sportboothäfen/Wasserwanderrastplätze): teils als Open Data angeboten, Lizenz je Land prüfen (nicht verifiziert).
- **Verbände/Kooperation** (DMYV, ADAC-Marinaführer, Kanu-Landesverbände): nur mit Erlaubnis.
- **Crowd**: Nutzer erfassen in der App; Korrekturen idealerweise auch in OSM zurückspielen.
