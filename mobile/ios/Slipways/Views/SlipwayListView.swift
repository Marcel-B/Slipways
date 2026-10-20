import SlipwaysKit
import SwiftUI

struct SlipwayListView: View {
    @Environment(\.api) private var api
    @State private var search = ""
    @State private var slipways: [SlipwaySummary]?
    @State private var error: Error?

    var body: some View {
        NavigationStack {
            content
                .navigationTitle("Slipanlagen")
                .navigationDestination(for: SlipwaySummary.self) { SlipwayDetailView(slipway: $0) }
                .searchable(text: $search, prompt: "Name, Ort oder Gewässer")
                .task(id: search) { await load() }
                .refreshable { await load() }
        }
    }

    @ViewBuilder
    private var content: some View {
        if let slipways {
            if slipways.isEmpty {
                ContentUnavailableView.search(text: search)
            } else {
                List(slipways) { slipway in
                    NavigationLink(value: slipway) {
                        VStack(alignment: .leading) {
                            Text(slipway.name)
                            Text(slipway.subtitle).font(.subheadline).foregroundStyle(.secondary)
                        }
                    }
                }
            }
        } else if error != nil {
            ContentUnavailableView {
                Label("Slipanlagen konnten nicht geladen werden", systemImage: "wifi.exclamationmark")
            } actions: {
                Button("Erneut versuchen") { Task { await load() } }
            }
        } else {
            ProgressView()
        }
    }

    private func load() async {
        // Debounce typing; a newer search cancels this task.
        if !search.isEmpty {
            try? await Task.sleep(for: .milliseconds(300))
            if Task.isCancelled { return }
        }
        do {
            slipways = try await api.slipways(search: search)
            error = nil
        } catch is CancellationError {
        } catch let error as URLError where error.code == .cancelled {
        } catch {
            if slipways == nil { self.error = error }
        }
    }
}
