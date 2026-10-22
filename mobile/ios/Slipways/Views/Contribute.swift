import SlipwaysKit
import SwiftUI

/// The place a suggestion is about.
struct SuggestPlace: Hashable {
    let type: PlaceType
    let id: String
    let name: String
    let source: Source
}

/// What the suggestion form asks for: values of some fields, a tip or a report of something wrong.
enum SuggestMode: Hashable, Identifiable {
    case edit(fields: [String], title: String)
    case tip
    case report

    var id: Self { self }

    var kind: SuggestionKind {
        switch self {
        case .edit: .edit
        case .tip: .tip
        case .report: .report
        }
    }

    var title: String {
        switch self {
        case let .edit(_, title): title
        case .tip: "Tipp geben"
        case .report: "Etwas stimmt nicht"
        }
    }

    /// The form for one field, from the pencil next to its value or from „Fehlt noch“.
    static func field(_ field: String, adding: Bool = false) -> SuggestMode {
        .edit(fields: [field], title: "\(Contributions.label(field)) \(adding ? "ergänzen" : "korrigieren")")
    }
}

/// Fetches and solves a proof-of-work challenge as soon as a form opens, so it is usually done by the time the sender
/// presses send. Run `run` in a `.task(id: round)`; `renew` starts over after a solution was used or turned down.
@MainActor @Observable
final class ProofState {
    private(set) var solution: ProofSolution?
    private(set) var error: String?
    private(set) var round = 0

    func run(_ api: SlipwaysAPI) async {
        do {
            let challenge = try await api.challenge()
            let found = try await Proof.solve(challenge)
            if !Task.isCancelled { solution = found }
        } catch where !error.isCancellation {
            self.error = "Der Spam-Schutz ließ sich nicht berechnen. Bitte versuch es später noch einmal."
        } catch {}
    }

    func renew() {
        solution = nil
        error = nil
        round += 1
    }
}

/// The form for a suggestion about a slipway or marina, shown as a sheet.
struct SuggestSheet: View {
    let place: SuggestPlace
    let mode: SuggestMode
    /// The values the place shows now, to fill in the form.
    let current: [String: String]

    @Environment(\.api) private var api
    @Environment(\.suggestions) private var store
    @Environment(\.dismiss) private var dismiss
    @State private var values: [String: String] = [:]
    @State private var text = ""
    @State private var consent = false
    @State private var sending = false
    @State private var error: String?
    @State private var sent = false
    @State private var proof = ProofState()

    private var fields: [String] {
        if case let .edit(fields, _) = mode { fields } else { [] }
    }

    private var changed: [String: String] { Contributions.changed(values, from: current) }

    private var hasContent: Bool {
        mode.kind == .edit ? !changed.isEmpty : !text.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty
    }

    private var canSend: Bool { hasContent && consent && proof.solution != nil && !sending }

    var body: some View {
        NavigationStack {
            Group {
                if sent {
                    SentView { dismiss() }
                } else {
                    form
                }
            }
            .navigationTitle(mode.title)
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                if !sent {
                    ToolbarItem(placement: .cancellationAction) { Button("Abbrechen") { dismiss() } }
                    ToolbarItem(placement: .confirmationAction) {
                        Button("Senden") { Task { await send() } }.disabled(!canSend)
                    }
                }
            }
        }
        .task(id: proof.round) { await proof.run(api) }
        .onAppear { values = Dictionary(uniqueKeysWithValues: fields.map { ($0, current[$0] ?? "") }) }
    }

    private var form: some View {
        Form {
            Section {
                Text(place.name).foregroundStyle(.secondary)
            }
            if !fields.isEmpty {
                Section {
                    ForEach(fields, id: \.self) { name in
                        if let field = Contributions.fields[name] {
                            FieldInput(field: field, value: binding(name))
                        }
                    }
                }
            }
            Section {
                TextField(textPrompt, text: $text, axis: .vertical)
                    .lineLimit(mode.kind == .edit ? 2...4 : 4...8)
                    .onChange(of: text) { _, new in
                        let limit = mode.kind == .tip ? Contributions.tipLength : Contributions.textLength
                        if new.count > limit { text = String(new.prefix(limit)) }
                    }
            } header: {
                Text(textLabel)
            } footer: {
                if mode.kind == .tip {
                    Text("\(text.count)/\(Contributions.tipLength) Zeichen. Bitte sachlich, ohne Werbung und ohne Links.")
                }
            }
            ConsentSection(
                consent: $consent,
                text: "Ich weiß das aus eigener Erfahrung und habe es nicht aus anderen Verzeichnissen abgeschrieben. Slipways darf den Beitrag veröffentlichen\(place.source == .osm ? ", als Ergänzung zu OpenStreetMap unter der ODbL" : "")."
            )
            SendStateSection(proof: proof, error: error, sending: sending)
        }
    }

    private var textLabel: String {
        switch mode.kind {
        case .tip: "Dein Tipp"
        case .report: "Was stimmt nicht?"
        default: "Woher weißt du das? (freiwillig)"
        }
    }

    private var textPrompt: String {
        switch mode.kind {
        case .tip: "z. B. „Am Wochenende früh kommen, der Parkplatz ist klein.“"
        case .report: "z. B. „Die Slipanlage ist seit Sommer gesperrt.“"
        default: "z. B. „War im August dort.“"
        }
    }

    private func binding(_ name: String) -> Binding<String> {
        Binding { values[name] ?? "" } set: { values[name] = $0 }
    }

    private func send() async {
        guard canSend, let solution = proof.solution else { return }
        sending = true
        error = nil
        defer { sending = false }
        var request = SuggestionRequest(kind: mode.kind)
        request.placeType = place.type
        request.placeId = place.id
        let changed = changed
        if mode.kind == .edit { request.fields = changed }
        let note = text.trimmingCharacters(in: .whitespacesAndNewlines)
        request.text = note.isEmpty ? nil : note
        request.proof = solution
        do {
            let created = try await api.suggest(request)
            store.remember(SentSuggestion(
                created: created, kind: mode.kind, placeType: place.type, placeId: place.id, name: place.name,
                fields: Array(changed.keys)))
            sent = true
        } catch {
            self.error = (error as? APIError)?.userMessage ?? "Der Vorschlag konnte nicht gesendet werden. Bitte prüf die Verbindung."
            proof.renew()
        }
    }
}

/// A picker for fields with choices, else a text field.
struct FieldInput: View {
    let field: SuggestionField
    @Binding var value: String

    var body: some View {
        if field.choices.isEmpty {
            LabeledContent(field.label) {
                TextField(field.placeholder ?? "", text: $value)
                    .multilineTextAlignment(.trailing)
                    .keyboardType(keyboard)
                    .textInputAutocapitalization(field.keyboard == .text ? .sentences : .never)
                    .autocorrectionDisabled(field.keyboard != .text)
            }
        } else {
            Picker(field.label, selection: $value) {
                Text("weiß nicht").tag("")
                ForEach(field.choices, id: \.value) { Text($0.label).tag($0.value) }
            }
        }
    }

    private var keyboard: UIKeyboardType {
        switch field.keyboard {
        case .text: .default
        case .decimal: .decimalPad
        case .phone: .phonePad
        case .email: .emailAddress
        }
    }
}

struct ConsentSection: View {
    @Binding var consent: Bool
    let text: String

    var body: some View {
        Section {
            Toggle(isOn: $consent) { Text(text).font(.subheadline) }
        } footer: {
            VStack(alignment: .leading, spacing: 4) {
                Text("Alles wird vor der Veröffentlichung von Hand geprüft. Kein Konto, keine Namen.")
                Link("Mehr dazu", destination: Links.contribute)
            }
        }
    }
}

/// The state of the spam protection and the last error, at the end of a form.
struct SendStateSection: View {
    let proof: ProofState
    let error: String?
    let sending: Bool

    var body: some View {
        Section {
            if let error { Text(error).foregroundStyle(.red) }
            if let error = proof.error { Text(error).foregroundStyle(.red) }
            if sending {
                Label("Wird gesendet…", systemImage: "paperplane")
            } else if proof.solution != nil {
                Label("Spam-Schutz bereit", systemImage: "checkmark.shield").foregroundStyle(.secondary)
            } else if proof.error == nil {
                HStack {
                    ProgressView()
                    Text("Spam-Schutz wird berechnet…").foregroundStyle(.secondary)
                }
            }
        }
        .font(.subheadline)
    }
}

/// Thanks after sending, with where to find the suggestion's state.
struct SentView: View {
    var message = "Dein Vorschlag wird geprüft und erscheint nach der Freigabe."
    let done: () -> Void

    var body: some View {
        ContentUnavailableView {
            Label("Danke!", systemImage: "checkmark.circle")
        } description: {
            Text("\(message) Den Stand siehst du unter Info › Meine Vorschläge, dort kannst du ihn auch zurückziehen.")
        } actions: {
            Button("Fertig", action: done).buttonStyle(.borderedProminent)
        }
    }
}

/// A small pencil next to a value that opens the form for it.
struct EditButton: View {
    let label: String
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            Image(systemName: "pencil").imageScale(.small)
        }
        .buttonStyle(.borderless)
        .accessibilityLabel("\(label) korrigieren")
    }
}

/// „Was gibt es dort?“ for slipways, missing details, and the ways to contribute, below the facts of a place.
struct ContributeSections: View {
    let place: SuggestPlace
    /// The values the place shows now; empty ones count as missing.
    let values: [String: String]
    /// Facts users ticked; only for slipways.
    var ticked: [TickedFact] = []
    let osmElement: String?
    /// Fields this device sent values for that still wait for moderation.
    let pending: Set<String>
    let suggest: (SuggestMode) -> Void

    var body: some View {
        if place.type == .slipway {
            Section {
                if ticked.isEmpty {
                    Text("Noch keine Angaben.").foregroundStyle(.secondary)
                }
                ForEach(ticked, id: \.field) { fact in
                    Label(fact.label, systemImage: fact.present ? "checkmark" : "xmark")
                        .foregroundStyle(fact.present ? Color.primary : Color.secondary)
                }
                Button("Angaben ankreuzen", systemImage: "checklist") {
                    suggest(.edit(fields: Contributions.facts, title: "Was gibt es dort?"))
                }
            } header: {
                Text("Was gibt es dort?")
            } footer: {
                if let first = ticked.first {
                    Text("von Nutzern ergänzt, Stand \(Format.monthYear(first.retrieved))")
                } else if !pending.isDisjoint(with: Contributions.facts) {
                    Text("Dein Vorschlag wird geprüft")
                }
            }
        }

        let missing = Contributions.wanted(place.type).filter { (values[$0] ?? "").isEmpty && !pending.contains($0) }
        if !missing.isEmpty {
            Section {
                ForEach(missing, id: \.self) { field in
                    Button(Contributions.label(field), systemImage: "plus") { suggest(.field(field, adding: true)) }
                }
            } header: {
                Text("Fehlt noch")
            } footer: {
                Text("Weißt du mehr? Ergänze es, es wird vor der Veröffentlichung geprüft.")
            }
        }

        Section {
            Button("Tipp geben", systemImage: "lightbulb") { suggest(.tip) }
            Button("Etwas stimmt nicht", systemImage: "flag") { suggest(.report) }
            if let element = osmElement, let url = Contributions.osmEditURL(element) {
                Link(destination: url) { Label("In OpenStreetMap bearbeiten", systemImage: "square.and.pencil") }
            }
        } header: {
            Text("Mitmachen")
        }
    }
}

extension EnvironmentValues {
    @Entry var suggestions = SuggestionStore()
}
