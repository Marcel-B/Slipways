import SlipwaysKit
import SwiftUI

/// Search field, choice of country and an optional plus button floating above a full-screen map.
struct MapSearchBar: View {
    @Binding var text: String
    let prompt: String
    let countries: [CountryCount]
    @Binding var country: String?
    var focused: FocusState<Bool>.Binding
    var addLabel: String? = nil
    var onAdd: (() -> Void)? = nil

    var body: some View {
        HStack(spacing: 8) {
            HStack(spacing: 6) {
                Image(systemName: "magnifyingglass")
                    .foregroundStyle(.secondary)
                    .accessibilityHidden(true)
                TextField(prompt, text: $text)
                    .focused(focused)
                    .submitLabel(.search)
                    .autocorrectionDisabled()
                    .onSubmit { focused.wrappedValue = false }
                if !text.isEmpty {
                    Button("Suche löschen", systemImage: "xmark.circle.fill") { text = "" }
                        .labelStyle(.iconOnly)
                        .foregroundStyle(.secondary)
                }
            }
            .padding(.horizontal, 12)
            .floating()
            CountryMenu(options: countries, selection: $country)
            if let addLabel, let onAdd {
                Button { onAdd() } label: {
                    Image(systemName: "plus")
                        .font(.body.bold())
                        .floating()
                }
                .accessibilityLabel(addLabel)
            }
        }
        .padding(.horizontal, 12)
        .padding(.top, 8)
    }
}

/// One line at the bottom of a full-screen map: loading, a failure or what a search found, with an optional button.
struct MapNotice: View {
    let text: String
    var systemImage: String? = nil
    var loading = false
    var actionTitle: String? = nil
    var action: (() -> Void)? = nil

    var body: some View {
        HStack(spacing: 8) {
            if loading {
                ProgressView()
            } else if let systemImage {
                Image(systemName: systemImage).foregroundStyle(.secondary)
            }
            Text(text)
            if let actionTitle, let action {
                Button(actionTitle, action: action).bold()
            }
        }
        .font(.subheadline)
        .padding(.horizontal, 14)
        .padding(.vertical, 10)
        .background(.regularMaterial, in: .capsule)
        .shadow(color: .black.opacity(0.15), radius: 4, y: 1)
        .padding(.horizontal, 12)
        // Leaves room for the attribution in the corner.
        .padding(.bottom, 32)
    }
}

/// Chooses one country or all; hidden while there is nothing to choose between. Shows the code once one is chosen.
struct CountryMenu: View {
    let options: [CountryCount]
    @Binding var selection: String?

    var body: some View {
        if options.count >= 2 {
            Menu {
                Picker("Land", selection: $selection) {
                    Text("Alle Länder").tag(String?.none)
                    ForEach(options) { option in
                        Text(option.label).tag(String?.some(option.code))
                    }
                }
            } label: {
                HStack(spacing: 4) {
                    Image(systemName: selection == nil ? "globe.europe.africa" : "globe.europe.africa.fill")
                    if let selection {
                        Text(selection).font(.subheadline.bold())
                    }
                }
                .padding(.horizontal, selection == nil ? 0 : 10)
                .floating()
            }
            .accessibilityLabel("Land")
            .accessibilityValue(selection.map(Countries.name) ?? "Alle Länder")
        }
    }
}

extension View {
    /// A control on the map: at least a fingertip in size, on material, with a light shadow.
    func floating() -> some View {
        frame(minWidth: 44, minHeight: 44)
            .background(.regularMaterial, in: .capsule)
            .shadow(color: .black.opacity(0.15), radius: 4, y: 1)
    }
}
