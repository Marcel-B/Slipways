import Foundation

extension MapArea {
    /// This area grown by `factor` of its size on every side, e.g. to keep shapes drawn while the map pans a little.
    public func expanded(by factor: Double) -> MapArea {
        MapArea(
            south: south - latitudeDelta * factor,
            west: west - longitudeDelta * factor,
            north: north + latitudeDelta * factor,
            east: east + longitudeDelta * factor
        )
    }
}

extension Geometry {
    /// The parts of this geometry inside `area`, with points closer together than about `resolution` degrees merged.
    ///
    /// MapKit gives every overlay a drawing surface as large as the overlay's bounds at the current zoom. A river of a
    /// few hundred kilometres zoomed in to street level needs gigabytes, so the maps draw only what lies in and around the
    /// view. Lines are cut at the edge of `area`, areas clipped to it; the cut edges lie outside the view when `area` is
    /// larger than it.
    public func clipped(to area: MapArea, resolution: Double = 0) -> Geometry {
        Geometry(
            lines: lines.flatMap { Clipping.line(Clipping.thin($0, resolution), to: area) },
            polygons: polygons.compactMap { rings in
                guard let outer = rings.first else { return nil }
                let clipped = Clipping.polygon(Clipping.thin(outer, resolution), to: area)
                return clipped.count >= 3 ? [clipped] : nil
            }
        )
    }

    public var isEmpty: Bool { lines.isEmpty && polygons.isEmpty }
}

enum Clipping {
    /// Drops points closer than `resolution` to the last kept one; first and last stay.
    static func thin(_ points: [Position], _ resolution: Double) -> [Position] {
        guard resolution > 0, points.count > 2 else { return points }
        var kept = [points[0]]
        for point in points.dropFirst().dropLast() {
            let last = kept[kept.count - 1]
            if abs(point.latitude - last.latitude) >= resolution || abs(point.longitude - last.longitude) >= resolution {
                kept.append(point)
            }
        }
        kept.append(points[points.count - 1])
        return kept
    }

    /// The pieces of a line inside `area` (Liang–Barsky per segment, joined where they touch).
    static func line(_ points: [Position], to area: MapArea) -> [[Position]] {
        var pieces: [[Position]] = []
        var current: [Position] = []
        for (a, b) in zip(points, points.dropFirst()) {
            guard let (start, end) = segment(a, b, area) else {
                if current.count >= 2 { pieces.append(current) }
                current = []
                continue
            }
            if current.last != start {
                if current.count >= 2 { pieces.append(current) }
                current = [start]
            }
            current.append(end)
        }
        if current.count >= 2 { pieces.append(current) }
        return pieces
    }

    private static func segment(_ a: Position, _ b: Position, _ area: MapArea) -> (Position, Position)? {
        let dx = b.longitude - a.longitude
        let dy = b.latitude - a.latitude
        var t0 = 0.0
        var t1 = 1.0
        let edges = [
            (-dx, a.longitude - area.west), (dx, area.east - a.longitude),
            (-dy, a.latitude - area.south), (dy, area.north - a.latitude),
        ]
        for (p, q) in edges {
            if p == 0 {
                if q < 0 { return nil }
            } else {
                let t = q / p
                if p < 0 { t0 = max(t0, t) } else { t1 = min(t1, t) }
                if t0 > t1 { return nil }
            }
        }
        let start = t0 == 0 ? a : Position(latitude: a.latitude + t0 * dy, longitude: a.longitude + t0 * dx)
        let end = t1 == 1 ? b : Position(latitude: a.latitude + t1 * dy, longitude: a.longitude + t1 * dx)
        return (start, end)
    }

    /// The part of a ring inside `area` (Sutherland–Hodgman).
    static func polygon(_ ring: [Position], to area: MapArea) -> [Position] {
        var output = ring
        if output.first == output.last { output.removeLast() }
        let edges: [(inside: (Position) -> Bool, cross: (Position, Position) -> Position)] = [
            ({ $0.longitude >= area.west }, { cross($0, $1, longitude: area.west) }),
            ({ $0.longitude <= area.east }, { cross($0, $1, longitude: area.east) }),
            ({ $0.latitude >= area.south }, { cross($0, $1, latitude: area.south) }),
            ({ $0.latitude <= area.north }, { cross($0, $1, latitude: area.north) }),
        ]
        for edge in edges {
            let input = output
            output = []
            guard let last = input.last else { break }
            var previous = last
            for point in input {
                if edge.inside(point) {
                    if !edge.inside(previous) { output.append(edge.cross(previous, point)) }
                    output.append(point)
                } else if edge.inside(previous) {
                    output.append(edge.cross(previous, point))
                }
                previous = point
            }
        }
        return output
    }

    private static func cross(_ a: Position, _ b: Position, longitude: Double) -> Position {
        let t = (longitude - a.longitude) / (b.longitude - a.longitude)
        return Position(latitude: a.latitude + t * (b.latitude - a.latitude), longitude: longitude)
    }

    private static func cross(_ a: Position, _ b: Position, latitude: Double) -> Position {
        let t = (latitude - a.latitude) / (b.latitude - a.latitude)
        return Position(latitude: latitude, longitude: a.longitude + t * (b.longitude - a.longitude))
    }
}
