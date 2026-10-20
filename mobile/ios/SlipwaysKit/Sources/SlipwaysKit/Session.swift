import Foundation
import Observation

/// Where the bearer token is persisted between app starts (the Keychain in the app).
public protocol TokenStorage: Sendable {
    func token() -> String?
    func save(_ token: String?)
}

/// The signed-in user.
@MainActor
@Observable
public final class Session {
    public private(set) var user: User?

    private let api: SlipwaysAPI
    private let tokens: TokenStorage

    public init(api: SlipwaysAPI, tokens: TokenStorage) {
        self.api = api
        self.tokens = tokens
    }

    /// Loads the user for a token stored by an earlier app start.
    public func restore() async {
        guard tokens.token() != nil else { return }
        do {
            user = try await api.me()
        } catch let error as APIError where error.status == 401 {
            tokens.save(nil)
        } catch {
            // Offline: keep the token and try again on the next start.
        }
    }

    public func login(email: String, password: String) async throws {
        let token = try await api.login(email: email, password: password)
        tokens.save(token.accessToken)
        user = try await api.me()
    }

    public func register(email: String, password: String) async throws {
        try await api.register(email: email, password: password)
        try await login(email: email, password: password)
    }

    public func logout() {
        tokens.save(nil)
        user = nil
    }
}
