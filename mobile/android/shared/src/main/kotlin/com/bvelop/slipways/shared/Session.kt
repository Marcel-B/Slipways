package com.bvelop.slipways.shared

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

/** Where the bearer token is persisted between app starts. */
interface TokenStorage {
    fun token(): String?
    fun save(token: String?)
}

/** The signed-in user. */
class Session(
    private val api: SlipwaysApi,
    private val tokens: TokenStorage,
) {
    private val _user = MutableStateFlow<User?>(null)
    val user: StateFlow<User?> = _user.asStateFlow()

    /** Loads the user for a token stored by an earlier app start. */
    suspend fun restore() {
        if (tokens.token() == null) return
        try {
            _user.value = api.me()
        } catch (e: ApiException) {
            if (e.status == 401) tokens.save(null)
        } catch (_: Exception) {
            // Offline: keep the token and try again on the next start.
        }
    }

    suspend fun login(email: String, password: String) {
        val token = api.login(email, password)
        tokens.save(token.accessToken)
        _user.value = api.me()
    }

    suspend fun register(email: String, password: String) {
        api.register(email, password)
        login(email, password)
    }

    fun logout() {
        tokens.save(null)
        _user.value = null
    }
}
