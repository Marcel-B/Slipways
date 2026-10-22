import MapKit
import SlipwaysKit
import SwiftUI

/// Suggests a slipway we do not know yet: a pin in the middle of a map to move under the ramp, a name and a few details.
struct NewSlipwayView: View {
    /// Close enough to place the pin on the ramp; the website's map starts there for a new slipway too.
    private static let preciseZoom = 13.0

    @Environment(\.api) private var api
    @Environment(\.suggestions) private var store
    @Environment(\.dismiss) private var dismiss
    @State private var position: MapCameraPosition
    @State private var area: MapArea
    @State private var mapWidth = 390.0
    @State private var name = ""
    @State private var fields: [String: String] = [:]
    @State private var text = ""
    @State private var consent = false
    @State private var nearby: [NearbySlipway]?
    @State private var sending = false
    @State private var error: String?
    @State private var sent = false
    @State private var proof = ProofState()

    init(start: MapArea) {
        _area = State(initialValue: start)
        _position = State(initialValue: .region(MKCoordinateRegion(start)))
    }

    private var precise: Bool { area.zoom(width: mapWidth) >= Self.preciseZoom }

    private var canSend: Bool {
        !name.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty && precise && consent && proof.solution != nil && !sending
    }

    var body: some View {
        NavigationStack {
            Group {
                if sent {
                    SentView(message: "Deine Slipanlage wird geprüft und erscheint nach der Freigabe auf der Karte.") { dismiss() }
                } else {
                    form
                }
            }
            .navigationTitle("Slipanlage fehlt?")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                if !sent {
                    ToolbarItem(placement: .cancellationAction) { Button("Abbrechen") { dismiss() } }
                    ToolbarItem(placement: .confirmationAction) {
                        Button("Senden") { Task { await send() } }.disabled(!canSend || nearby != nil)
                    }
                }
            }
        }
        .task(id: proof.round) { await proof.run(api) }
    }

    private var form: some View {
        Form {
            Section {
                map.frame(height: 280).listRowInsets(EdgeInsets())
            } footer: {
                Text(precise
                     ? String(format: "Position: %.5f, %.5f", area.centerLatitude, area.centerLongitude)
                     : "Zoom hinein und schieb die Karte, bis der Pin genau auf der Rampe liegt.")
            }
            Section {
                TextField("Name, z. B. Slipanlage am Yachthafen Musterstadt", text: $name)
                    .onChange(of: name) { _, new in if new.count > 120 { name = String(new.prefix(120)) } }
                ForEach(["fee", "surface", "access"], id: \.self) { name in
                    if let field = Contributions.fields[name] {
                        FieldInput(field: field, value: binding(name))
                    }
                }
            }
            Section("Was gibt es dort?") {
                ForEach(Contributions.facts, id: \.self) { fact in
                    Toggle(Contributions.label(fact), isOn: Binding { fields[fact] == "yes" } set: { fields[fact] = $0 ? "yes" : "" })
                }
            }
            Section("Woher weißt du das? (freiwillig)") {
                TextField("z. B. „War im August dort.“", text: $text, axis: .vertical)
                    .lineLimit(2...4)
                    .onChange(of: text) { _, new in
                        if new.count > Contributions.textLength { text = String(new.prefix(Contributions.textLength)) }
                    }
            }
            ConsentSection(
                consent: $consent,
                text: "Ich kenne die Slipanlage selbst und habe die Angaben nicht aus anderen Verzeichnissen abgeschrieben. Slipways darf sie veröffentlichen."
            )
            if let nearby {
                Section {
                    ForEach(nearby) { slipway in
                        LabeledContent(slipway.name, value: "\(Int(slipway.distance.rounded())) m")
                    }
                    Button("Nein, trotzdem neu eintragen") { Task { await send(confirmNew: true) } }
                        .disabled(!canSend)
                } header: {
                    Text("Ist es eine von diesen?")
                } footer: {
                    Text("In der Nähe kennen wir schon diese Slipanlagen. Wenn deine dabei ist, ergänze sie lieber dort.")
                }
            }
            SendStateSection(proof: proof, error: error, sending: sending)
        }
    }

    private var map: some View {
        Map(position: $position)
            .overlay {
                // The tip of the pin marks the middle of the map.
                Image(systemName: "mappin")
                    .font(.system(size: 34))
                    .foregroundStyle(.red)
                    .offset(y: -17)
                    .allowsHitTesting(false)
                    .accessibilityLabel("Position der Slipanlage, zum Verschieben die Karte bewegen")
            }
            .onMapCameraChange(frequency: .onEnd) { context in
                area = MapArea(context.region)
                nearby = nil
            }
            .onGeometryChange(for: Double.self) { $0.size.width } action: { mapWidth = $0 }
    }

    private func binding(_ name: String) -> Binding<String> {
        Binding { fields[name] ?? "" } set: { fields[name] = $0 }
    }

    private func send(confirmNew: Bool = false) async {
        guard canSend, let solution = proof.solution else { return }
        sending = true
        error = nil
        defer { sending = false }
        let trimmed = name.trimmingCharacters(in: .whitespacesAndNewlines)
        var request = SuggestionRequest(kind: .newSlipway)
        request.name = trimmed
        request.latitude = (area.centerLatitude * 1e6).rounded() / 1e6
        request.longitude = (area.centerLongitude * 1e6).rounded() / 1e6
        request.fields = fields.filter { !$0.value.isEmpty }
        let note = text.trimmingCharacters(in: .whitespacesAndNewlines)
        request.text = note.isEmpty ? nil : note
        request.confirmNew = confirmNew
        request.proof = solution
        do {
            let created = try await api.suggest(request)
            store.remember(SentSuggestion(created: created, kind: .newSlipway, name: trimmed))
            sent = true
        } catch let error as NearbySlipwaysError {
            nearby = error.slipways
            proof.renew()
        } catch {
            self.error = (error as? APIError)?.userMessage ?? "Der Vorschlag konnte nicht gesendet werden. Bitte prüf die Verbindung."
            proof.renew()
        }
    }
}
