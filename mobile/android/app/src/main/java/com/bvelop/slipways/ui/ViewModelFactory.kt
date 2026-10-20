package com.bvelop.slipways.ui

import androidx.lifecycle.ViewModelProvider.AndroidViewModelFactory.Companion.APPLICATION_KEY
import androidx.lifecycle.viewmodel.CreationExtras
import com.bvelop.slipways.SlipwaysApplication
import com.bvelop.slipways.data.AppContainer

val CreationExtras.container: AppContainer
    get() = (this[APPLICATION_KEY] as SlipwaysApplication).container
