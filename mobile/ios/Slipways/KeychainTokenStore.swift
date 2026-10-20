import Foundation
import Security
import SlipwaysKit

/// Keeps the bearer token in the Keychain.
struct KeychainTokenStore: TokenStorage {
    private var query: [String: Any] {
        [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: "com.bvelop.slipways",
            kSecAttrAccount as String: "access_token",
        ]
    }

    func token() -> String? {
        var result: AnyObject?
        var lookup = query
        lookup[kSecReturnData as String] = true
        lookup[kSecMatchLimit as String] = kSecMatchLimitOne
        guard SecItemCopyMatching(lookup as CFDictionary, &result) == errSecSuccess,
              let data = result as? Data else { return nil }
        return String(data: data, encoding: .utf8)
    }

    func save(_ token: String?) {
        SecItemDelete(query as CFDictionary)
        guard let token else { return }
        var item = query
        item[kSecValueData as String] = Data(token.utf8)
        item[kSecAttrAccessible as String] = kSecAttrAccessibleAfterFirstUnlock
        SecItemAdd(item as CFDictionary, nil)
    }
}
