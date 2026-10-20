import { Link } from "react-router";
import { legalUpdatedAt } from "../legal/operator";
import { OperatorAddress, OperatorEmail } from "../legal/OperatorField";

export function DatenschutzPage() {
  return (
    <article className="legal">
      <title>Datenschutz · Slipways</title>
      <h1>Datenschutzerklärung</h1>
      <p>
        Diese Erklärung gilt für die Website slipways.de und die Slipways-Apps
        für iOS und Android. Slipways ist ein reines Nachschlagewerk: Es gibt
        keine Benutzerkonten, keine Registrierung, keine Werbung, kein Tracking
        und keine Analysewerkzeuge. Wir setzen keine Cookies und speichern
        nichts im Browser oder auf dem Gerät, das Sie identifiziert.
      </p>

      <h2>Verantwortlicher</h2>
      <OperatorAddress />
      <p>
        E-Mail: <OperatorEmail />
      </p>

      <h2>Aufruf der Website und Nutzung der Apps</h2>
      <p>
        Wenn Sie die Website aufrufen oder die Apps Daten laden, verbindet sich
        Ihr Browser bzw. Ihr Gerät mit unserem Server. Dabei werden technisch
        zwingend Ihre IP-Adresse, Datum und Uhrzeit, die abgerufene Adresse
        sowie Angaben zu Browser bzw. App und Betriebssystem übertragen. Wir
        verwenden diese Daten nur, um die Inhalte auszuliefern und den Betrieb
        sicher zu halten.
      </p>
      <p>
        Wir führen keine Zugriffsprotokolle. Technische Fehlermeldungen des
        Servers können im Einzelfall die IP-Adresse enthalten; sie werden in
        begrenztem Umfang gespeichert und fortlaufend automatisch überschrieben.
        Eine Kopie der Server-Meldungen liegt auf unserem eigenen
        Überwachungsserver, um Störungen zu erkennen, und wird dort nach 30
        Tagen automatisch gelöscht. Für die Überwachung erfassen wir außerdem
        Kennzahlen des Servers (z. B. Anzahl und Dauer der Anfragen,
        Speicherverbrauch), die keine personenbezogenen Daten enthalten.
      </p>
      <p>
        Rechtsgrundlage ist Art. 6 Abs. 1 lit. f DSGVO. Unser berechtigtes
        Interesse liegt darin, die Website und die Apps bereitzustellen und
        gegen Störungen und Angriffe zu schützen.
      </p>

      <h2>Hosting</h2>
      <p>
        Der Server steht in Nürnberg in einem Rechenzentrum der Hetzner Online
        GmbH, Industriestr. 25, 91710 Gunzenhausen. Hetzner verarbeitet die oben
        genannten Verbindungsdaten in unserem Auftrag auf Grundlage eines
        Vertrags zur Auftragsverarbeitung nach Art. 28 DSGVO.
      </p>

      <h2>Karten auf der Website</h2>
      <p>
        Die Karte auf der Website lädt Kartenbilder direkt von den Servern der
        OpenStreetMap Foundation, St John’s Innovation Centre, Cowley Road,
        Cambridge, CB4 0WS, Vereinigtes Königreich. Dabei erhält die
        OpenStreetMap Foundation Ihre IP-Adresse und die Angaben Ihres Browsers;
        sie nutzt dafür auch Dienstleister für die Auslieferung (Content
        Delivery Network). Für das Vereinigte Königreich besteht ein
        Angemessenheitsbeschluss der EU-Kommission. Rechtsgrundlage ist Art. 6
        Abs. 1 lit. f DSGVO; unser berechtigtes Interesse ist die Darstellung
        der Slipanlagen auf einer Karte. Näheres steht in der{" "}
        <a href="https://osmfoundation.org/wiki/Privacy_Policy">
          Datenschutzerklärung der OpenStreetMap Foundation
        </a>
        .
      </p>

      <h2>Karten in den Apps</h2>
      <p>
        Die iOS-App zeigt Karten mit Apple Maps (MapKit) an. Dabei lädt Ihr
        Gerät Kartendaten von Apple. Die Android-App lädt Kartenbilder,
        Schriften und Symbole von{" "}
        <a href="https://openfreemap.org/">OpenFreeMap</a>{" "}
        (tiles.openfreemap.org); dabei werden Ihre IP-Adresse und der angezeigte
        Kartenausschnitt übertragen. Außerdem kann sie eine Slipanlage auf Ihren
        Wunsch an eine installierte Karten-App übergeben. Für diese Dienste
        gelten die Datenschutzbestimmungen des jeweiligen Anbieters, etwa von{" "}
        <a href="https://www.apple.com/de/legal/privacy/">Apple</a> oder{" "}
        <a href="https://policies.google.com/privacy?hl=de">Google</a>.
      </p>

      <h2>Berechtigungen der Apps</h2>
      <p>
        Die Apps benötigen nur den Internetzugang, um Slipanlagen und
        Pegelstände zu laden. Sie fragen weder Ihren Standort noch Kontakte,
        Fotos oder andere Daten auf Ihrem Gerät ab.
      </p>

      <h2>App Store und Google Play</h2>
      <p>
        Wenn Sie eine App herunterladen, verarbeiten Apple (App Store) bzw.
        Google (Google Play) dafür Daten wie Ihr Konto und Ihre Gerätekennung in
        eigener Verantwortung. Wenn Sie in den Einstellungen Ihres Geräts
        zugestimmt haben, stellen Apple bzw. Google uns anonymisierte
        Absturzberichte und Nutzungsstatistiken zur Verfügung. Diese nutzen wir
        nur, um Fehler zu beheben.
      </p>

      <h2>Kontakt per E-Mail</h2>
      <p>
        Wenn Sie uns eine E-Mail schreiben, verarbeiten wir Ihre Adresse und den
        Inhalt, um Ihre Anfrage zu beantworten. Das E-Mail-Postfach wird bei der
        Contabo GmbH, Aschauer Straße 32a, 81549 München, in unserem Auftrag
        betrieben. Rechtsgrundlage ist Art. 6 Abs. 1 lit. f DSGVO, bei Anfragen
        zu einem Vertrag Art. 6 Abs. 1 lit. b DSGVO. Wir löschen die
        Nachrichten, wenn die Anfrage erledigt ist und keine gesetzlichen
        Aufbewahrungspflichten bestehen.
      </p>

      <h2>Pegelstände</h2>
      <p>
        Pegelstände holt unser Server selbst von PEGELONLINE ab. Dabei werden
        keine Daten über Sie übermittelt.
      </p>

      <h2>Ihre Rechte</h2>
      <p>Sie haben das Recht auf</p>
      <ul>
        <li>Auskunft über Ihre bei uns gespeicherten Daten (Art. 15 DSGVO),</li>
        <li>Berichtigung (Art. 16 DSGVO) und Löschung (Art. 17 DSGVO),</li>
        <li>
          Einschränkung der Verarbeitung (Art. 18 DSGVO) und
          Datenübertragbarkeit (Art. 20 DSGVO),
        </li>
        <li>
          Widerspruch gegen Verarbeitungen auf Grundlage von Art. 6 Abs. 1 lit.
          f DSGVO aus Gründen, die sich aus Ihrer besonderen Situation ergeben
          (Art. 21 DSGVO).
        </li>
      </ul>
      <p>
        Wenden Sie sich dafür formlos an <OperatorEmail />. Außerdem können Sie
        sich bei einer Datenschutz-Aufsichtsbehörde beschweren, insbesondere in
        dem Bundesland oder Mitgliedstaat, in dem Sie wohnen (Art. 77 DSGVO).
      </p>

      <h2>Änderungen</h2>
      <p>
        Wir passen diese Erklärung an, wenn sich die Website, die Apps oder die
        Rechtslage ändern. Es gilt die jeweils hier veröffentlichte Fassung.
      </p>

      <p className="muted">
        Stand: {legalUpdatedAt} · <Link to="/impressum">Impressum</Link> ·{" "}
        <Link to="/support">Support</Link>
      </p>
    </article>
  );
}
