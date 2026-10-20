package com.bvelop.slipways.ui.overview

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.bvelop.slipways.shared.MapArea
import com.bvelop.slipways.shared.Marina
import com.bvelop.slipways.shared.SlipwaySummary
import com.bvelop.slipways.shared.SlipwaysApi
import com.bvelop.slipways.ui.UiState
import com.bvelop.slipways.ui.container
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.FlowPreview
import kotlinx.coroutines.async
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.debounce
import kotlinx.coroutines.flow.distinctUntilChanged
import kotlinx.coroutines.flow.flatMapLatest
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch

/** Everything the overview map filters locally; marinas stay empty when they could not be loaded. */
data class Places(val slipways: List<SlipwaySummary>, val marinas: List<Marina>)

@OptIn(FlowPreview::class, ExperimentalCoroutinesApi::class)
class OverviewViewModel(private val api: SlipwaysApi) : ViewModel() {
    private val _places = MutableStateFlow<UiState<Places>>(UiState.Loading)
    val places: StateFlow<UiState<Places>> = _places.asStateFlow()

    private val _query = MutableStateFlow("")
    val query: StateFlow<String> = _query.asStateFlow()

    private val _area = MutableStateFlow(MapArea.START)
    val area: StateFlow<MapArea> = _area.asStateFlow()

    /** Slipways matching the search from the API; null without a search term or while it loads. */
    val results: StateFlow<List<SlipwaySummary>?> =
        _query.map { it.trim() }.distinctUntilChanged().debounce(300L)
            .flatMapLatest { search ->
                flow {
                    emit(null)
                    if (search.isNotEmpty()) {
                        emit(
                            try {
                                api.slipways(search)
                            } catch (e: CancellationException) {
                                throw e
                            } catch (_: Exception) {
                                emptyList()
                            },
                        )
                    }
                }
            }
            .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), null)

    init {
        load()
    }

    fun load() {
        _places.value = UiState.Loading
        viewModelScope.launch {
            val marinas = async {
                try {
                    api.marinas()
                } catch (e: CancellationException) {
                    throw e
                } catch (_: Exception) {
                    emptyList()
                }
            }
            _places.value = try {
                UiState.Success(Places(api.slipways(), marinas.await()))
            } catch (e: CancellationException) {
                throw e
            } catch (e: Exception) {
                UiState.Error(e.message ?: "Unbekannter Fehler")
            }
        }
    }

    fun onQueryChange(value: String) {
        _query.value = value
    }

    fun onAreaChange(value: MapArea) {
        _area.value = value
    }

    companion object {
        val Factory = viewModelFactory {
            initializer { OverviewViewModel(container.api) }
        }
    }
}
