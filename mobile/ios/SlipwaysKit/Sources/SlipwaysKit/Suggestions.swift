import Foundation
#if canImport(CryptoKit)
import CryptoKit
#endif

public enum PlaceType: String, Codable, Hashable, Sendable {
    case slipway, marina
}

public enum SuggestionKind: String, Codable, Hashable, Sendable {
    case newSlipway = "new_slipway", edit, tip, report

    /// "Neue Slipanlage", "Ergänzung", …
    public var label: String {
        switch self {
        case .newSlipway: "Neue Slipanlage"
        case .edit: "Ergänzung"
        case .tip: "Tipp"
        case .report: "Hinweis auf ein Problem"
        }
    }
}

/// A proof-of-work challenge in the format of ALTCHA: find the number with SHA-256(salt + number) = challenge.
public struct ProofChallenge: Codable, Hashable, Sendable {
    public let algorithm: String
    public let challenge: String
    public let maxNumber: Int
    public let salt: String
    public let signature: String

    public init(algorithm: String, challenge: String, maxNumber: Int, salt: String, signature: String) {
        self.algorithm = algorithm
        self.challenge = challenge
        self.maxNumber = maxNumber
        self.salt = salt
        self.signature = signature
    }
}

public struct ProofSolution: Codable, Hashable, Sendable {
    public let algorithm: String
    public let challenge: String
    public let number: Int
    public let salt: String
    public let signature: String
}

public enum Proof {
    public struct Unsolvable: Error {}

    /// Finds the number for a challenge; about a second for the server's usual one. Runs off the caller's actor.
    public static func solve(_ challenge: ProofChallenge) async throws -> ProofSolution {
        guard let target = bytes(hex: challenge.challenge) else { throw Unsolvable() }
        let salt = Array(challenge.salt.utf8)
        for number in 0...max(challenge.maxNumber, 0) {
            if number % 1000 == 0 { try Task.checkCancellation() }
            if sha256(salt + Array(String(number).utf8)) == target {
                return ProofSolution(
                    algorithm: challenge.algorithm, challenge: challenge.challenge, number: number,
                    salt: challenge.salt, signature: challenge.signature)
            }
        }
        throw Unsolvable()
    }

    static func bytes(hex: String) -> [UInt8]? {
        let chars = Array(hex.utf8)
        guard chars.count % 2 == 0 else { return nil }
        var result: [UInt8] = []
        result.reserveCapacity(chars.count / 2)
        for i in stride(from: 0, to: chars.count, by: 2) {
            guard let byte = UInt8(String(decoding: chars[i...i + 1], as: UTF8.self), radix: 16) else { return nil }
            result.append(byte)
        }
        return result
    }

    static func sha256(_ data: [UInt8]) -> [UInt8] {
        #if canImport(CryptoKit)
        Array(SHA256.hash(data: data))
        #else
        SHA256Fallback.hash(data)
        #endif
    }
}

#if !canImport(CryptoKit)
/// SHA-256 for platforms without CryptoKit, such as Linux where the package's tests also run.
enum SHA256Fallback {
    private static let k: [UInt32] = [
        0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
        0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
        0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
        0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
        0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
        0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
        0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
        0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2,
    ]

    static func hash(_ message: [UInt8]) -> [UInt8] {
        var h: [UInt32] = [0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a, 0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19]
        var data = message + [0x80]
        while data.count % 64 != 56 { data.append(0) }
        let bits = UInt64(message.count) * 8
        data += (0..<8).reversed().map { UInt8(truncatingIfNeeded: bits >> (UInt64($0) * 8)) }

        var w = [UInt32](repeating: 0, count: 64)
        for chunk in stride(from: 0, to: data.count, by: 64) {
            for i in 0..<16 {
                w[i] = (0..<4).reduce(0) { $0 << 8 | UInt32(data[chunk + i * 4 + $1]) }
            }
            for i in 16..<64 {
                let s0 = rotr(w[i - 15], 7) ^ rotr(w[i - 15], 18) ^ (w[i - 15] >> 3)
                let s1 = rotr(w[i - 2], 17) ^ rotr(w[i - 2], 19) ^ (w[i - 2] >> 10)
                w[i] = w[i - 16] &+ s0 &+ w[i - 7] &+ s1
            }
            var (a, b, c, d, e, f, g, hh) = (h[0], h[1], h[2], h[3], h[4], h[5], h[6], h[7])
            for i in 0..<64 {
                let t1 = hh &+ (rotr(e, 6) ^ rotr(e, 11) ^ rotr(e, 25)) &+ ((e & f) ^ (~e & g)) &+ k[i] &+ w[i]
                let t2 = (rotr(a, 2) ^ rotr(a, 13) ^ rotr(a, 22)) &+ ((a & b) ^ (a & c) ^ (b & c))
                (hh, g, f, e, d, c, b, a) = (g, f, e, d &+ t1, c, b, a, t1 &+ t2)
            }
            for (i, v) in [a, b, c, d, e, f, g, hh].enumerated() { h[i] = h[i] &+ v }
        }
        return h.flatMap { v in (0..<4).reversed().map { UInt8(truncatingIfNeeded: v >> (UInt32($0) * 8)) } }
    }

    private static func rotr(_ x: UInt32, _ n: UInt32) -> UInt32 { (x >> n) | (x << (32 - n)) }
}
#endif

/// A suggestion as the app sends it; see `SuggestionRequestDto` in the backend.
public struct SuggestionRequest: Encodable, Sendable {
    public let kind: SuggestionKind
    public var placeType: PlaceType?
    public var placeId: String?
    public var name: String?
    public var latitude: Double?
    public var longitude: Double?
    public var fields: [String: String]?
    public var text: String?
    public var confirmNew = false
    public var proof: ProofSolution?

    public init(kind: SuggestionKind) {
        self.kind = kind
    }
}

/// `token` is shown only once; with it the sender can look at or withdraw the suggestion.
public struct SuggestionCreated: Codable, Hashable, Sendable {
    public let id: String
    public let token: String
}

public struct NearbySlipway: Codable, Hashable, Identifiable, Sendable {
    public let id: String
    public let name: String
    /// In metres.
    public let distance: Double
    public let source: Source
}

/// A suggested new slipway lies close to ones we have; sending it again with `confirmNew` takes it anyway.
public struct NearbySlipwaysError: Error, Sendable {
    public let slipways: [NearbySlipway]
}

public struct SuggestionStatus: Codable, Hashable, Sendable {
    public enum State: String, Codable, Sendable { case pending, accepted, rejected }

    public let id: String
    public let kind: SuggestionKind
    public let status: State
    public let created: Date
    public let decided: Date?
}

/// A suggestion sent from this device; kept only here, so the app can say what is waiting and show its state.
public struct SentSuggestion: Codable, Hashable, Identifiable, Sendable {
    public let id: String
    public let token: String
    public let kind: SuggestionKind
    public let placeType: PlaceType?
    public let placeId: String?
    /// The place's name, or the name of a new slipway, for the list of sent suggestions.
    public let name: String
    public let fields: [String]
    public let sent: Date

    public init(
        created: SuggestionCreated, kind: SuggestionKind, placeType: PlaceType? = nil, placeId: String? = nil,
        name: String, fields: [String] = [], sent: Date = .now
    ) {
        id = created.id
        token = created.token
        self.kind = kind
        self.placeType = placeType
        self.placeId = placeId
        self.name = name
        self.fields = fields
        self.sent = sent
    }
}

/// Suggestions sent from this device, in the app's defaults. Like the website's local storage: nothing leaves the device.
public final class SuggestionStore: @unchecked Sendable {
    // Older entries are decided or deleted on the server by then (see the privacy page).
    static let keepDays = 90.0
    // A suggestion still undecided after this long was most likely turned down.
    static let pendingDays = 30.0

    private static let key = "slipways-suggestions"
    private let defaults: UserDefaults
    private let lock = NSLock()

    public init(defaults: UserDefaults = .standard) {
        self.defaults = defaults
    }

    /// Newest first.
    public func all(now: Date = .now) -> [SentSuggestion] {
        lock.withLock { load(now: now) }.sorted { $0.sent > $1.sent }
    }

    public func remember(_ suggestion: SentSuggestion) {
        lock.withLock { save(load(now: .now) + [suggestion]) }
    }

    public func forget(_ id: String) {
        lock.withLock { save(load(now: .now).filter { $0.id != id }) }
    }

    /// Fields of a place this device sent values for that still wait, as far as the app can tell: sent recently, and
    /// after the place last changed (`changed`: when it was updated or got a value from users).
    public func pendingFields(_ type: PlaceType, _ id: String, changed: Date?, now: Date = .now) -> Set<String> {
        let cutoff = now.addingTimeInterval(-Self.pendingDays * 86_400)
        return Set(
            all(now: now)
                .filter { $0.placeType == type && $0.placeId == id && $0.sent > cutoff && $0.sent > (changed ?? .distantPast) }
                .flatMap(\.fields)
        )
    }

    private func load(now: Date) -> [SentSuggestion] {
        guard let data = defaults.data(forKey: Self.key),
              let all = try? JSONDecoder().decode([SentSuggestion].self, from: data) else { return [] }
        let cutoff = now.addingTimeInterval(-Self.keepDays * 86_400)
        return all.filter { $0.sent > cutoff }
    }

    private func save(_ suggestions: [SentSuggestion]) {
        if let data = try? JSONEncoder().encode(suggestions) { defaults.set(data, forKey: Self.key) }
    }
}
