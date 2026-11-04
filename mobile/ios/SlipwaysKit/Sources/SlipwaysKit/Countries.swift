import Foundation

/// Places of one country, drawn as a single bubble while the map shows a continent.
public struct CountryGroup<T>: Identifiable {
    /// ISO code such as "NL".
    public let code: String
    public let items: [T]
    /// Where the bubble stands: the median, which stays on land for long coasts where the mean drifts out to sea.
    public let latitude: Double
    public let longitude: Double
    /// Where most of its places lie; a few far-off ones (islands, Svalbard) do not count.
    public let area: MapArea

    public var id: String { code }
    public var name: String { Countries.name(code) }
}

/// A country to choose with the number of entries in it.
public struct CountryCount: Hashable, Identifiable, Sendable {
    public let code: String
    public let count: Int

    public init(code: String, count: Int) {
        self.code = code
        self.count = count
    }

    public var id: String { code }
    /// "Frankreich (1.234)"
    public var label: String { "\(Countries.name(code)) (\(Format.number(count)))" }
}

/// Grouping by country as on the website (web/src/components/countries.ts).
public enum Countries {
    private static let names = [
        "AT": "Österreich", "BE": "Belgien", "CH": "Schweiz", "CZ": "Tschechien", "DE": "Deutschland",
        "DK": "Dänemark", "ES": "Spanien", "FI": "Finnland", "FR": "Frankreich", "GB": "Vereinigtes Königreich",
        "GR": "Griechenland", "HR": "Kroatien", "IE": "Irland", "IT": "Italien", "LU": "Luxemburg",
        "NL": "Niederlande", "NO": "Norwegen", "PL": "Polen", "PT": "Portugal", "SE": "Schweden", "SI": "Slowenien",
    ]

    /// The German name of a country, e.g. "Niederlande" for NL.
    public static func name(_ code: String) -> String {
        names[code] ?? Locale(identifier: "de_DE").localizedString(forRegionCode: code) ?? code
    }

    /// The flag emoji of a country, built from its ISO code ("DE" → 🇩🇪); nil for anything that is not two letters.
    public static func flag(_ code: String) -> String? {
        let letters = code.uppercased().unicodeScalars
        let alphabet: ClosedRange<Unicode.Scalar> = "A"..."Z"
        guard letters.count == 2, letters.allSatisfy({ alphabet.contains($0) }) else { return nil }
        var flag = ""
        for letter in letters {
            guard let symbol = Unicode.Scalar(0x1F1E6 + letter.value - 65) else { return nil }
            flag.unicodeScalars.append(symbol)
        }
        return flag
    }

    /// Groups places by their ISO country code, largest first; places without one are left out.
    public static func group<T>(
        _ items: [T],
        country: (T) -> String?,
        position: (T) -> (latitude: Double, longitude: Double)
    ) -> [CountryGroup<T>] {
        var groups: [String: [T]] = [:]
        for item in items {
            guard let code = country(item) else { continue }
            groups[code, default: []].append(item)
        }
        return groups.map { code, members in
            let points = members.map(position)
            return CountryGroup(
                code: code,
                items: members,
                latitude: median(points.map(\.latitude)),
                longitude: median(points.map(\.longitude)),
                area: coreArea(points)!
            )
        }
        .sorted { $0.items.count != $1.items.count ? $0.items.count > $1.items.count : $0.code < $1.code }
    }

    /// How many entries touch each country, most first; a river through three countries counts in all three.
    public static func counts(_ countries: [[String]]) -> [CountryCount] {
        var counts: [String: Int] = [:]
        for code in countries.joined() { counts[code, default: 0] += 1 }
        return counts.map { CountryCount(code: $0.key, count: $0.value) }
            .sorted { $0.count != $1.count ? $0.count > $1.count : $0.code < $1.code }
    }

    /// Like `MapArea.fitting`, but from the 2nd to the 98th percentile, so a single far-off place does not zoom out
    /// to half of Europe.
    public static func coreArea(_ points: [(latitude: Double, longitude: Double)]) -> MapArea? {
        guard !points.isEmpty else { return nil }
        func spread(_ values: [Double]) -> (Double, Double) {
            let sorted = values.sorted()
            let low = sorted[Int((Double(sorted.count) * 0.02).rounded(.down))]
            let high = sorted[Int((Double(sorted.count) * 0.98).rounded(.up)) - 1]
            return (low, high)
        }
        let (south, north) = spread(points.map(\.latitude))
        let (west, east) = spread(points.map(\.longitude))
        return MapArea.fitting([(latitude: south, longitude: west), (latitude: north, longitude: east)])
    }

    private static func median(_ values: [Double]) -> Double {
        values.sorted()[values.count / 2]
    }
}
