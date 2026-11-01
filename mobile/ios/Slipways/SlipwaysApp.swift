import SlipwaysKit
import SwiftUI

@main
struct SlipwaysApp: App {
    private let api = SlipwaysAPI(baseURL: AppConfig.apiBaseURL)
    @State private var tab = LaunchState.tab ?? .slipways

    var body: some Scene {
        WindowGroup {
            TabView(selection: $tab) {
                NavigationStack { SlipwayOverviewView().slipwaysDestinations() }
                    .tabItem { Label("Slipanlagen", systemImage: "sailboat") }
                    .tag(AppTab.slipways)
                NavigationStack { MarinaOverviewView().slipwaysDestinations() }
                    .tabItem { Label("Marinas", systemImage: "ferry") }
                    .tag(AppTab.marinas)
                NavigationStack { WaterOverviewView().slipwaysDestinations() }
                    .tabItem { Label("Gewässer", systemImage: "water.waves") }
                    .tag(AppTab.waters)
                NavigationStack { InfoView() }
                    .tabItem { Label("Info", systemImage: "info.circle") }
                    .tag(AppTab.info)
            }
            .environment(\.api, api)
        }
    }
}

enum AppConfig {
    /// https://slipways.de from Info.plist; the scheme's environment variable SLIPWAYS_API_BASE_URL overrides it,
    /// e.g. http://localhost:5080 for the docker compose API.
    static let apiBaseURL: URL = {
        if let value = ProcessInfo.processInfo.environment["SLIPWAYS_API_BASE_URL"], let url = URL(string: value) {
            return url
        }
        guard let value = Bundle.main.object(forInfoDictionaryKey: "SlipwaysAPIBaseURL") as? String,
              let url = URL(string: value) else {
            fatalError("SlipwaysAPIBaseURL is missing from Info.plist")
        }
        return url
    }()
}

enum AppTab: String {
    case slipways, marinas, waters, info
}

/// Where the app starts when the screenshot UI test (`SlipwaysScreenshots`) launches it; nothing is set otherwise. The
/// test passes environment variables: `SLIPWAYS_TAB` (an `AppTab`), `SLIPWAYS_AREA` (the map area as
/// `south,west,north,east`), `SLIPWAYS_OPEN` (the id of a place or water to open once loaded) and `SLIPWAYS_SELECT`
/// (the id of a water to highlight on the map).
enum LaunchState {
    private static let environment = ProcessInfo.processInfo.environment

    static let tab = environment["SLIPWAYS_TAB"].flatMap(AppTab.init(rawValue:))
    static let open = environment["SLIPWAYS_OPEN"]
    static let select = environment["SLIPWAYS_SELECT"]

    /// The area the overview maps start with: Europe unless the test asks for another.
    static let area: MapArea = {
        let values = (environment["SLIPWAYS_AREA"] ?? "").split(separator: ",").compactMap { Double($0) }
        guard values.count == 4 else { return .start }
        return MapArea(south: values[0], west: values[1], north: values[2], east: values[3])
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
