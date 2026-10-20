import { Link } from 'react-router'
import { operator } from '../legal/operator'
import { OperatorAddress, OperatorEmail, OperatorField } from '../legal/OperatorField'

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
        {operator.phone && (
          <>
            <br />
            Telefon: <OperatorField value={operator.phone} />
          </>
        )}
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
