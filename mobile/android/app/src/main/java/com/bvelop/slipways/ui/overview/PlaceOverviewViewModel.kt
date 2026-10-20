package com.bvelop.slipways.ui.overview

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.bvelop.slipways.shared.MapArea
import com.bvelop.slipways.shared.Marina
import com.bvelop.slipways.shared.Overview
import com.bvelop.slipways.shared.SlipwaySummary
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
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.mapLatest
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch

/**
 * One kind of place for the overview map: everything loaded once, the visible area and the search.
 * [find] gets the search term and everything loaded, for places that are searched locally.
 */
@OptIn(FlowPreview::class, ExperimentalCoroutinesApi::class)
class PlaceOverviewViewModel<T : Any>(
    private val loadAll: suspend () -> List<T>,
    private val find: suspend (String, List<T>) -> List<T>,
) : ViewModel() {
    private val _items = MutableStateFlow<UiState<List<T>>>(UiState.Loading)
    val items: StateFlow<UiState<List<T>>> = _items.asStateFlow()

    private val typed = MutableStateFlow("")

    /** The search term once typing has paused; everything shown follows this, not every keystroke. */
    val query: StateFlow<String> =
        typed.map { it.trim() }
            .debounce { if (it.isEmpty()) 0L else 350L }
            .distinctUntilChanged()
            .stateIn(viewModelScope, SharingStarted.Eagerly, "")

    private val _area = MutableStateFlow(MapArea.START)
    val area: StateFlow<MapArea> = _area.asStateFlow()

    /** Hits for [query]; null without a search term. Old hits stay until new ones arrive, so nothing flickers. */
    val results: StateFlow<List<T>?> =
        combine(query, _items) { term, state -> term to (state as? UiState.Success)?.data }
            .distinctUntilChanged()
            .mapLatest { (term, all) ->
                if (term.isEmpty() || all == null) {
                    null
                } else {
                    try {
                        find(term, all)
                    } catch (e: CancellationException) {
                        throw e
                    } catch (_: Exception) {
                        emptyList()
                    }
                }
            }
            .stateIn(viewModelScope, SharingStarted.Eagerly, null)

    init {
        load()
    }

    fun load() {
        _items.value = UiState.Loading
        viewModelScope.launch {
            _items.value = try {
                UiState.Success(loadAll())
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

    fun onAreaChange(value: MapArea) {
        _area.value = value
    }

    companion object {
        val Slipways = viewModelFactory {
            initializer {
                val api = container.api
                PlaceOverviewViewModel<SlipwaySummary>(loadAll = { api.slipways() }, find = { term, _ -> api.slipways(term) })
            }
        }

        // The API has no marina search; a few thousand marinas are quick to filter here.
        val Marinas = viewModelFactory {
            initializer {
                val api = container.api
                PlaceOverviewViewModel<Marina>(loadAll = { api.marinas() }, find = { term, all -> all.filter { Overview.matches(it, term) } })
            }
        }
    }
}
