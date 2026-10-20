package com.bvelop.slipways.ui.list

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import com.bvelop.slipways.shared.SlipwaySummary
import com.bvelop.slipways.shared.SlipwaysApi
import com.bvelop.slipways.ui.UiState
import com.bvelop.slipways.ui.container
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.FlowPreview
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.debounce
import kotlinx.coroutines.flow.distinctUntilChanged
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.flatMapLatest
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn

@OptIn(FlowPreview::class, ExperimentalCoroutinesApi::class)
class SlipwayListViewModel(private val api: SlipwaysApi) : ViewModel() {
    private val _query = MutableStateFlow("")
    val query: StateFlow<String> = _query.asStateFlow()

    private val reload = MutableStateFlow(0)

    val state: StateFlow<UiState<List<SlipwaySummary>>> =
        combine(_query.debounce(SEARCH_DEBOUNCE_MS).map { it.trim() }.distinctUntilChanged(), reload) { search, _ -> search }
            .flatMapLatest { search ->
                flow {
                    emit(UiState.Loading)
                    emit(
                        try {
                            UiState.Success(api.slipways(search))
                        } catch (e: Exception) {
                            UiState.Error(e.message ?: "Unbekannter Fehler")
                        },
                    )
                }
            }
            .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), UiState.Loading)

    fun onQueryChange(value: String) {
        _query.value = value
    }

    fun retry() {
        reload.value++
    }

    companion object {
        const val SEARCH_DEBOUNCE_MS = 300L

        val Factory = viewModelFactory {
            initializer { SlipwayListViewModel(container.api) }
        }
    }
}
