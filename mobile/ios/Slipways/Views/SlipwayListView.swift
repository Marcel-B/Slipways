import SlipwaysKit
import SwiftUI

struct SlipwayListView: View {
    @Environment(\.api) private var api
    @State private var search = ""
    @State private var slipways: [SlipwaySummary]?
    @State private var error: Error?

    var body: some View {
        content
            .navigationTitle("Slipanlagen")
            .searchable(text: $search, prompt: "Name, Ort oder Gewässer")
            .task(id: search) { await load() }
            .refreshable { await load() }
    }

    @ViewBuilder
    private var content: some View {
        if let slipways {
            if slipways.isEmpty {
                ContentUnavailableView.search(text: search)
            } else {
                List {
                    Section {
                        ForEach(slipways) { slipway in
                            NavigationLink(value: slipway) {
                                VStack(alignment: .leading) {
                                    Text(slipway.name)
                                    if !slipway.subtitle.isEmpty {
                                        Text(slipway.subtitle).font(.subheadline).foregroundStyle(.secondary)
                                    }
                                }
                            }
                        }
                    } footer: {
                        if slipways.contains(where: { $0.source == .osm }) { OSMAttribution() }
                    }
                }
            }
        } else if error != nil {
            LoadFailedView(title: "Slipanlagen konnten nicht geladen werden") { await load() }
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
        } catch let error where error.isCancellation {
        } catch {
            if slipways == nil { self.error = error }
        }
    }
}
