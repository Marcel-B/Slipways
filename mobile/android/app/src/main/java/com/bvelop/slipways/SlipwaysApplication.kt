package com.bvelop.slipways

import android.app.Application
import com.bvelop.slipways.data.AppContainer

class SlipwaysApplication : Application() {
    lateinit var container: AppContainer
        private set

    override fun onCreate() {
        super.onCreate()
        container = AppContainer(this)
    }
}
