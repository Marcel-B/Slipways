import SlipwaysKit
import SwiftUI

struct WaterListView: View {
    @Environment(\.api) private var api
    @State private var search = ""
    @State private var waters: [WaterSummary]?
    @State private var error: Error?

    var body: some View {
        content
            .navigationTitle("Gewässer")
            .searchable(text: $search, prompt: "Gewässer suchen")
            .task(id: search) { await load() }
            .refreshable { await load() }
    }

    @ViewBuilder
    private var content: some View {
        if let waters {
            if waters.isEmpty {
                ContentUnavailableView.search(text: search)
            } else {
                List(waters) { water in
                    NavigationLink(value: water.water) {
                        VStack(alignment: .leading, spacing: 2) {
                            Text(water.name)
                            Text(counts(water)).font(.subheadline).foregroundStyle(.secondary)
                            if let gauge = water.gauge {
                                HStack(spacing: 4) {
                                    Image(systemName: "ruler")
                                    WaterLevelText(level: gauge.waterLevel)
                                }
                                .font(.subheadline)
                            }
                        }
                    }
                }
            }
        } else if error != nil {
            LoadFailedView(title: "Gewässer konnten nicht geladen werden") { await load() }
        } else {
            ProgressView()
        }
    }

    private func counts(_ water: WaterSummary) -> String {
        [Format.count(water.slipwayCount, "Slipanlage", "Slipanlagen"), Format.count(water.stationCount, "Pegel", "Pegel")]
            .joined(separator: " · ")
    }

    private func load() async {
        if !search.isEmpty {
            try? await Task.sleep(for: .milliseconds(300))
            if Task.isCancelled { return }
        }
        do {
            waters = try await api.waters(search: search)
            error = nil
        } catch let error where error.isCancellation {
        } catch {
            if waters == nil { self.error = error }
        }
    }
}
