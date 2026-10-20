package com.bvelop.slipways.ui.account

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.bvelop.slipways.shared.ApiException
import com.bvelop.slipways.shared.Session
import com.bvelop.slipways.shared.User
import com.bvelop.slipways.ui.container
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

class AccountViewModel(private val session: Session) : ViewModel() {
    val user: StateFlow<User?> = session.user

    private val _busy = MutableStateFlow(false)
    val busy: StateFlow<Boolean> = _busy.asStateFlow()

    private val _error = MutableStateFlow<String?>(null)
    val error: StateFlow<String?> = _error.asStateFlow()

    fun submit(email: String, password: String, register: Boolean) {
        if (_busy.value) return
        _busy.value = true
        _error.value = null
        viewModelScope.launch {
            try {
                if (register) session.register(email.trim(), password) else session.login(email.trim(), password)
            } catch (e: ApiException) {
                _error.value = when {
                    !register && e.status == 401 -> "E-Mail oder Passwort ist falsch."
                    register && e.status == 400 -> "Registrierung fehlgeschlagen. Ist die E-Mail schon vergeben oder das Passwort zu schwach?"
                    else -> "Fehler ${e.status}"
                }
            } catch (e: Exception) {
                _error.value = e.message ?: "Unbekannter Fehler"
            } finally {
                _busy.value = false
            }
        }
    }

    fun logout() = session.logout()

    companion object {
        val Factory = viewModelFactory {
            initializer { AccountViewModel(container.session) }
        }
    }
}
