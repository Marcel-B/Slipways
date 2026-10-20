import Foundation

/// German texts and formats shared by the views; kept here so they are tested.
public enum Format {
    private static let german = Locale(identifier: "de_DE")

    /// "93309 Kelheim", or nil when both are missing.
    public static func place(_ postalCode: String?, _ city: String?) -> String? {
        let text = [postalCode, city].compactMap { $0 }.joined(separator: " ")
        return text.isEmpty ? nil : text
    }

    public static func euros(_ value: Decimal) -> String {
        value.formatted(.currency(code: "EUR").locale(german))
    }

    /// "248 cm · niedrig"
    public static func waterLevel(_ level: WaterLevel) -> String {
        let value = level.value.formatted(.number.precision(.fractionLength(0...1)).locale(german))
        return ["\(value) cm", level.state.flatMap(Labels.state)].compactMap { $0 }.joined(separator: " · ")
    }

    /// "1,2 km"
    public static func kilometres(_ value: Double) -> String {
        value.formatted(.number.precision(.fractionLength(0...1)).locale(german)) + " km"
    }

    /// "4 Slipanlagen", "1 Slipanlage"
    public static func count(_ n: Int, _ singular: String, _ plural: String) -> String {
        "\(n) \(n == 1 ? singular : plural)"
    }
}

/// OpenStreetMap tag values and PEGELONLINE states in German; values without a label are shown as they are.
public enum Labels {
    private static let fees = ["yes": "kostenpflichtig", "no": "kostenlos"]

    private static let accesses = [
        "yes": "öffentlich",
        "permissive": "geduldet",
        "customers": "nur für Kunden",
        "members": "nur für Mitglieder",
        "private": "privat",
        "no": "kein Zugang",
    ]

    private static let surfaces = [
        "concrete": "Beton",
        "asphalt": "Asphalt",
        "paved": "befestigt",
        "paving_stones": "Pflaster",
        "sett": "Pflaster",
        "gravel": "Kies",
        "fine_gravel": "Kies",
        "wood": "Holz",
        "sand": "Sand",
        "grass": "Gras",
        "unpaved": "unbefestigt",
    ]

    private static let states = [
        "low": "niedrig",
        "normal": "normal",
        "high": "hoch",
        "unknown": "unbekannt",
        "commented": "kommentiert",
        "out-dated": "veraltet",
    ]

    public static func fee(_ value: String) -> String { fees[value] ?? value }
    public static func access(_ value: String) -> String { accesses[value] ?? value }
    public static func surface(_ value: String) -> String { surfaces[value] ?? value }
    /// Nil for states PEGELONLINE may add later.
    public static func state(_ value: String) -> String? { states[value] }
}

/// Links the apps show; the legal pages of the website also cover the apps.
public enum Links {
    public static let website = URL(string: "https://slipways.de")!
    public static let impressum = URL(string: "https://slipways.de/impressum")!
    public static let datenschutz = URL(string: "https://slipways.de/datenschutz")!
    public static let support = URL(string: "https://slipways.de/support")!
    public static let osmCopyright = URL(string: "https://www.openstreetmap.org/copyright")!
    public static let pegelonline = URL(string: "https://www.pegelonline.wsv.de/")!
    public static let pegelonlineLicence = URL(string: "https://www.govdata.de/dl-de/zero-2-0")!

    /// "Daten © OpenStreetMap-Mitwirkende (ODbL)", required next to every map and list with imported entries.
    public static let osmAttribution = "Daten © OpenStreetMap-Mitwirkende (ODbL)"

    public static func osm(_ element: String) -> URL? {
        URL(string: "https://www.openstreetmap.org/\(element)")
    }

    /// Website tags from OpenStreetMap sometimes lack the scheme, e.g. "www.leg-an.com".
    public static func web(_ value: String) -> URL? {
        let text = value.trimmingCharacters(in: .whitespaces)
        guard !text.isEmpty else { return nil }
        return URL(string: text.contains("://") ? text : "https://\(text)")
    }
}
