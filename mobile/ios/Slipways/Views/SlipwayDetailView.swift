import MapKit
import SlipwaysKit
import SwiftUI

struct SlipwayDetailView: View {
    let slipway: SlipwaySummary

    @Environment(\.api) private var api
    @State private var details: SlipwayDetails?
    @State private var failed = false

    private var coordinate: CLLocationCoordinate2D {
        CLLocationCoordinate2D(latitude: slipway.latitude, longitude: slipway.longitude)
    }

    var body: some View {
        List {
            Section {
                Map(initialPosition: .region(MKCoordinateRegion(center: coordinate, latitudinalMeters: 1500, longitudinalMeters: 1500))) {
                    Marker(slipway.name, systemImage: "sailboat", coordinate: coordinate)
                }
                .frame(height: 240)
                .listRowInsets(EdgeInsets())
            }

            Section {
                LabeledContent("Gewässer", value: slipway.water.name)
                if let details {
                    if !details.address.isEmpty { LabeledContent("Adresse", value: details.address) }
                    LabeledContent("Kosten", value: costs(details.costs))
                    if let marina = details.marina { LabeledContent("Marina", value: marina.name) }
                    if !details.extras.isEmpty { LabeledContent("Extras", value: details.extras.joined(separator: ", ")) }
                } else if failed {
                    Text("Details konnten nicht geladen werden.").foregroundStyle(.secondary)
                } else {
                    ProgressView()
                }
            }

            if let comment = details?.comment {
                Section("Hinweise") { Text(comment) }
            }

            Section {
                Button("In Karten öffnen", systemImage: "map") {
                    let item = MKMapItem(placemark: MKPlacemark(coordinate: coordinate))
                    item.name = slipway.name
                    item.openInMaps()
                }
            }
        }
        .navigationTitle(slipway.name)
        .navigationBarTitleDisplayMode(.inline)
        .task {
            do {
                details = try await api.slipway(id: slipway.id)
            } catch {
                failed = true
            }
        }
    }

    private func costs(_ value: Double?) -> String {
        guard let value else { return "unbekannt" }
        return value == 0 ? "kostenlos" : value.formatted(.currency(code: "EUR"))
    }
}
