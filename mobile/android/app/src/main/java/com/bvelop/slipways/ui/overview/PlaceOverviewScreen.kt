package com.bvelop.slipways.ui.overview

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Clear
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.bvelop.slipways.shared.Format
import com.bvelop.slipways.shared.MapArea
import com.bvelop.slipways.shared.Overview
import com.bvelop.slipways.shared.Position
import com.bvelop.slipways.ui.Centered
import com.bvelop.slipways.ui.LoadFailed
import com.bvelop.slipways.ui.OsmAttribution
import com.bvelop.slipways.ui.UiState

/** Map with search for one kind of place; the list below shows only what lies in the visible area or matches the search. */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun <T : Any> PlaceOverviewScreen(
    title: String,
    singular: String,
    plural: String,
    color: String,
    viewModel: PlaceOverviewViewModel<T>,
    id: (T) -> String,
    position: (T) -> Position,
    onSelect: (T) -> Unit,
    row: @Composable (T) -> Unit,
) {
    val items by viewModel.items.collectAsStateWithLifecycle()
    val query by viewModel.query.collectAsStateWithLifecycle()
    val area by viewModel.area.collectAsStateWithLifecycle()
    val results by viewModel.results.collectAsStateWithLifecycle()
    // The field keeps its own text, so typing never waits for the view model.
    var text by rememberSaveable { mutableStateOf("") }

    val all = (items as? UiState.Success)?.data
    val searching = query.isNotEmpty()
    val candidates = if (searching) results.orEmpty() else all.orEmpty()
    val pins = remember(candidates) { candidates.map { MapPin(id(it), position(it)) } }
    // Null when the area holds too many entries to list.
    val listed: List<T>? = remember(all, searching, results, area) {
        when {
            all == null -> emptyList()
            searching -> results.orEmpty()
            else -> Overview.visible(all, area, position = position)
        }
    }
    // Zoom to the hits of each finished search.
    val fitTo = remember(results) { results?.let { found -> MapArea.fitting(found.map(position)) } }

    Scaffold(topBar = { TopAppBar(title = { Text(title) }) }) { padding ->
        Column(Modifier.padding(padding).fillMaxSize()) {
            OutlinedTextField(
                value = text,
                onValueChange = {
                    text = it
                    viewModel.onQueryChange(it)
                },
                modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp),
                placeholder = { Text("Nach Name, Ort oder Gewässer suchen") },
                leadingIcon = { Icon(Icons.Filled.Search, contentDescription = null) },
                trailingIcon = {
                    if (text.isNotEmpty()) {
                        IconButton(onClick = {
                            text = ""
                            viewModel.onQueryChange("")
                        }) { Icon(Icons.Filled.Clear, contentDescription = "Suche leeren") }
                    }
                },
                singleLine = true,
            )
            OverviewMap(
                pins = pins,
                color = color,
                fitTo = fitTo,
                onAreaChange = viewModel::onAreaChange,
                onPinClick = { pinId -> candidates.firstOrNull { id(it) == pinId }?.let(onSelect) },
                modifier = Modifier.fillMaxWidth().weight(1f),
            )
            Column(Modifier.fillMaxWidth().weight(1f)) {
                val shown = listed
                if (items is UiState.Error) {
                    LoadFailed("$plural konnten nicht geladen werden.", viewModel::load)
                } else if (items is UiState.Loading || (searching && results == null)) {
                    Centered { CircularProgressIndicator() }
                } else if (shown == null) {
                    Hint("Zoome hinein oder suche", "Im Kartenausschnitt liegen zu viele $plural für eine Liste.")
                } else if (shown.isEmpty()) {
                    Hint(
                        if (searching) "Keine Treffer" else "Hier gibt es keine $plural",
                        if (searching) "Versuche einen anderen Suchbegriff." else "Verschiebe die Karte oder suche.",
                    )
                } else {
                    LazyColumn {
                        item(key = "header") {
                            Text(
                                Format.count(shown.size, singular, plural),
                                modifier = Modifier.padding(start = 16.dp, end = 16.dp, top = 16.dp, bottom = 4.dp),
                                style = MaterialTheme.typography.titleSmall,
                                color = MaterialTheme.colorScheme.primary,
                            )
                        }
                        items(shown, key = id) { item ->
                            row(item)
                            HorizontalDivider()
                        }
                        item(key = "attribution") { OsmAttribution(Modifier.padding(16.dp)) }
                    }
                }
            }
        }
    }
}

@Composable
private fun Hint(title: String, text: String) {
    Centered {
        Column(horizontalAlignment = Alignment.CenterHorizontally, verticalArrangement = Arrangement.spacedBy(4.dp)) {
            Text(title, style = MaterialTheme.typography.titleMedium)
            Text(
                text,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                textAlign = TextAlign.Center,
            )
        }
    }
}
