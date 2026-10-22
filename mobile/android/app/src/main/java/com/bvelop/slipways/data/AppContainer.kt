package com.bvelop.slipways.data

import android.content.Context
import androidx.core.content.edit
import com.bvelop.slipways.BuildConfig
import com.bvelop.slipways.shared.SlipwaysApi
import com.bvelop.slipways.shared.SuggestionStore
import io.ktor.client.HttpClient
import io.ktor.client.engine.okhttp.OkHttp

/** Manual dependency container; created once per process by [com.bvelop.slipways.SlipwaysApplication]. */
class AppContainer(context: Context) {
    val api = SlipwaysApi(baseUrl = BuildConfig.API_BASE_URL, httpClient = HttpClient(OkHttp))

    private val preferences = context.getSharedPreferences("slipways", Context.MODE_PRIVATE)

    /** Suggestions sent from this device; they stay on it. */
    val suggestions = SuggestionStore(
        read = { preferences.getString(SUGGESTIONS, null) },
        write = { preferences.edit { putString(SUGGESTIONS, it) } },
    )

    private companion object {
        const val SUGGESTIONS = "slipways-suggestions"
    }
}
