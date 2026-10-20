package com.bvelop.slipways.ui.details

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.createSavedStateHandle
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.bvelop.slipways.shared.ApiException
import com.bvelop.slipways.shared.NearestGauge
import com.bvelop.slipways.shared.SlipwayDetails
import com.bvelop.slipways.shared.SlipwaysApi
import com.bvelop.slipways.ui.UiState
import com.bvelop.slipways.ui.container
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

class SlipwayDetailsViewModel(savedStateHandle: SavedStateHandle, private val api: SlipwaysApi) : ViewModel() {
    private val id: String = checkNotNull(savedStateHandle["id"])

    private val _state = MutableStateFlow<UiState<SlipwayDetails>>(UiState.Loading)
    val state: StateFlow<UiState<SlipwayDetails>> = _state.asStateFlow()

    /** The gauge with a current reading closest to the slipway, from the water the slipway is on. */
    private val _nearestGauge = MutableStateFlow<NearestGauge?>(null)
    val nearestGauge: StateFlow<NearestGauge?> = _nearestGauge.asStateFlow()

    init {
        load()
    }

    fun load() {
        _state.value = UiState.Loading
        viewModelScope.launch {
            val slipway = try {
                api.slipway(id)
            } catch (e: CancellationException) {
                throw e
            } catch (e: ApiException) {
                _state.value = UiState.Error(if (e.status == 404) "Diese Slipanlage gibt es nicht." else "Fehler ${e.status}")
                return@launch
            } catch (e: Exception) {
                _state.value = UiState.Error(e.message ?: "Unbekannter Fehler")
                return@launch
            }
            _state.value = UiState.Success(slipway)
            val waterId = slipway.water?.id ?: return@launch
            _nearestGauge.value = try {
                api.water(waterId).nearestGauge(slipway.latitude, slipway.longitude)
            } catch (e: CancellationException) {
                throw e
            } catch (_: Exception) {
                null
            }
        }
    }

    companion object {
        val Factory = viewModelFactory {
            initializer { SlipwayDetailsViewModel(createSavedStateHandle(), container.api) }
        }
    }
}
