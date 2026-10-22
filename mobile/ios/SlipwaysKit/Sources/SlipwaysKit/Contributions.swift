import Foundation

/// Fields users can send values for, as the server knows them (SuggestionFields.cs) and the website labels them
/// (web/src/contribute/fields.ts).
public enum Contributions {
    private static let labels = [
        "fee": "Gebühr",
        "costs": "Kosten in Euro",
        "access": "Zugang",
        "opening_hours": "Öffnungszeiten",
        "surface": "Untergrund",
        "operator": "Betreiber",
        "phone": "Telefon",
        "email": "E-Mail",
        "trailer_parking": "Parkplatz für Gespann",
        "jetty": "Steg",
        "toilet": "Toilette",
        "crane": "Kran",
        "steep": "Rampe steil",
        "slippery_at_low_water": "Rutschig bei Niedrigwasser",
        "key_required": "Nur mit Schlüssel",
    ]

    /// Facts about a slipway to tick, shown together under „Was gibt es dort?“.
    public static let facts = ["trailer_parking", "jetty", "toilet", "crane", "steep", "slippery_at_low_water", "key_required"]

    public static func label(_ field: String) -> String { labels[field] ?? field }

    /// The newest value users sent in for a field, if any; it wins over the place's own one.
    public static func fromUsers(_ infos: [PlaceInfo], _ field: String) -> PlaceInfo? {
        infos.filter { $0.field == field && $0.source == "user" }.max { $0.retrieved < $1.retrieved }
    }

    /// Tips from users in the order the server sends them.
    public static func tips(_ infos: [PlaceInfo]) -> [PlaceInfo] {
        infos.filter { $0.field == "tip" && $0.source == "user" }
    }
}
