package com.bvelop.slipways.ui.details

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.createSavedStateHandle
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.bvelop.slipways.shared.ApiException
import com.bvelop.slipways.shared.SlipwayDetails
import com.bvelop.slipways.shared.SlipwaysApi
import com.bvelop.slipways.ui.UiState
import com.bvelop.slipways.ui.container
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

class SlipwayDetailsViewModel(savedStateHandle: SavedStateHandle, private val api: SlipwaysApi) : ViewModel() {
    private val id: String = checkNotNull(savedStateHandle["id"])

    private val _state = MutableStateFlow<UiState<SlipwayDetails>>(UiState.Loading)
    val state: StateFlow<UiState<SlipwayDetails>> = _state.asStateFlow()

    init {
        load()
    }

    fun load() {
        _state.value = UiState.Loading
        viewModelScope.launch {
            _state.value = try {
                UiState.Success(api.slipway(id))
            } catch (e: ApiException) {
                UiState.Error(if (e.status == 404) "Diese Slipanlage gibt es nicht." else "Fehler ${e.status}")
            } catch (e: Exception) {
                UiState.Error(e.message ?: "Unbekannter Fehler")
            }
        }
    }

    companion object {
        val Factory = viewModelFactory {
            initializer { SlipwayDetailsViewModel(createSavedStateHandle(), container.api) }
        }
    }
}
