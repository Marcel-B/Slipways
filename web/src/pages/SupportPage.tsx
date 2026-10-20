import { Link } from 'react-router'
import { OperatorEmail } from '../legal/OperatorField'

export function SupportPage() {
  return (
    <article className="legal">
      <title>Support · Slipways</title>
      <h1>Support</h1>
      <p>
        Slipways zeigt Slipanlagen und aktuelle Pegelstände an Gewässern in Deutschland, auf der Website und in den Apps für
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
        <dd>Nein. Slipways lässt sich ohne Anmeldung nutzen, es gibt keine Benutzerkonten.</dd>

        <dt>Woher stammen die Daten?</dt>
        <dd>
          Angaben zu Slipanlagen stammen aus eigener Erfassung und aus <a href="https://www.openstreetmap.org/">OpenStreetMap</a>,
          Pegelstände
          von <a href="https://www.pegelonline.wsv.de/">PEGELONLINE</a> der Wasserstraßen- und Schifffahrtsverwaltung des Bundes.
        </dd>

        <dt>Wie aktuell sind die Pegelstände?</dt>
        <dd>
          Slipways ruft die Pegelstände alle 15 Minuten ab. Die Messwerte selbst können je nach Pegel älter sein; maßgeblich ist der
          angezeigte Zeitpunkt der Messung.
        </dd>

        <dt>Eine Angabe stimmt nicht. Was kann ich tun?</dt>
        <dd>
          Schreiben Sie uns eine E-Mail. Angaben aus OpenStreetMap können Sie auch direkt auf{' '}
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
