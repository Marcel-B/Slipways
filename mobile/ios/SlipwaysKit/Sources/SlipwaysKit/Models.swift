import Foundation

public struct Water: Codable, Hashable, Sendable {
    public let id: String
    public let name: String
}

public struct SlipwaySummary: Codable, Hashable, Identifiable, Sendable {
    public let id: String
    public let name: String
    public let latitude: Double
    public let longitude: Double
    public let postalCode: String?
    public let city: String?
    public let country: String?
    public let water: Water

    /// "Donau · 93309 Kelheim"
    public var subtitle: String {
        [water.name, [postalCode, city].compactMap { $0 }.joined(separator: " ")]
            .filter { !$0.isEmpty }
            .joined(separator: " · ")
    }
}

public struct MarinaSummary: Codable, Hashable, Sendable {
    public let id: String
    public let name: String
    public let url: String?
}

public struct SlipwayDetails: Codable, Hashable, Identifiable, Sendable {
    public let id: String
    public let name: String
    public let latitude: Double
    public let longitude: Double
    public let street: String?
    public let postalCode: String?
    public let city: String?
    public let country: String?
    public let comment: String?
    public let rating: Int
    /// Launch fee in euros; nil when unknown.
    public let costs: Double?
    public let pro: String?
    public let contra: String?
    public let water: Water
    public let marina: MarinaSummary?
    public let extras: [String]

    public var address: String {
        let place = [postalCode, city].compactMap { $0 }.joined(separator: " ")
        return [street, place.isEmpty ? nil : place, country].compactMap { $0 }.joined(separator: ", ")
    }
}

public struct User: Codable, Hashable, Sendable {
    public let id: String
    public let email: String
    public let displayName: String?
}

public struct AccessToken: Codable, Sendable {
    public let tokenType: String
    public let accessToken: String
    public let expiresIn: Int
    public let refreshToken: String
}
