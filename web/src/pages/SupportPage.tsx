import { Link } from 'react-router'
import { OperatorEmail } from '../legal/OperatorField'

export function SupportPage() {
  return (
    <article className="legal">
      <title>Support · Slipways</title>
      <h1>Support</h1>
      <p>
        Slipways zeigt Slipanlagen, Marinas und aktuelle Pegelstände an Gewässern in Deutschland und weiteren Ländern Europas, auf der Website und in den Apps für
        iOS und Android.
      </p>

      <h2>Kontakt</h2>
      <p>
        Fragen, Fehler oder Hinweise auf falsche Angaben schicken Sie bitte per E-Mail an <OperatorEmail />. Hilfreich sind der Name
        der Slipanlage, bei Fehlern in den Apps außerdem das Gerät, die Version des Betriebssystems und die Version der App.
      </p>

      <h2>Häufige Fragen</h2>
      <dl className="faq">
        <dt>Brauche ich ein Konto?</dt>
        <dd>Nein. Slipways lässt sich ohne Anmeldung nutzen, auch zum Mitmachen. Es gibt keine Benutzerkonten.</dd>

        <dt>Woher stammen die Daten?</dt>
        <dd>
          Angaben zu Slipanlagen stammen aus eigener Erfassung und aus <a href="https://www.openstreetmap.org/">OpenStreetMap</a>,
          Pegelstände
          von <a href="https://www.pegelonline.wsv.de/">PEGELONLINE</a> der Wasserstraßen- und Schifffahrtsverwaltung des Bundes,
          in den Niederlanden von <a href="https://waterinfo.rws.nl/">Rijkswaterstaat</a> (Wasserstand über NAP).
        </dd>

        <dt>Wie aktuell sind die Pegelstände?</dt>
        <dd>
          Slipways ruft die Pegelstände alle 15 Minuten ab. Die Messwerte selbst können je nach Pegel älter sein; maßgeblich ist der
          angezeigte Zeitpunkt der Messung.
        </dd>

        <dt>Eine Angabe stimmt nicht. Was kann ich tun?</dt>
        <dd>
          Auf der Detailseite über den Stift neben der Angabe oder über „Etwas stimmt nicht“ (siehe unten), oder per E-Mail. Angaben
          aus OpenStreetMap können Sie auch direkt auf{' '}
          <a href="https://www.openstreetmap.org/fixthebug">openstreetmap.org</a> korrigieren; die Änderung erscheint dann mit der
          nächsten Aktualisierung bei Slipways.
        </dd>

        <dt>Welche Geräte werden unterstützt?</dt>
        <dd>Die iOS-App läuft ab iOS 17, die Android-App ab Android 8.0. Die Website funktioniert in allen aktuellen Browsern.</dd>

        <dt>Werden meine Daten gespeichert?</dt>
        <dd>
          Slipways verwendet kein Tracking und keine Cookies. Einzelheiten stehen in der <Link to="/datenschutz">Datenschutzerklärung</Link>.
        </dd>
      </dl>

      <h2 id="mitmachen">Mitmachen</h2>
      <p>
        Auf jeder Detailseite können Sie fehlende Angaben ergänzen, falsche korrigieren, ankreuzen, was es vor Ort gibt (etwa
        Parkplatz für Gespann, Steg oder Toilette), einen kurzen Tipp geben oder ein Problem melden. Fehlt eine Slipanlage ganz,
        schlagen Sie sie über „Slipanlage fehlt?“ auf der Startseite vor. Ein Konto brauchen Sie dafür nicht.
      </p>
      <p>
        Jeder Vorschlag wird von Hand geprüft, bevor er erscheint. Bitte tragen Sie nur ein, was Sie aus eigener Erfahrung wissen,
        und schreiben Sie nichts aus anderen Verzeichnissen oder Webseiten ab. Mit dem Absenden erlauben Sie Slipways, den Beitrag
        dauerhaft und ohne Namensnennung zu veröffentlichen. Ergänzungen zu Einträgen aus OpenStreetMap veröffentlichen wir wie die
        Einträge selbst unter der{' '}
        <a href="https://opendatacommons.org/licenses/odbl/1-0/">Open Database License (ODbL)</a>; sie stehen gesammelt unter{' '}
        <a href="/api/contributions/odbl">/api/contributions/odbl</a> zum Herunterladen bereit.
      </p>
      <p>
        Tipps sollen sachlich sein: keine Werbung, keine Links, keine Angaben zu Personen und keine Bewertungen von Betreibern.
      </p>

      <h2>Keine Gewähr</h2>
      <p>
        Alle Angaben werden ohne Gewähr bereitgestellt. Prüfen Sie Zufahrt, Wasserstand und Bedingungen vor Ort. Slipways ist nicht
        für die Navigation bestimmt.
      </p>

      <p className="muted">
        <Link to="/impressum">Impressum</Link> · <Link to="/datenschutz">Datenschutz</Link>
      </p>
    </article>
  )
}
