package com.bvelop.slipways.data

import android.content.Context
import com.bvelop.slipways.BuildConfig
import com.bvelop.slipways.shared.Session
import com.bvelop.slipways.shared.SlipwaysApi
import io.ktor.client.HttpClient
import io.ktor.client.engine.okhttp.OkHttp
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch

/** Manual dependency container; created once per process by [com.bvelop.slipways.SlipwaysApplication]. */
class AppContainer(context: Context) {
    private val tokenStore = TokenStore(context)
    private val appScope = CoroutineScope(SupervisorJob())

    val api = SlipwaysApi(
        baseUrl = BuildConfig.API_BASE_URL,
        httpClient = HttpClient(OkHttp),
        tokenProvider = tokenStore::token,
    )

    val session = Session(api, tokenStore).also { session ->
        appScope.launch { session.restore() }
    }
}
