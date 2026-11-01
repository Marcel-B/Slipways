import MapKit
import SlipwaysKit
import SwiftUI
import UIKit

/// A request to move the map, applied once per `id`.
struct CameraRequest: Equatable {
    let id: Int
    let area: MapArea
    let animated: Bool
}

/// A search hit with a pin on the water map.
struct WaterPin: Equatable {
    let id: String
    let name: String
    let position: Position
}

/// The water overview's map: an `MKMapView` with one overlay per water.
///
/// SwiftUI's `Map` draws every `MapPolyline` again while zooming, so long rivers made zooming stutter, and at street
/// level they needed so much memory that they had to be cut to the view (which rebuilt all of them after every move).
/// `MKMapView` draws its own multi-polyline and multi-polygon renderers as vectors, so the shapes are added once, whole,
/// and the map only adds or removes the waters that come or go.
struct WaterMap: UIViewRepresentable {
    /// The shapes to draw. Changes only take effect with a new `shapesRevision`, so the view does not compare all shapes.
    let shapes: [WaterShape]
    let shapesRevision: Int
    let pins: [WaterPin]
    let selectedId: String?
    let camera: CameraRequest
    /// After every move, with true when a finger moved the map.
    var onCameraEnd: (MapArea, Bool) -> Void
    /// When a finger starts to move the map.
    var onUserMove: () -> Void
    var onTap: (CLLocationCoordinate2D) -> Void

    func makeCoordinator() -> Coordinator { Coordinator() }

    func makeUIView(context: Context) -> MKMapView {
        let mapView = MKMapView()
        mapView.delegate = context.coordinator
        // North stays up: a compass would sit under the search field.
        mapView.isRotateEnabled = false
        mapView.isPitchEnabled = false
        mapView.showsCompass = false
        mapView.register(MKAnnotationView.self, forAnnotationViewWithReuseIdentifier: Coordinator.pinReuseId)
        let tap = UITapGestureRecognizer(target: context.coordinator, action: #selector(Coordinator.tapped(_:)))
        tap.delegate = context.coordinator
        mapView.addGestureRecognizer(tap)
        return mapView
    }

    func updateUIView(_ mapView: MKMapView, context: Context) {
        let coordinator = context.coordinator
        coordinator.parent = self
        coordinator.update(mapView, shapes: shapes, revision: shapesRevision)
        coordinator.update(mapView, pins: pins)
        coordinator.select(selectedId, in: mapView)
        if coordinator.cameraId != camera.id {
            coordinator.cameraId = camera.id
            mapView.setRegion(MKCoordinateRegion(camera.area), animated: camera.animated)
        }
    }

    @MainActor
    final class Coordinator: NSObject, @preconcurrency MKMapViewDelegate, UIGestureRecognizerDelegate {
        static let pinReuseId = "water"

        var parent: WaterMap?
        var cameraId: Int?
        private var shapesRevision: Int?
        private var selectedId: String?
        /// The overlays of each water, and back from an overlay to its water.
        private var overlays: [String: [MKOverlay]] = [:]
        private var shapeIds: [ObjectIdentifier: String] = [:]
        private var pins: [String: MKPointAnnotation] = [:]
        private var pinIds: [ObjectIdentifier: String] = [:]
        private var userMoving = false

        func update(_ mapView: MKMapView, shapes: [WaterShape], revision: Int) {
            guard revision != shapesRevision else { return }
            shapesRevision = revision
            var wanted: [String: WaterShape] = [:]
            for shape in shapes where wanted[shape.id] == nil {
                wanted[shape.id] = shape
            }
            for id in overlays.keys where wanted[id] == nil {
                removeOverlays(of: id, from: mapView)
            }
            // A water keeps its course from one load to the next, so only new ones are added.
            for (id, shape) in wanted where overlays[id] == nil {
                addOverlays(of: shape, to: mapView)
            }
        }

        func update(_ mapView: MKMapView, pins wanted: [WaterPin]) {
            let ids = Set(wanted.map(\.id))
            let gone = pins.filter { !ids.contains($0.key) }
            for (id, annotation) in gone {
                pins[id] = nil
                pinIds[ObjectIdentifier(annotation)] = nil
            }
            mapView.removeAnnotations(Array(gone.values))
            var added: [MKPointAnnotation] = []
            for pin in wanted where pins[pin.id] == nil {
                let annotation = MKPointAnnotation()
                annotation.coordinate = CLLocationCoordinate2D(pin.position)
                annotation.title = pin.name
                pins[pin.id] = annotation
                pinIds[ObjectIdentifier(annotation)] = pin.id
                added.append(annotation)
            }
            mapView.addAnnotations(added)
        }

        func select(_ id: String?, in mapView: MKMapView) {
            guard id != selectedId else { return }
            let previous = selectedId
            selectedId = id
            for changed in [previous, id].compactMap({ $0 }) {
                // A renderer takes its colour when it is made; adding the overlays again also puts the selection on top.
                if let existing = overlays[changed] {
                    mapView.removeOverlays(existing)
                    mapView.addOverlays(existing)
                }
                if let annotation = pins[changed], let view = mapView.view(for: annotation) {
                    style(view, selected: changed == id)
                }
            }
        }

        private func addOverlays(of shape: WaterShape, to mapView: MKMapView) {
            var added: [MKOverlay] = []
            let lines = shape.geometry.lines.filter { $0.count >= 2 }.map { line in
                let coordinates = line.map { CLLocationCoordinate2D($0) }
                return MKPolyline(coordinates: coordinates, count: coordinates.count)
            }
            if !lines.isEmpty { added.append(MKMultiPolyline(lines)) }
            let areas = shape.geometry.polygons.compactMap { rings -> MKPolygon? in
                guard let outer = rings.first, outer.count >= 3 else { return nil }
                let holes = rings.dropFirst().filter { $0.count >= 3 }.map { ring in
                    let coordinates = ring.map { CLLocationCoordinate2D($0) }
                    return MKPolygon(coordinates: coordinates, count: coordinates.count)
                }
                let coordinates = outer.map { CLLocationCoordinate2D($0) }
                return MKPolygon(coordinates: coordinates, count: coordinates.count, interiorPolygons: holes)
            }
            if !areas.isEmpty { added.append(MKMultiPolygon(areas)) }
            guard !added.isEmpty else { return }
            overlays[shape.id] = added
            for overlay in added {
                shapeIds[ObjectIdentifier(overlay)] = shape.id
            }
            mapView.addOverlays(added)
        }

        private func removeOverlays(of id: String, from mapView: MKMapView) {
            guard let existing = overlays.removeValue(forKey: id) else { return }
            for overlay in existing {
                shapeIds[ObjectIdentifier(overlay)] = nil
            }
            mapView.removeOverlays(existing)
        }

        private func style(_ view: MKAnnotationView, selected: Bool) {
            let configuration = UIImage.SymbolConfiguration(textStyle: .title2)
                .applying(UIImage.SymbolConfiguration(paletteColors: [.white, selected ? .systemOrange : .systemBlue]))
            view.image = UIImage(systemName: "drop.circle.fill", withConfiguration: configuration)
        }

        // MARK: MKMapViewDelegate

        func mapView(_ mapView: MKMapView, rendererFor overlay: MKOverlay) -> MKOverlayRenderer {
            let selected = selectedId != nil && shapeIds[ObjectIdentifier(overlay)] == selectedId
            let color: UIColor = selected ? .systemOrange : .systemBlue
            if let lines = overlay as? MKMultiPolyline {
                let renderer = MKMultiPolylineRenderer(multiPolyline: lines)
                renderer.strokeColor = color
                renderer.lineWidth = selected ? 4 : 2.5
                return renderer
            }
            if let areas = overlay as? MKMultiPolygon {
                let renderer = MKMultiPolygonRenderer(multiPolygon: areas)
                renderer.fillColor = color.withAlphaComponent(0.2)
                renderer.strokeColor = color
                renderer.lineWidth = 1.5
                return renderer
            }
            return MKOverlayRenderer(overlay: overlay)
        }

        func mapView(_ mapView: MKMapView, viewFor annotation: MKAnnotation) -> MKAnnotationView? {
            guard let id = pinIds[ObjectIdentifier(annotation)] else { return nil }
            let view = mapView.dequeueReusableAnnotationView(withIdentifier: Self.pinReuseId, for: annotation)
            style(view, selected: id == selectedId)
            view.canShowCallout = false
            // The pins only show; the tap recogniser finds them, so they need no selection of their own.
            view.isEnabled = false
            view.layer.shadowColor = UIColor.black.cgColor
            view.layer.shadowOpacity = 0.3
            view.layer.shadowRadius = 2
            view.layer.shadowOffset = .zero
            view.isAccessibilityElement = true
            view.accessibilityLabel = annotation.title ?? nil
            return view
        }

        func mapView(_ mapView: MKMapView, regionWillChangeAnimated animated: Bool) {
            userMoving = Self.fingerOnMap(mapView)
            if userMoving { parent?.onUserMove() }
        }

        func mapView(_ mapView: MKMapView, regionDidChangeAnimated animated: Bool) {
            let area = MapArea(mapView.region)
            let byUser = userMoving
            userMoving = false
            // Not during a SwiftUI update, which may have moved the map.
            Task { @MainActor [weak self] in
                self?.parent?.onCameraEnd(area, byUser)
            }
        }

        /// True while a pan or pinch of the map's own gesture recognisers is under way.
        private static func fingerOnMap(_ mapView: MKMapView) -> Bool {
            let recognizers = (mapView.gestureRecognizers ?? []) + mapView.subviews.flatMap { $0.gestureRecognizers ?? [] }
            return recognizers.contains { $0.state == .began || $0.state == .changed }
        }

        // MARK: Taps

        @objc func tapped(_ recognizer: UITapGestureRecognizer) {
            guard let mapView = recognizer.view as? MKMapView, recognizer.state == .ended else { return }
            let coordinate = mapView.convert(recognizer.location(in: mapView), toCoordinateFrom: mapView)
            parent?.onTap(coordinate)
        }

        /// The map keeps its own gestures, such as a double tap to zoom.
        func gestureRecognizer(
            _ gestureRecognizer: UIGestureRecognizer,
            shouldRecognizeSimultaneouslyWith otherGestureRecognizer: UIGestureRecognizer
        ) -> Bool {
            true
        }
    }
}
