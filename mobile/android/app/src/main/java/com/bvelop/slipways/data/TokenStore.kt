package com.bvelop.slipways.data

import android.content.Context
import androidx.core.content.edit
import com.bvelop.slipways.shared.TokenStorage

/** Persists the bearer token issued by the backend. */
class TokenStore(context: Context) : TokenStorage {
    private val prefs = context.getSharedPreferences("session", Context.MODE_PRIVATE)

    override fun token(): String? = prefs.getString(KEY, null)

    override fun save(token: String?) = prefs.edit {
        if (token == null) remove(KEY) else putString(KEY, token)
    }

    private companion object {
        const val KEY = "access_token"
    }
}
