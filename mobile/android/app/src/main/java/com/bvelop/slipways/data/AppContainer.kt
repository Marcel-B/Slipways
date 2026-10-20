package com.bvelop.slipways.data

import com.bvelop.slipways.BuildConfig
import com.bvelop.slipways.shared.SlipwaysApi
import io.ktor.client.HttpClient
import io.ktor.client.engine.okhttp.OkHttp

/** Manual dependency container; created once per process by [com.bvelop.slipways.SlipwaysApplication]. */
class AppContainer {
    val api = SlipwaysApi(baseUrl = BuildConfig.API_BASE_URL, httpClient = HttpClient(OkHttp))
}
