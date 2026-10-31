# Roadmap

[Deutsche Fassung](roadmap.md)

Rough plan for Slipways. Open decisions and remaining work are tracked in more detail in [`offene-punkte.md`](offene-punkte.md) (German), data sources in [`datenquellen.md`](datenquellen.md) (German). As of 31 October 2026.

## Done

- **Rebuild** on .NET 10 (Minimal APIs, SQLite) and React 19; running at slipways.de with HTTPS, automatic updates from `master`, backups and monitoring.
- **Slipways and marinas from OpenStreetMap** for 16 countries: Germany, Austria, Switzerland, the Netherlands, Belgium, France, Spain, Portugal, Italy, Croatia, Denmark, Sweden, Norway, the United Kingdom, Ireland and Poland. Each country is imported again every month.
- **Waters** from PEGELONLINE and OpenStreetMap, with their courses on the map.
- **Gauges** for Germany (PEGELONLINE) and the Netherlands (Rijkswaterstaat).
- **Web:** map with country bubbles and clusters, country selection, search, light/dark mode, pages for slipways, marinas and waters.
- **Contributions without an account:** suggest corrections, facts, tips and new slipways; moderation with push notifications.
- **iOS and Android apps** with the same features as the website.
- **Search engines:** per-page metadata, sitemap, robots.txt.

## Next

- **Query by map area**, so the website and apps no longer load every entry at once; needed as soon as the lists grow too large with the new countries.
- **Apps in the stores:** on-device testing, icons, signing, screenshots, privacy details.
- **Gauges in more countries**, starting where open APIs without a key exist (e.g. France, England, Ireland).
- **Meaningful names** for entries without a name in OpenStreetMap.
- **Search Console** setup and a preview image for link previews.

## Later

- **Photos** of slipways and marinas, through moderation.
- **Count confirmations** ("confirmed 3×") and edit suggestions before approving them.
- **Calmer map style** (vector tiles).
- **More countries**, e.g. Finland and Greece.

## Ideas

- **"Users say …":** keywords or a word cloud instead of star ratings on the detail pages.
