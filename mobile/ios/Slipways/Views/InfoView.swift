import SlipwaysKit
import SwiftUI

struct InfoView: View {
    private var version: String {
        let info = Bundle.main.infoDictionary
        let short = info?["CFBundleShortVersionString"] as? String ?? "?"
        let build = info?["CFBundleVersion"] as? String ?? "?"
        return "\(short) (\(build))"
    }

    var body: some View {
        List {
            Section {
                Text("Slipways zeigt Slipanlagen, Marinas und aktuelle Pegelstände an Gewässern in Deutschland und den Nachbarländern.")
                Link(destination: Links.website) { Label("slipways.de", systemImage: "safari") }
            }

            Section("Datenquellen") {
                VStack(alignment: .leading, spacing: 4) {
                    Text("OpenStreetMap")
                    Text("Die meisten Slipanlagen und Marinas sowie die Gewässerverläufe stammen aus OpenStreetMap. Daten © OpenStreetMap-Mitwirkende, verfügbar unter der Open Database License (ODbL).")
                        .font(.subheadline)
                        .foregroundStyle(.secondary)
                    Link("openstreetmap.org/copyright", destination: Links.osmCopyright).font(.subheadline)
                }
                VStack(alignment: .leading, spacing: 4) {
                    Text("PEGELONLINE")
                    Text("Pegel und Wasserstände: PEGELONLINE der Wasserstraßen- und Schifffahrtsverwaltung des Bundes (WSV), Datenlizenz Deutschland – Zero – Version 2.0.")
                        .font(.subheadline)
                        .foregroundStyle(.secondary)
                    Link("pegelonline.wsv.de", destination: Links.pegelonline).font(.subheadline)
                }
                VStack(alignment: .leading, spacing: 4) {
                    Text("Karten")
                    Text("Apple Maps, siehe „Rechtliches“ auf der Karte.")
                        .font(.subheadline)
                        .foregroundStyle(.secondary)
                }
            }

            Section("Lizenz") {
                Text("© 2019–2026 Marcel Benders. Die App steht unter der PolyForm Strict License 1.0.0: Nutzen ist erlaubt, Weitergeben, Ändern und kommerzielle Nutzung nur nach Rückfrage. Die Daten aus OpenStreetMap (ODbL) und PEGELONLINE (DL-DE->Zero-2.0) stehen unter ihren eigenen Lizenzen.")
                    .font(.subheadline)
                Link(destination: Links.licence) { Label("PolyForm Strict License 1.0.0", systemImage: "doc.text") }
                Link(destination: Links.licenceContact) { Label("slipways@marcelbenders.com", systemImage: "envelope") }
            }

            Section("Rechtliches") {
                Link(destination: Links.impressum) { Label("Impressum", systemImage: "building.columns") }
                Link(destination: Links.datenschutz) { Label("Datenschutz", systemImage: "hand.raised") }
                Link(destination: Links.support) { Label("Support", systemImage: "questionmark.circle") }
            }

            Section {
                LabeledContent("Version", value: version)
            }
        }
        .navigationTitle("Info")
    }
}
