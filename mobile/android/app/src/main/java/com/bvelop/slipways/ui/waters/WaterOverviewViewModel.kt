package com.bvelop.slipways.ui.waters

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.bvelop.slipways.shared.MapArea
import com.bvelop.slipways.shared.ShapeArea
import com.bvelop.slipways.shared.SlipwaysApi
import com.bvelop.slipways.shared.WaterShapes
import com.bvelop.slipways.shared.WaterSummary
import com.bvelop.slipways.ui.UiState
import com.bvelop.slipways.ui.container
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.FlowPreview
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.debounce
import kotlinx.coroutines.flow.distinctUntilChanged
import kotlinx.coroutines.flow.filterNotNull
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.mapLatest
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch

/** Which shapes the map loads: those of the search hits, or those in the visible area. */
sealed interface ShapeFilter {
    data class Search(val term: String) : ShapeFilter
    data class Area(val area: ShapeArea) : ShapeFilter
}

/**
 * The waters overview: every water loaded once, the search through the API, and the shapes for the map. Thousands of
 * shapes are too much to load at once, so the map loads those in its view, further out only the large ones.
 */
@OptIn(FlowPreview::class, ExperimentalCoroutinesApi::class)
class WaterOverviewViewModel(private val api: SlipwaysApi) : ViewModel() {
    private val _items = MutableStateFlow<UiState<List<WaterSummary>>>(UiState.Loading)
    val items: StateFlow<UiState<List<WaterSummary>>> = _items.asStateFlow()

    private val typed = MutableStateFlow("")

    /** The search term once typing has paused. */
    val query: StateFlow<String> =
        typed.map { it.trim() }
            .debounce { if (it.isEmpty()) 0L else 350L }
            .distinctUntilChanged()
            .stateIn(viewModelScope, SharingStarted.Eagerly, "")

    /** Hits for [query]; null without a search term. Old hits stay until new ones arrive. */
    val results: StateFlow<List<WaterSummary>?> =
        query.mapLatest { term ->
            if (term.isEmpty()) {
                null
            } else {
                try {
                    api.waters(term)
                } catch (e: CancellationException) {
                    throw e
                } catch (_: Exception) {
                    emptyList()
                }
            }
        }.stateIn(viewModelScope, SharingStarted.Eagerly, null)

    private val _area = MutableStateFlow(MapArea.START)
    val area: StateFlow<MapArea> = _area.asStateFlow()

    /** As the website counts it; null until the map reports its first view. */
    private val zoom = MutableStateFlow<Double?>(null)

    private val _country = MutableStateFlow<String?>(null)
    val country: StateFlow<String?> = _country.asStateFlow()

    /** Shapes for the map; the old ones stay until new ones arrive, so nothing flickers. */
    val shapes: StateFlow<WaterShapes?> =
        combine(query, _area, zoom) { term, area, zoom ->
            when {
                term.isNotEmpty() -> ShapeFilter.Search(term)
                zoom != null -> ShapeFilter.Area(ShapeArea.of(area, zoom))
                else -> null
            }
        }
            .filterNotNull()
            .distinctUntilChanged()
            .mapLatest { filter ->
                try {
                    when (filter) {
                        is ShapeFilter.Search -> api.waterShapes(filter.term)
                        is ShapeFilter.Area -> api.waterShapes(filter.area)
                    }
                } catch (e: CancellationException) {
                    throw e
                } catch (_: Exception) {
                    // Without shapes the list still works.
                    null
                }
            }
            .filterNotNull()
            .stateIn(viewModelScope, SharingStarted.Eagerly, null)

    init {
        load()
    }

    fun load() {
        _items.value = UiState.Loading
        viewModelScope.launch {
            _items.value = try {
                UiState.Success(api.waters())
            } catch (e: CancellationException) {
                throw e
            } catch (e: Exception) {
                UiState.Error(e.message ?: "Unbekannter Fehler")
            }
        }
    }

    fun onQueryChange(value: String) {
        typed.value = value
    }

    fun onViewChange(area: MapArea, zoom: Double) {
        _area.value = area
        this.zoom.value = zoom
    }

    fun onCountryChange(value: String?) {
        _country.value = value
    }

    companion object {
        val Factory = viewModelFactory {
            initializer { WaterOverviewViewModel(container.api) }
        }
    }
}
