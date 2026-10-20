import SlipwaysKit
import SwiftUI

@main
struct SlipwaysApp: App {
    private let api = SlipwaysAPI(baseURL: AppConfig.apiBaseURL)

    var body: some Scene {
        WindowGroup {
            TabView {
                NavigationStack { SlipwayListView().slipwaysDestinations() }
                    .tabItem { Label("Slipanlagen", systemImage: "sailboat") }
                NavigationStack { WaterListView().slipwaysDestinations() }
                    .tabItem { Label("Gewässer", systemImage: "water.waves") }
                NavigationStack { InfoView() }
                    .tabItem { Label("Info", systemImage: "info.circle") }
            }
            .environment(\.api, api)
        }
    }
}

enum AppConfig {
    static let apiBaseURL: URL = {
        guard let value = Bundle.main.object(forInfoDictionaryKey: "SlipwaysAPIBaseURL") as? String,
              let url = URL(string: value) else {
            fatalError("SlipwaysAPIBaseURL is missing from Info.plist")
        }
        return url
    }()
}

extension EnvironmentValues {
    @Entry var api = SlipwaysAPI(baseURL: URL(string: "http://localhost:5080")!)
}

extension View {
    /// Slipways and waters link to each other, so every tab needs both destinations.
    func slipwaysDestinations() -> some View {
        navigationDestination(for: SlipwaySummary.self) { SlipwayDetailView(slipway: $0) }
            .navigationDestination(for: Water.self) { WaterDetailView(water: $0) }
    }
}
