# Roadmap

[English version](roadmap.en.md)

Grober Plan für Slipways. Einzelheiten zu offenen Entscheidungen und Restarbeiten stehen in [`offene-punkte.md`](offene-punkte.md), Datenquellen in [`datenquellen.md`](datenquellen.md). Stand: 31.10.2026.

## Erledigt

- **Neuaufbau** auf .NET 10 (Minimal APIs, SQLite) und React 19; Betrieb auf slipways.de mit HTTPS, automatischem Update von `master`, Backup und Überwachung.
- **Slipanlagen und Marinas aus OpenStreetMap** für 16 Länder: Deutschland, Österreich, Schweiz, Niederlande, Belgien, Frankreich, Spanien, Portugal, Italien, Kroatien, Dänemark, Schweden, Norwegen, Großbritannien, Irland und Polen. Jedes Land wird monatlich neu importiert.
- **Gewässer** aus PEGELONLINE und OpenStreetMap, mit Verläufen auf der Karte.
- **Pegel** für Deutschland (PEGELONLINE) und die Niederlande (Rijkswaterstaat).
- **Web:** Karte mit Länderblasen und Clustern, Länderauswahl, Suche, Hell/Dunkel, Seiten für Slipanlagen, Marinas und Gewässer.
- **Nutzerbeiträge ohne Anmeldung:** Korrekturen, Fakten, Tipps und neue Slipanlagen vorschlagen; Moderation mit Push-Benachrichtigung.
- **Apps für iOS und Android** mit denselben Funktionen wie das Web.
- **Suchmaschinen:** Kopfdaten je Seite, Sitemap, robots.txt.

## Als Nächstes

- **Abfrage nach Kartenausschnitt**, damit Web und Apps nicht mehr alle Einträge auf einmal laden; nötig, sobald die Listen nach den neuen Ländern zu groß werden.
- **Apps in die Stores:** Test auf dem Gerät, Icons, Signierung, Screenshots, Datenschutzangaben.
- **Pegel in weiteren Ländern**, zuerst dort, wo es offene Schnittstellen ohne Schlüssel gibt (z. B. Frankreich, England, Irland).
- **Aussagekräftigere Namen** für Einträge, die in OpenStreetMap keinen Namen haben.
- **Search Console** einrichten und Vorschaubild für Link-Vorschauen.

## Später

- **Fotos** zu Slipanlagen und Marinas, über die Moderation.
- **Bestätigungen zählen** („3× bestätigt“) und Vorschläge vor der Freigabe bearbeiten.
- **Ruhigerer Kartenstil** (Vektorkacheln).
- **Weitere Länder**, z. B. Finnland und Griechenland.

## Ideen

- **„Nutzer sagen …“:** Schlagworte oder Wortwolke statt Sternebewertung auf den Detailseiten.
