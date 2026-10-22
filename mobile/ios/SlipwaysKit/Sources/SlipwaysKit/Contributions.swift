import Foundation

/// A field users can suggest a value for, as the server knows it (SuggestionFields.cs).
public struct SuggestionField: Hashable, Sendable {
    public enum Keyboard: Sendable { case text, decimal, phone, email }

    public struct Choice: Hashable, Sendable {
        public let value: String
        public let label: String
    }

    public let name: String
    public let label: String
    /// Choices for the value; without them the field takes text.
    public let choices: [Choice]
    public let placeholder: String?
    public let keyboard: Keyboard

    init(_ name: String, _ label: String, choices: [(String, String)] = [], placeholder: String? = nil, keyboard: Keyboard = .text) {
        self.name = name
        self.label = label
        self.choices = choices.map { Choice(value: $0.0, label: $0.1) }
        self.placeholder = placeholder
        self.keyboard = keyboard
    }

    /// The label of a choice, else the value itself.
    public func label(of value: String) -> String {
        choices.first { $0.value == value }?.label ?? value
    }
}

/// Fields users can send values for, as the server knows them (SuggestionFields.cs) and the website labels them
/// (web/src/contribute/fields.ts).
public enum Contributions {
    private static let yesNo = [("yes", "ja"), ("no", "nein")]

    public static let fields: [String: SuggestionField] = Dictionary(uniqueKeysWithValues: [
        SuggestionField("fee", "Gebühr", choices: [("no", "kostenlos"), ("yes", "kostenpflichtig")]),
        SuggestionField("costs", "Kosten in Euro", placeholder: "z. B. 5", keyboard: .decimal),
        SuggestionField("access", "Zugang", choices: [
            ("yes", "öffentlich"), ("permissive", "geduldet"), ("customers", "nur für Kunden"),
            ("members", "nur für Mitglieder"), ("private", "privat"), ("no", "kein Zugang"),
        ]),
        SuggestionField("opening_hours", "Öffnungszeiten", placeholder: "z. B. Mo–Fr 8–18 Uhr, Sa 9–12 Uhr"),
        SuggestionField("surface", "Untergrund", choices: [
            ("concrete", "Beton"), ("asphalt", "Asphalt"), ("paving_stones", "Pflaster"), ("gravel", "Kies"),
            ("wood", "Holz"), ("sand", "Sand"), ("grass", "Gras"), ("unpaved", "unbefestigt"),
        ]),
        SuggestionField("operator", "Betreiber", placeholder: "z. B. Stadt, Verein oder Marina"),
        SuggestionField("phone", "Telefon", placeholder: "+49 …", keyboard: .phone),
        SuggestionField("email", "E-Mail", placeholder: "hafen@example.de", keyboard: .email),
        SuggestionField("trailer_parking", "Parkplatz für Gespann", choices: yesNo),
        SuggestionField("jetty", "Steg", choices: yesNo),
        SuggestionField("toilet", "Toilette", choices: yesNo),
        SuggestionField("crane", "Kran", choices: yesNo),
        SuggestionField("steep", "Rampe steil", choices: yesNo),
        SuggestionField("slippery_at_low_water", "Rutschig bei Niedrigwasser", choices: yesNo),
        SuggestionField("key_required", "Nur mit Schlüssel", choices: yesNo),
    ].map { ($0.name, $0) })

    /// Facts about a slipway to tick, shown together under „Was gibt es dort?“.
    public static let facts = ["trailer_parking", "jetty", "toilet", "crane", "steep", "slippery_at_low_water", "key_required"]

    /// The few fields a place should have; only these are asked for when missing.
    public static func wanted(_ type: PlaceType) -> [String] {
        switch type {
        case .slipway: ["fee", "access", "opening_hours", "surface"]
        case .marina: ["phone", "opening_hours", "access", "fee"]
        }
    }

    /// Longest tip the server takes; other texts may have up to `textLength` characters.
    public static let tipLength = 200
    public static let textLength = 500

    public static func label(_ field: String) -> String { fields[field]?.label ?? field }

    /// The newest value users sent in for a field, if any; it wins over the place's own one.
    public static func fromUsers(_ infos: [PlaceInfo], _ field: String) -> PlaceInfo? {
        infos.filter { $0.field == field && $0.source == "user" }.max { $0.retrieved < $1.retrieved }
    }

    /// Tips from users in the order the server sends them.
    public static func tips(_ infos: [PlaceInfo]) -> [PlaceInfo] {
        infos.filter { $0.field == "tip" && $0.source == "user" }
    }

    /// The OpenStreetMap editor for an element such as "node/123".
    public static func osmEditURL(_ element: String) -> URL? {
        let parts = element.split(separator: "/")
        guard parts.count == 2 else { return nil }
        return URL(string: "https://www.openstreetmap.org/edit?\(parts[0])=\(parts[1])")
    }

    /// Only the values the sender filled in and changed go to the server.
    public static func changed(_ values: [String: String], from current: [String: String]) -> [String: String] {
        values.filter { field, value in
            !value.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty && value != (current[field] ?? "")
        }
    }
}
