package com.bvelop.slipways.ui.overview

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Clear
import androidx.compose.material.icons.filled.RestartAlt
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
import androidx.compose.runtime.LaunchedEffect
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
import com.bvelop.slipways.shared.Countries
import com.bvelop.slipways.shared.CountryCount
import com.bvelop.slipways.shared.Format
import com.bvelop.slipways.shared.MapArea
import com.bvelop.slipways.shared.Overview
import com.bvelop.slipways.shared.Position
import com.bvelop.slipways.ui.Centered
import com.bvelop.slipways.ui.CountryMenu
import com.bvelop.slipways.ui.LoadFailed
import com.bvelop.slipways.ui.OsmAttribution
import com.bvelop.slipways.ui.map.MapTarget
import com.bvelop.slipways.ui.UiState

/**
 * Map with search and choice of country for one kind of place; the list below shows only what lies in the visible
 * area or matches the search.
 */
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
    country: (T) -> String?,
    onSelect: (T) -> Unit,
    /** Shows a plus button that suggests a new entry in the visible area. */
    onAdd: ((MapArea) -> Unit)? = null,
    row: @Composable (T) -> Unit,
) {
    val items by viewModel.items.collectAsStateWithLifecycle()
    val query by viewModel.query.collectAsStateWithLifecycle()
    val area by viewModel.area.collectAsStateWithLifecycle()
    val results by viewModel.results.collectAsStateWithLifecycle()
    val chosen by viewModel.country.collectAsStateWithLifecycle()
    // The field keeps its own text, so typing never waits for the view model.
    var text by rememberSaveable { mutableStateOf("") }

    val all = (items as? UiState.Success)?.data
    val searching = query.isNotEmpty()
    // Counted over everything, so the choice keeps its numbers while a search narrows the map.
    val countries = remember(all) {
        Countries.group(all.orEmpty(), country, position).map { CountryCount(it.code, it.items.size) }
    }
    val candidates = remember(all, searching, results, chosen) {
        val shown = if (searching) results.orEmpty() else all.orEmpty()
        if (chosen == null) shown else shown.filter { country(it) == chosen }
    }
    val pins = remember(candidates) { candidates.map { MapPin(id(it), position(it)) } }
    val bubbles = remember(candidates) {
        if (candidates.any { country(it) == null }) {
            emptyList()
        } else {
            Countries.group(candidates, country, position).map { CountryBubble(it.code, it.items.size, it.center, it.area) }
        }
    }
    // Null when the area holds too many entries to list.
    val listed: List<T>? = remember(all, searching, candidates, area) {
        when {
            all == null -> emptyList()
            searching -> candidates
            else -> Overview.visible(candidates, area, position = position)
        }
    }
    // A finished search moves the map to its hits, a chosen country to that country.
    var fitTo by remember { mutableStateOf<MapTarget?>(null) }
    LaunchedEffect(results) {
        if (results != null) MapArea.fitting(candidates.map(position))?.let { fitTo = MapTarget(it) }
    }
    val chooseCountry = { code: String? ->
        viewModel.onCountryChange(code)
        val base = if (searching) results.orEmpty() else all.orEmpty()
        val points = base.filter { code == null || country(it) == code }.map(position)
        val target = when {
            searching -> MapArea.fitting(points)
            code != null -> Countries.coreArea(points)
            else -> MapArea.START
        }
        // Back to Europe exactly as at start, so the map counts as unmoved again.
        if (target != null) fitTo = MapTarget(target, padding = if (target == MapArea.START) 0 else 48)
    }
    // The first view the map reports is its start; any other view offers the reset button.
    var start by remember { mutableStateOf<Pair<MapArea, Double>?>(null) }
    var moved by remember { mutableStateOf(false) }
    val onView = { visible: MapArea, zoom: Double ->
        val first = start ?: (visible to zoom).also { start = it }
        moved = !visible.sameView(zoom, first.first, first.second)
    }
    val canReset = text.isNotEmpty() || chosen != null || moved
    // Back to how the screen opened: no search, all countries, Europe.
    val reset = {
        text = ""
        viewModel.onQueryChange("")
        viewModel.onCountryChange(null)
        fitTo = MapTarget(MapArea.START, padding = 0)
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(title) },
                actions = {
                    if (onAdd != null) {
                        IconButton(onClick = { onAdd(area) }) { Icon(Icons.Filled.Add, contentDescription = "$singular fehlt?") }
                    }
                },
            )
        },
    ) { padding ->
        Column(Modifier.padding(padding).fillMaxSize()) {
            Row(
                Modifier.fillMaxWidth().padding(start = 16.dp, end = 8.dp, top = 8.dp, bottom = 8.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                OutlinedTextField(
                    value = text,
                    onValueChange = {
                        text = it
                        viewModel.onQueryChange(it)
                    },
                    modifier = Modifier.weight(1f),
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
                CountryMenu(countries, chosen, chooseCountry)
                if (canReset) {
                    IconButton(onClick = reset) { Icon(Icons.Filled.RestartAlt, contentDescription = "Zurücksetzen") }
                }
            }
            OverviewMap(
                pins = pins,
                countries = bubbles,
                color = color,
                fitTo = fitTo,
                onAreaChange = { visible, zoom ->
                    viewModel.onAreaChange(visible)
                    onView(visible, zoom)
                },
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
                    Hint("Zoome hinein oder suche", "${Format.count(candidates.size, singular, plural)}, zu viele für eine Liste.")
                } else if (shown.isEmpty()) {
                    Hint(
                        if (searching) "Keine Treffer" else "Hier gibt es keine $plural",
                        if (searching) "Versuche einen anderen Suchbegriff." else "Verschiebe die Karte oder suche.",
                    )
                } else {
                    LazyColumn {
                        item(key = "header") {
                            ListHeader(
                                if (searching) Format.count(shown.size, "Treffer", "Treffer")
                                else "${Format.count(shown.size, singular, plural)} im Kartenausschnitt",
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
internal fun ListHeader(text: String) {
    Text(
        text,
        modifier = Modifier.padding(start = 16.dp, end = 16.dp, top = 16.dp, bottom = 4.dp),
        style = MaterialTheme.typography.titleSmall,
        color = MaterialTheme.colorScheme.primary,
    )
}

@Composable
internal fun Hint(title: String, text: String) {
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
