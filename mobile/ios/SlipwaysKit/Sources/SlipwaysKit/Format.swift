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

    /// "4 Slipanlagen", "1 Slipanlage", "1.234 Slipanlagen"
    public static func count(_ n: Int, _ singular: String, _ plural: String) -> String {
        "\(number(n)) \(n == 1 ? singular : plural)"
    }

    /// "1.234"
    public static func number(_ n: Int) -> String {
        n.formatted(.number.locale(german))
    }

    /// "20.10.2026"
    public static func date(_ date: Date, timeZone: TimeZone = .current) -> String {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = timeZone
        let parts = calendar.dateComponents([.day, .month, .year], from: date)
        return String(format: "%02d.%02d.%04d", parts.day!, parts.month!, parts.year!)
    }

    /// "10.2026", for values from users: the day would suggest more precision than a moderated value has.
    public static func monthYear(_ date: Date, timeZone: TimeZone = .current) -> String {
        String(Format.date(date, timeZone: timeZone).dropFirst(3))
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
    /// How contributing works and what happens to suggestions.
    public static let contribute = URL(string: "https://slipways.de/support#mitmachen")!
    public static let osmCopyright = URL(string: "https://www.openstreetmap.org/copyright")!
    public static let pegelonline = URL(string: "https://www.pegelonline.wsv.de/")!
    public static let pegelonlineLicence = URL(string: "https://www.govdata.de/dl-de/zero-2-0")!
    public static let licence = URL(string: "https://github.com/Marcel-B/Slipways/blob/master/LICENSE")!
    public static let licenceContact = URL(string: "mailto:slipways@marcelbenders.com")!

    /// "Daten © OpenStreetMap-Mitwirkende (ODbL)", required next to every map and list with imported entries.
    public static let osmAttribution = "Daten © OpenStreetMap-Mitwirkende (ODbL)"

    public static func osm(_ element: String) -> URL? {
        URL(string: "https://www.openstreetmap.org/\(element)")
    }

    /// Phone tags from OpenStreetMap look like "+49 9441 1234"; the dial link keeps only digits and the plus.
    public static func phone(_ value: String) -> URL? {
        let digits = value.filter { $0.isNumber || $0 == "+" }
        return digits.isEmpty ? nil : URL(string: "tel:\(digits)")
    }

    public static func mail(_ value: String) -> URL? {
        let text = value.trimmingCharacters(in: .whitespaces)
        return text.isEmpty ? nil : URL(string: "mailto:\(text)")
    }

    /// Website tags from OpenStreetMap sometimes lack the scheme, e.g. "www.leg-an.com".
    public static func web(_ value: String) -> URL? {
        let text = value.trimmingCharacters(in: .whitespaces)
        guard !text.isEmpty else { return nil }
        return URL(string: text.contains("://") ? text : "https://\(text)")
    }
}
