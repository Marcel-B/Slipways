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
                Text("Slipways zeigt Slipanlagen, Marinas und aktuelle Pegelstände an Gewässern in Deutschland und weiteren Ländern Europas.")
                Link(destination: Links.website) { Label("slipways.de", systemImage: "safari") }
            }

            Section {
                NavigationLink { MySuggestionsView() } label: { Label("Meine Vorschläge", systemImage: "tray.full") }
            } footer: {
                Text("Fehlt eine Slipanlage oder stimmt etwas nicht? Vorschläge gehen ohne Konto: auf der Seite einer Slipanlage oder Marina unter „Mitmachen“, neue Slipanlagen mit dem Plus auf der Karte. Alles wird vor der Veröffentlichung geprüft.")
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
                    Text("Pegel und Wasserstände: PEGELONLINE der Wasserstraßen- und Schifffahrtsverwaltung des Bundes (WSV), Datenlizenz Deutschland – Zero – Version 2.0. In den Niederlanden: Rijkswaterstaat (offene Daten), Wasserstand über NAP.")
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
                Text("© 2019–2026 Marcel Benders. Quellcode unter der PolyForm Strict License 1.0.0. Nutzung über persönliche, nicht-kommerzielle Zwecke hinaus nur nach Rückfrage. Die Daten aus OpenStreetMap (ODbL) und PEGELONLINE (DL-DE->Zero-2.0) stehen unter ihren eigenen Lizenzen.")
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
