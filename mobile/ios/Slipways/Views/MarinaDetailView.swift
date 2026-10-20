import MapKit
import SlipwaysKit
import SwiftUI

/// Short overview of a marina, shown as a sheet over the map.
struct MarinaDetailView: View {
    let marina: Marina

    @Environment(\.dismiss) private var dismiss

    private var coordinate: CLLocationCoordinate2D {
        CLLocationCoordinate2D(latitude: marina.latitude, longitude: marina.longitude)
    }

    var body: some View {
        List {
            Section {
                Map(initialPosition: .region(MKCoordinateRegion(center: coordinate, latitudinalMeters: 1500, longitudinalMeters: 1500))) {
                    Marker(marina.name, systemImage: "ferry", coordinate: coordinate).tint(.teal)
                }
                .frame(height: 200)
                .osmAttribution(marina.source == .osm)
                .listRowInsets(EdgeInsets())
            }

            Section {
                if let water = marina.water {
                    NavigationLink(value: water) {
                        Label { Text(water.name) } icon: { Image(systemName: "water.waves") }
                    }
                }
                Fact(label: "Adresse", systemImage: "mappin.and.ellipse", value: marina.address)
                Fact(label: "Betreiber", systemImage: "building.2", value: marina.operator)
                Fact(label: "Gebühr", systemImage: "eurosign.circle", value: marina.fee.map(Labels.fee))
                Fact(label: "Zugang", systemImage: "door.left.hand.open", value: marina.access.map(Labels.access))
                Fact(label: "Öffnungszeiten", systemImage: "clock", value: marina.openingHours)
                if let text = marina.url, let url = Links.web(text) {
                    Link(destination: url) { Label(text, systemImage: "link") }
                }
                if let phone = marina.phone, let url = Links.phone(phone) {
                    Link(destination: url) { Label(phone, systemImage: "phone") }
                }
                if let email = marina.email, let url = Links.mail(email) {
                    Link(destination: url) { Label(email, systemImage: "envelope") }
                }
            } footer: {
                if marina.source == .osm { OSMAttribution() } else { Text("Quelle: eigene Erfassung") }
            }

            Section {
                Button("Route planen", systemImage: "car") {
                    mapItem.openInMaps(launchOptions: [MKLaunchOptionsDirectionsModeKey: MKLaunchOptionsDirectionsModeDriving])
                }
                Button("In Karten öffnen", systemImage: "map") { mapItem.openInMaps() }
                if let element = marina.osmElement, let url = Links.osm(element) {
                    Link(destination: url) { Label("In OpenStreetMap ansehen", systemImage: "globe") }
                }
            }
        }
        .navigationTitle(marina.name)
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItem(placement: .confirmationAction) { Button("Fertig") { dismiss() } }
        }
    }

    private var mapItem: MKMapItem {
        let item = MKMapItem(placemark: MKPlacemark(coordinate: coordinate))
        item.name = marina.name
        return item
    }
}
