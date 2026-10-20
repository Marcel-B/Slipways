import SlipwaysKit
import SwiftUI

struct AccountView: View {
    @Environment(Session.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var email = ""
    @State private var password = ""
    @State private var registering = false
    @State private var busy = false
    @State private var message: String?

    var body: some View {
        NavigationStack {
            Form {
                if let user = session.user {
                    Section {
                        LabeledContent("Angemeldet als", value: user.displayName ?? user.email)
                        Button("Abmelden", role: .destructive) { session.logout() }
                    }
                } else {
                    Section(registering ? "Registrieren" : "Anmelden") {
                        TextField("E-Mail", text: $email)
                            .textContentType(.emailAddress)
                            .keyboardType(.emailAddress)
                            .textInputAutocapitalization(.never)
                            .autocorrectionDisabled()
                        SecureField("Passwort", text: $password)
                            .textContentType(registering ? .newPassword : .password)
                        if registering {
                            Text("Mindestens 8 Zeichen").font(.footnote).foregroundStyle(.secondary)
                        }
                        if let message {
                            Text(message).foregroundStyle(.red)
                        }
                        Button(registering ? "Konto anlegen" : "Anmelden") { Task { await submit() } }
                            .disabled(busy || email.isEmpty || password.count < (registering ? 8 : 1))
                    }
                    Section {
                        Button(registering ? "Schon registriert? Anmelden" : "Noch kein Konto? Registrieren") {
                            registering.toggle()
                            message = nil
                        }
                    }
                }
            }
            .navigationTitle("Konto")
            .toolbar {
                ToolbarItem(placement: .confirmationAction) {
                    Button("Fertig") { dismiss() }
                }
            }
        }
    }

    private func submit() async {
        busy = true
        defer { busy = false }
        message = nil
        let email = email.trimmingCharacters(in: .whitespaces)
        do {
            if registering {
                try await session.register(email: email, password: password)
            } else {
                try await session.login(email: email, password: password)
            }
            dismiss()
        } catch let error as APIError where error.status == 401 {
            message = "E-Mail oder Passwort ist falsch."
        } catch let error as APIError where error.status == 400 {
            message = "Registrierung fehlgeschlagen. Ist die E-Mail schon vergeben oder das Passwort zu schwach?"
        } catch {
            message = "Verbindung fehlgeschlagen."
        }
    }
}
