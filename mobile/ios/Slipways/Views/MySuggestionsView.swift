import SlipwaysKit
import SwiftUI

/// Suggestions sent from this device, with their state; pending ones can be withdrawn.
struct MySuggestionsView: View {
    @Environment(\.suggestions) private var store
    @State private var suggestions: [SentSuggestion] = []

    var body: some View {
        List {
            if suggestions.isEmpty {
                ContentUnavailableView(
                    "Noch keine Vorschläge",
                    systemImage: "tray",
                    description: Text("Was du zu Slipanlagen und Marinas vorschlägst, erscheint hier, bis es entschieden ist.")
                )
            }
            ForEach(suggestions) { suggestion in
                SuggestionRow(suggestion: suggestion) {
                    store.forget(suggestion.id)
                    suggestions = store.all()
                }
            }
        }
        .navigationTitle("Meine Vorschläge")
        .onAppear { suggestions = store.all() }
        .refreshable { suggestions = store.all() }
    }
}

private struct SuggestionRow: View {
    let suggestion: SentSuggestion
    let onWithdrawn: () -> Void

    @Environment(\.api) private var api
    @State private var status: SuggestionStatus?
    @State private var gone = false
    @State private var withdrawing = false
    @State private var error: String?

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            Text(suggestion.name)
            Text("\(suggestion.kind.label), gesendet am \(Format.date(suggestion.sent))")
                .font(.subheadline)
                .foregroundStyle(.secondary)
            state
            if let error { Text(error).font(.caption).foregroundStyle(.red) }
        }
        .task { await load() }
        .swipeActions {
            if status?.status == .pending {
                Button("Zurückziehen", role: .destructive) { Task { await withdraw() } }
                    .disabled(withdrawing)
            }
        }
    }

    @ViewBuilder
    private var state: some View {
        if gone {
            Label("Nicht mehr vorhanden", systemImage: "questionmark.circle").foregroundStyle(.secondary)
        } else if let status {
            switch status.status {
            case .pending:
                Label("Wird geprüft, zum Zurückziehen nach links wischen", systemImage: "clock").foregroundStyle(.orange)
            case .accepted:
                Label("Angenommen am \(status.decided.map { Format.date($0) } ?? "?"). Danke!", systemImage: "checkmark.circle")
                    .foregroundStyle(.green)
            case .rejected:
                Label("Nicht übernommen (\(status.decided.map { Format.date($0) } ?? "?"))", systemImage: "xmark.circle")
                    .foregroundStyle(.secondary)
            }
        } else {
            ProgressView()
        }
    }

    private func load() async {
        do {
            status = try await api.suggestion(id: suggestion.id, token: suggestion.token)
        } catch let error as APIError where error.status == 404 {
            // Decided suggestions are deleted after a while, withdrawn ones at once.
            gone = true
        } catch {
            if !error.isCancellation { self.error = "Der Stand konnte nicht geladen werden." }
        }
    }

    private func withdraw() async {
        withdrawing = true
        defer { withdrawing = false }
        do {
            try await api.withdrawSuggestion(id: suggestion.id, token: suggestion.token)
            onWithdrawn()
        } catch {
            self.error = (error as? APIError)?.userMessage ?? "Der Vorschlag konnte nicht zurückgezogen werden."
        }
    }
}
