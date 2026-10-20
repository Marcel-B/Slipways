# Nutzerbeiträge ohne Registrierung

Konzept vom 20.10.2026, mit Marcels Entscheidungen. Umgesetzt sind Stufe 1 und 2 (Web und Backend); wie es technisch läuft, steht im README („Nutzerbeiträge“), was fehlt, in [`offene-punkte.md`](offene-punkte.md). Abweichung vom Konzept: Statt der Altcha-Bibliothek gibt es eine kleine eigene Umsetzung im selben Format (`ProofOfWork.cs`, `web/src/contribute/proof.ts`), weil das Altcha-Widget Worker aus `blob:`-Adressen startet, die unsere CSP nicht erlaubt.

Frage: Wie können Nutzer in Web und Apps Korrekturen, neue Slipanlagen, Hinweise und später Fotos beitragen, ohne Konto, aber geschützt vor Missbrauch?

## Kurzfassung (Empfehlung)

1. **Vorschlag statt Direktänderung, mit Moderations-Queue.** Jeder Beitrag landet als `Suggestion` in einer eigenen Tabelle und wird erst nach Freigabe durch Marcel sichtbar. Das ist der eigentliche Schutz, alles andere hält nur die Queue sauber.
2. **Vor der Queue drei billige Filter, ohne Drittanbieter:** Proof-of-Work-Captcha mit **Altcha** (Open Source, selbst gehostet, keine Cookies, keine Anfrage an fremde Server), ein eigenes, strenges Rate-Limit für schreibende Endpunkte (nur im Arbeitsspeicher, wie heute) und inhaltliche Plausibilitätsprüfungen (Koordinaten im Abdeckungsgebiet, Dublettensuche, Längen, keine Links).
3. **Ergänzungen zu OSM-Einträgen getrennt speichern.** Freigegebene Angaben zu `osm`-Einträgen landen in der Zusatzinfo-Tabelle (aus „Marina-Infos automatisch anreichern“), nicht in den Spalten des Eintrags. Der Import fasst sie nicht an, danach wird abgeglichen, ob OSM nachgezogen hat. Name und Lage korrigiert der Nutzer weiter direkt in OSM (Link auf der Detailseite). Die Ergänzungen zu OSM-Werten sind eine abgeleitete Datenbank und müssen unter ODbL angeboten werden.
4. **Ohne E-Mail, ohne App-Attest/Play Integrity; Fotos als eigene Stufe nach den Textbeiträgen.** E-Mail optional erst, wenn jemand über die Freigabe benachrichtigt werden will. Fotos in einer späteren Stufe, weil sie rechtlich (Urheberrecht, Personen, Kennzeichen, EXIF-Ortsdaten) und beim Moderieren am teuersten sind.

## Was das Repo heute mitbringt

- `Slipway`/`Marina` haben `Source` (`manual` oder `osm`), `OsmType`, `OsmId`, `Created`, `Updated`.
- Der OSM-Import (`Osm/OsmImport.cs`) aktualisiert nur Einträge mit `Source = osm`. Eigene Einträge (`manual`) verknüpft er höchstens mit einem nahen OSM-Element (`SamePlaceDistance`), überschreibt sie aber nie. Freigegebene neue Slipanlagen als `manual` sind also importfest. Korrekturen an `osm`-Einträgen wären es nicht.
- Rate-Limit pro Client-Adresse (`RateLimiting.cs`, Token-Bucket, großzügig wegen geteilter Mobilfunk-Adressen), Adresse nur im Speicher. Security-Header und CSP aus PR #93.
- Konto-Endpunkte aus (`Accounts:Enabled=false`), Identity-Tabellen sind noch da.
- Die Datenschutzseite verspricht „keine Zugriffslogs“. Jede neue Verarbeitung muss dort rein.

## Ansätze im Vergleich

| Ansatz | Schutz gegen | Aufwand | Datenschutz | Web | Apps | Urteil |
|---|---|---|---|---|---|---|
| **Vorschlag statt Direktänderung** | Vandalismus, Falschangaben, Spam wird nie öffentlich | mittel (Tabelle, Diff, Freigabe) | nur Beitragsinhalt | ja | ja | **Pflicht** |
| **Moderations-Queue** | alles, was die Filter durchlassen | Marcels Zeit pro Beitrag | keine zusätzliche | – | – | **Pflicht**, Aufwand mit Filtern klein halten |
| **Altcha (Proof-of-Work, selbst gehostet)** | Massen-Spam durch Bots, macht jeden Beitrag rechnerisch teuer | klein (Web-Komponente, .NET-Prüfung, nativ in Apps nachbaubar) | keine Cookies, kein Drittanbieter, CSP bleibt `self` | ja | ja (SHA-256 in CryptoKit bzw. `MessageDigest`) | **empfohlen** |
| Friendly Captcha | wie Altcha, zusätzlich Risikobewertung | klein | Drittanbieter (EU), muss in Datenschutz und CSP; für gewerbliche Seiten kostenpflichtig | ja | nur per WebView | nicht nötig |
| hCaptcha/reCAPTCHA/Turnstile | Bots | klein | Drittanbieter außerhalb bzw. US-Bezug, Einwilligungsthema | ja | umständlich | nein |
| Reines Proof-of-Work ohne Captcha-Widget | Bots | klein | wie Altcha | ja | ja | Altcha *ist* das, nur fertig |
| Honeypot-Feld + Mindestausfüllzeit | einfache Bots | sehr klein | keine | ja | entfällt | **zusätzlich**, kostet nichts |
| Eigenes Rate-Limit für POST | Fluten von einer Adresse | sehr klein (zweite Policy in `RateLimiting.cs`) | wie heute, nur RAM | ja | ja | **empfohlen**, z. B. 5 Beiträge/Stunde und 20/Tag pro Adresse |
| E-Mail-Bestätigung (Double-Opt-in) | Wegwerf-Spam, gibt eine Rückkanal-Adresse | mittel bis groß (SMTP-Versand, Zustellbarkeit, Token, Löschfristen) | E-Mail ist personenbezogen, Speicherung und Löschung regeln | ja | ja | **nicht als Pflicht**; Wegwerfadressen umgehen es, und es schreckt ab |
| App Attest (iOS) / Play Integrity (Android) | Beiträge nur aus echten App-Installationen | groß (Schlüssel, Server-Prüfung, Google-API-Aufruf je Beitrag) | Play Integrity schickt Daten an Google | nein | ja | **nicht jetzt**; schützt nur die Apps, das Web bleibt offen |
| Vertrauensstufen ohne Konto (Geräte-Token) | wiederholte gute/schlechte Beitragende erkennen | mittel | pseudonymer Token, muss erklärt werden | ja | ja | später, falls die Queue zu voll wird |

## Empfohlener Aufbau

### Datenmodell

Eine Tabelle `Suggestion` (datenbankneutral, JSON als Text):

- `Id`, `Created`, `Status` (`pending`, `accepted`, `rejected`), `Decided`
- `Kind`: `new-slipway`, `new-marina`, `edit`, `note` (Hinweis ohne konkrete Feldänderung, z. B. „Slip ist gesperrt“), später `photo`
- `TargetType` + `TargetId` (bei `edit`/`note`), Ziel kann `manual` oder `osm` sein
- `Payload`: die vorgeschlagenen Felder als JSON; bei `edit` nur geänderte Felder, plus Stand beim Einreichen (`BaseUpdated`), damit die Freigabe Konflikte erkennt
- `Comment` des Einreichers (keine Namen oder Kürzel; öffentlich steht nur „von Nutzern ergänzt, Stand …“)
- `StatusToken` (Zufallswert, nur als Hash gespeichert): der Einreicher bekommt einen Link, unter dem er den Stand sehen und den Vorschlag zurückziehen kann. Ersetzt das Konto für diesen Zweck.

Keine IP-Adresse, keine Geräte-ID, kein User-Agent in der Tabelle.

### Ablauf

1. Client holt `GET /api/suggestions/challenge` (Altcha-Challenge, HMAC-signiert, kurz gültig).
2. Client löst sie (im Browser per Web Worker, in den Apps nativ; dauert je nach Schwierigkeit 0,5 bis 2 s).
3. `POST /api/suggestions` mit Lösung, Honeypot und Inhalt. Der Server prüft der Reihe nach: Rate-Limit, Altcha-Lösung (und dass sie noch nicht benutzt wurde, kurze In-Memory-Sperrliste), Honeypot, Plausibilität. Antwort `202 Accepted` mit Status-Link.
4. Plausibilität: Koordinaten in den abgedeckten Ländern; bei neuen Slipanlagen Dublettensuche im Umkreis von `SamePlaceDistance` (eigene **und** OSM-Einträge), Treffer werden dem Nutzer vor dem Absenden gezeigt („Meinst du diese?“); Feldlängen, keine URLs in Freitext (außer im Feld `Url`), keine Steuerzeichen.
5. Moderation: Marcel sieht die Queue mit Diff (alt/neu, Karte), gibt frei oder lehnt ab. Freigabe wendet `Payload` an: neue Einträge mit `Source = manual`, Änderungen nur auf `manual`-Einträge oder auf nicht-OSM-Felder.
6. Abgelehnte Vorschläge nach 30 Tagen löschen, angenommene nach 90 Tagen auf das Nötigste kürzen (Inhalt steckt dann im Eintrag selbst).

### Moderationsoberfläche ohne öffentliches Konto

Drei Möglichkeiten, ohne die Konto-Endpunkte wieder einzuschalten:

- **Nur im Tailnet (Marcels Wahl).** Die Moderation lauscht auf einem eigenen Port nur auf `127.0.0.1` des Servers; `tailscale serve` reicht ihn mit HTTPS ins Tailnet weiter, Adresse z. B. `https://slipways-1.<tailnet>.ts.net/moderation`. Über slipways.de ist sie nicht erreichbar, auch nicht aus dem Tailnet (die Domain zeigt auf die öffentliche IP). Kein Passwort im Internet, passt zum bestehenden Betrieb.
- Admin-Schlüssel per Header (einfach, aber ein Geheimnis, das im Browser liegt).
- Identity nur für eine Admin-Rolle wieder anschalten (größter Aufwand, öffnet wieder Login-Angriffsfläche).

Benachrichtigung über neue Vorschläge z. B. per Messwert `slipways_suggestions_pending` (ohne Labels mit Inhalten) und einen Grafana-Alarm, sobald Alarme existieren (siehe `docs/offene-punkte.md`, Überwachung Punkt 3).

### Umgang mit OpenStreetMap

| Beitrag zu … | Wohin | Warum |
|---|---|---|
| Neue Slipanlage, die es in OSM nicht gibt | zu uns (`manual`) | eigene Daten; Import überschreibt `manual` nie. Optional Hinweis „Gern auch in OSM eintragen“ |
| Fehlende oder falsche Angaben (Gebühr, Öffnungszeiten, Telefon, Zugang) zu einem `osm`-Eintrag | zu uns, **getrennt** in der Zusatzinfo-Tabelle (siehe unten), dazu weiter der Link nach OSM | der Import fasst die Tabelle nicht an; nach jedem Import wird abgeglichen |
| Name oder Lage eines `osm`-Eintrags falsch | **zu OSM** (Bearbeiten-Link oder OSM-Hinweis) | Grunddaten des Eintrags; eine eigene Fassung davon würde zwei Wahrheiten erzeugen |
| Erfahrungen, Kosten, Pro/Contra, Bewertung, „Slip im Winter gesperrt“ | zu uns, als eigene Daten neben dem OSM-Eintrag | keine OSM-Werte, getrennt gehalten und nur per OSM-ID verknüpft (im Sinne der OSMF-Richtlinie zu „Collective Databases“) |
| Eintrag existiert nicht mehr | als `note` zu uns **und** Link zu OSM | wir können ihn ausblenden, gelöscht wird er in OSM |

### Eigene Ergänzungen zu OSM-Einträgen (Marcels Wunsch, 20.10.2026)

Freigegebene Ergänzungen landen nicht in den Spalten von `Slipway`/`Marina`, sondern in der Zusatzinfo-Tabelle, die der Thread „Marina-Infos automatisch anreichern“ gerade baut (Quelle, Quell-URL, Abrufdatum; dort kommen auch Werte von Homepages hin). Kein zweites Modell. Für Nutzerbeiträge braucht die Tabelle:

- Quelle `user` neben z. B. `website`
- eine Zeile pro Feld (`fee`, `opening_hours`, `phone`, `access`, …)
- Verknüpfung über die Entity-Id und zusätzlich `OsmType`/`OsmId`, falls der Import einen Eintrag neu anlegt
- `BaseValue`: der OSM-Wert zum Zeitpunkt der Ergänzung (leer, wenn OSM nichts hatte)

Umgesetzt im anderen Thread (Branch `claude/project-thread-7lnre7`, Migration `WebsiteInfos`): Tabelle `PlaceInfos`, Entity `PlaceInfo` mit `PlaceType` (`slipway`/`marina`), `PlaceId` (bewusst kein FK), `OsmType`/`OsmId`, `Field` (`phone`, `email`, `opening_hours`, offen für `fee` usw.), `Value`, `BaseValue`, `Source` (offen; `website`, für Nutzerbeiträge `user` ergänzen), `SourceUrl`, `Retrieved`. Der Website-Job fasst nur `Source = website` an. Die API liefert die Zeilen als `PlaceInfoDto` in `MarinaDetailsDto.Infos`.

**Abgleich nach jedem OSM-Import** (als Schritt am Ende des Imports):

| OSM jetzt | eigener Wert | Ergebnis |
|---|---|---|
| gleich dem eigenen Wert | – | OSM hat nachgezogen; eigene Zeile löschen bzw. als erledigt markieren |
| unverändert (= `BaseValue`) | weicht ab | eigener Wert bleibt, Anzeige bevorzugt ihn mit „laut Nutzern, Stand …“ |
| geändert, aber anders als der eigene Wert | weicht ab | Konflikt; eigene Zeile in die Moderations-Queue zur Prüfung, bis dahin OSM-Wert zeigen |
| Eintrag aus OSM verschwunden | – | Zeilen bleiben über `OsmType`/`OsmId` erhalten, werden aber nicht angezeigt |

Lizenz: Eigene Korrekturen an OSM-Werten zusammen mit dem OSM-Eintrag gezeigt sind nach ODbL eine abgeleitete Datenbank. Das ist erlaubt, verlangt aber, dass diese Korrekturen unter ODbL angeboten werden (Share-Alike), z. B. als Download der Zeilen mit Quelle `user` zu `osm`-Einträgen. Deshalb bleiben sie strikt getrennt von den eigenen `manual`-Daten. Marcel nimmt das in Kauf (20.10.2026): lieber bei uns erfassen als Nutzer zu OSM schicken. Umsetzung: beim Absenden einer Ergänzung zu einem `osm`-Eintrag der Hinweis, dass sie unter ODbL veröffentlicht wird; ein Download (z. B. `/api/place-infos/odbl` als CSV/JSON der Zeilen mit `Source = user` zu `osm`-Einträgen, ohne interne Felder) und ein Satz im README-Abschnitt „Datenquellen und Lizenzen“. Der OSM-Link bleibt als Zusatz, nicht als Hauptweg.

Wir senden selbst nichts an OSM. Ein automatischer Versand von Nutzertexten an die OSM-Notes-API wäre eine weitere Datenübermittlung und bräuchte Lizenzzusagen der Nutzer. Der Link lässt den Nutzer direkt bei OSM schreiben, unter deren Bedingungen.

Hinweis: Das ist eine fachliche Einschätzung, keine Rechtsberatung. Für die Abgrenzung „abgeleitet“ gegen „gesammelt“ sind die Guidelines der OSMF-Lizenzarbeitsgruppe maßgeblich.

### Rechte an den Beiträgen

Neben dem Absende-Knopf ein Satz plus Haken: Der Beitrag stammt aus eigener Kenntnis (nicht aus anderen Verzeichnissen wie slipanlage.info abgeschrieben, siehe `docs/datenquellen.md`), und der Nutzer räumt Slipways ein einfaches, unbefristetes Nutzungsrecht ein. Das gehört als kurzer Abschnitt auf eine Seite „Mitmachen“ oder in die Nutzungshinweise. Wegen der PolyForm-Strict-Lizenz des Codes betrifft das nur die Daten.

### Datenschutz (Ergänzung für `/datenschutz`)

- Was: Inhalt des Vorschlags, bei Fotos das neu kodierte Bild, Zeitpunkt, gehashter Status-Token. Keine Namen.
- Nicht: IP-Adresse wird nur kurzzeitig im Arbeitsspeicher für das Rate-Limit genutzt und nie gespeichert; Altcha setzt keine Cookies und ruft keinen Drittanbieter.
- Rechtsgrundlage: Art. 6 Abs. 1 lit. f DSGVO (Pflege des Verzeichnisses, Missbrauchsschutz).
- Löschfristen wie oben, Löschung auf Wunsch über den Status-Link.
- Bei Fotos zusätzlich: EXIF entfernen, Hinweis auf Personen/Kennzeichen.

### Fotos (Stufe 4, von Marcel gewünscht)

- Nur über die Queue, nicht öffentlich vor Freigabe.
- Serverseitig neu kodieren (WebP/JPEG, max. z. B. 2000 px), EXIF und GPS entfernen, Größenlimit (z. B. 8 MB Upload), nur JPEG/PNG/HEIC annehmen.
- Lizenzfrage beim Upload: Nutzungsrecht für Slipways, Bestätigung „selbst fotografiert“.
- Speicher: Dateien im Volume neben der SQLite-Datenbank, ins Nextcloud-Backup aufnehmen; Plattenplatz auf dem CAX11 (40 GB) im Auge behalten.
- Strengere Rate-Limits und höhere Altcha-Schwierigkeit als für Text.

## Oberfläche (Vorschlag)

**Detailseite**
- Jedes vorhandene Feld bekommt einen kleinen Stift (Desktop beim Überfahren, auf dem Handy immer sichtbar, aber dezent). Ein Tipp öffnet ein kleines Formular nur für dieses Feld, auf dem Handy als Bottom-Sheet.
- Fehlende Felder nur aus einer kurzen, festen Liste zeigen (Slipanlage: Gebühr, Öffnungszeiten, Untergrund, Zugang; Marina zusätzlich Telefon), gesammelt in einem Kasten „Fehlt noch: Gebühr, Öffnungszeiten. Weißt du mehr?“ mit je einem „Ergänzen“. So wirkt die Seite nicht wie ein Lückentext.
- Passende Eingaben je Feld: Auswahl statt Freitext, wo es geht (Gebühr ja/nein + Betrag, Untergrund aus Liste, Zugang öffentlich/Kunden/privat), Öffnungszeiten mit Beispiel. Dazu optional „Woher weißt du das?“ und der Lizenzhaken (bei `osm`-Einträgen mit ODbL-Hinweis).
- Altcha rechnet schon beim Öffnen des Formulars im Hintergrund, sodass beim Absenden nichts mehr zu warten ist. Sichtbar ist nur ein kleines „Schutz vor Spam ✓“.
- Nach dem Absenden: „Danke, wird geprüft“ und der Status-Link. Im selben Browser zeigt das Feld „Vorschlag in Prüfung“ (nur `localStorage`, nicht serverseitig).
- Unten auf der Seite „Etwas stimmt nicht“ für freie Hinweise (z. B. gesperrt, existiert nicht mehr).
- Von Nutzern stammende Werte tragen ein kleines Etikett „von Nutzern ergänzt, Stand 10/2026“, Werte von Homepages „laut Homepage, Stand …“.

**Erfahrungen statt Pro/Contra** (Vorschlag)
- Kein freies Pro/Contra und keine Sterne: subjektiv, ohne Konto leicht zu manipulieren, nicht überprüfbar, und Aussagen über Betreiber bergen Haftungsrisiken.
- Stattdessen Fakten zum Ankreuzen, gesammelt in „Was gibt es dort?“: Parkplatz für Gespann, Steg, Toilette, Kran, Rampe steil, rutschig bei Niedrigwasser, nur mit Schlüssel. Technisch die vorhandenen `Extras` (heute z. B. Camping, Parken) bzw. Zeilen in `PlaceInfos`. Mehrere gleiche Meldungen zählen als Bestätigung („3× bestätigt, zuletzt 08/2026“).
- Optional ein kurzer „Tipp“ (max. 200 Zeichen, datiert). Moderiert wird nur Ton und Inhalt (keine Werbung, keine Beleidigung), nicht der Wahrheitsgehalt.
- Die alten `Pro`/`Contra`/`Rating`-Werte der eigenen Einträge (2019/20) können als Tipps übernommen oder ausgeblendet werden.

**Neue Slipanlage**
- Knopf „Slipanlage fehlt?“ über der Karte. Danach erscheint ein verschiebbarer Pin in der Kartenmitte, darunter ein kurzes Formular (Name, Gebühr, Untergrund, Kommentar). Das Gewässer wird aus der Lage vorgeschlagen.
- Liegt ein bekannter Eintrag in der Nähe, zeigt das Formular ihn zuerst: „Ist es diese hier?“ mit Sprung zu dessen Detailseite.

**Fotos**
- In der Bildleiste der Detailseite eine letzte Kachel „Foto hinzufügen“. Verkleinert wird schon im Browser bzw. in der App vor dem Hochladen (spart Datenvolumen), der Server kodiert trotzdem selbst neu.

**Apps** übernehmen dasselbe Muster mit nativen Sheets.

## Stufenplan

1. **Hinweise und Links (klein).** „In OpenStreetMap bearbeiten“ und „Hinweis an OSM“ auf Detailseiten von `osm`-Einträgen in Web und Apps. Kein Backend nötig.
2. **Vorschläge für Text (mittel).** `Suggestion`-Tabelle, Altcha, POST-Rate-Limit, Honeypot, Plausibilität, Formular im Web, Moderation im Tailnet, Datenschutz- und „Mitmachen“-Seite. Neue Slipanlagen, Korrekturen an `manual`-Einträgen, Ergänzungen zu `osm`-Einträgen in der Zusatzinfo-Tabelle samt Abgleich nach dem Import, Notizen zu allen Einträgen.
3. **Apps (mittel).** Dieselben Endpunkte, Altcha nativ gelöst; DTOs in `Contracts.cs` und Modelle der Apps mitziehen.
4. **Fotos (groß).** Erst wenn Stufe 2 läuft und die Queue überschaubar bleibt.
5. **Falls Spam trotzdem durchkommt:** Altcha-Schwierigkeit hochsetzen, Geräte-Token mit Vertrauensstufe, als letztes App Attest/Play Integrity für die Apps.

## Entscheidungen (Marcel, 20.10.2026)

1. **Moderation nur im Tailnet.**
2. **Ergänzungen zu OSM-Einträgen getrennt speichern** und nach dem Import abgleichen (Abschnitt „Eigene Ergänzungen zu OSM-Einträgen“), in der Zusatzinfo-Tabelle aus „Marina-Infos automatisch anreichern“.
3. **Keine Kürzel.** Öffentlich steht nur „von Nutzern ergänzt, Stand …“.
4. **Fotos kommen**, mit starker Kompression. Rechnung: lange Kante 1600 px als WebP (Qualität ~75) ≈ 150–300 KB, Vorschaubild 400 px ≈ 20–30 KB, Originale nicht aufheben. 10.000 Fotos ≈ 2–3 GB, also auf dem CAX11 (40 GB) machbar. Dazu höchstens z. B. 5 Fotos je Eintrag, abgelehnte sofort löschen. Reicht der Platz später nicht, Hetzner Object Storage (S3-kompatibel) oder ein Volume; beides müsste dann ins Backup und in die Datenschutzseite.
