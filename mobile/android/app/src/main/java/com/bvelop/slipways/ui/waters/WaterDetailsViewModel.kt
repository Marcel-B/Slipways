package com.bvelop.slipways.ui.waters

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.createSavedStateHandle
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.bvelop.slipways.shared.ApiException
import com.bvelop.slipways.shared.SlipwaysApi
import com.bvelop.slipways.shared.WaterDetails
import com.bvelop.slipways.ui.UiState
import com.bvelop.slipways.ui.container
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

class WaterDetailsViewModel(savedStateHandle: SavedStateHandle, private val api: SlipwaysApi) : ViewModel() {
    private val id: String = checkNotNull(savedStateHandle["id"])

    private val _state = MutableStateFlow<UiState<WaterDetails>>(UiState.Loading)
    val state: StateFlow<UiState<WaterDetails>> = _state.asStateFlow()

    init {
        load()
    }

    fun load() {
        _state.value = UiState.Loading
        viewModelScope.launch {
            _state.value = try {
                UiState.Success(api.water(id))
            } catch (e: CancellationException) {
                throw e
            } catch (e: ApiException) {
                UiState.Error(if (e.status == 404) "Dieses Gewässer gibt es nicht." else "Fehler ${e.status}")
            } catch (e: Exception) {
                UiState.Error(e.message ?: "Unbekannter Fehler")
            }
        }
    }

    companion object {
        val Factory = viewModelFactory {
            initializer { WaterDetailsViewModel(createSavedStateHandle(), container.api) }
        }
    }
}
