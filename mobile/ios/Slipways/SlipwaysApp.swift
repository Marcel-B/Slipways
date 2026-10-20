import SlipwaysKit
import SwiftUI

@main
struct SlipwaysApp: App {
    @State private var session: Session
    private let api: SlipwaysAPI

    init() {
        let tokens = KeychainTokenStore()
        let api = SlipwaysAPI(baseURL: AppConfig.apiBaseURL, token: { tokens.token() })
        self.api = api
        _session = State(initialValue: Session(api: api, tokens: tokens))
    }

    var body: some Scene {
        WindowGroup {
            SlipwayListView()
                .environment(session)
                .environment(\.api, api)
                .task { await session.restore() }
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
