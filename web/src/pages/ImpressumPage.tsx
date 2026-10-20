import { Link } from 'react-router'
import { operator } from '../legal/operator'
import { OperatorAddress, OperatorEmail } from '../legal/OperatorField'

export function ImpressumPage() {
  return (
    <article className="legal">
      <title>Impressum · Slipways</title>
      <h1>Impressum</h1>
      <p>Angaben gemäß § 5 Digitale-Dienste-Gesetz (DDG) für die Website slipways.de und die Slipways-Apps für iOS und Android.</p>

      <h2>Anbieter</h2>
      <OperatorAddress />

      <h2>Kontakt</h2>
      <p>
        E-Mail: <OperatorEmail />
      </p>

      {operator.vatId && (
        <>
          <h2>Umsatzsteuer-ID</h2>
          <p>Umsatzsteuer-Identifikationsnummer gemäß § 27a Umsatzsteuergesetz: {operator.vatId}</p>
        </>
      )}

      <h2>Datenquellen</h2>
      <p>
        Kartendaten © <a href="https://www.openstreetmap.org/copyright">OpenStreetMap-Mitwirkende</a>,
        lizenziert unter der Open Database License (ODbL). Pegelstände von{' '}
        <a href="https://www.pegelonline.wsv.de/">PEGELONLINE</a> der Wasserstraßen- und Schifffahrtsverwaltung des Bundes,
        lizenziert unter der <a href="https://www.govdata.de/dl-de/zero-2-0">Datenlizenz Deutschland – Zero – Version 2.0</a>.
      </p>

      <h2>Urheberrecht und Lizenz</h2>
      <p>
        Website, Apps und die selbst erfassten Daten zu Slipanlagen, Marinas und Motorservices: © 2019–2026 Marcel Benders.
        Der <a href="https://github.com/Marcel-B/Slipways">Quellcode</a> ist öffentlich einsehbar und steht unter der{' '}
        <a href="https://github.com/Marcel-B/Slipways/blob/master/LICENSE">PolyForm Strict License 1.0.0</a>. Lesen und für
        persönliche, nicht-kommerzielle Zwecke ausprobieren ist erlaubt. Für alles Weitere, etwa kommerzielle Nutzung,
        Änderungen, eigene Instanzen oder die Übernahme der Daten, bitte vorher über die oben genannte E-Mail-Adresse fragen. Daten
        aus OpenStreetMap und PEGELONLINE stehen unter ihren eigenen Lizenzen (siehe oben).
      </p>

      <h2>Keine Gewähr</h2>
      <p>
        Alle Angaben zu Slipanlagen, Gewässern und Pegelständen werden ohne Gewähr bereitgestellt. Sie ersetzen weder amtliche
        Informationen noch die eigene Prüfung vor Ort und sind nicht für die Navigation bestimmt.
      </p>

      <p className="muted">
        Siehe auch <Link to="/datenschutz">Datenschutz</Link> und <Link to="/support">Support</Link>.
      </p>
    </article>
  )
}
