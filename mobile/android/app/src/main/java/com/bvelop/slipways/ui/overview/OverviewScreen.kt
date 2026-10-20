package com.bvelop.slipways.ui.overview

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyListScope
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalUriHandler
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.bvelop.slipways.shared.Format
import com.bvelop.slipways.shared.Links
import com.bvelop.slipways.shared.MapArea
import com.bvelop.slipways.shared.Marina
import com.bvelop.slipways.shared.Overview
import com.bvelop.slipways.shared.Position
import com.bvelop.slipways.shared.SlipwaySummary
import com.bvelop.slipways.ui.Centered
import com.bvelop.slipways.ui.LoadFailed
import com.bvelop.slipways.ui.OsmAttribution
import com.bvelop.slipways.ui.UiState

/** Map with search; the list below shows only what lies in the visible area or matches the search. */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun OverviewScreen(
    onSlipwayClick: (String) -> Unit,
    viewModel: OverviewViewModel = viewModel(factory = OverviewViewModel.Factory),
) {
    val places by viewModel.places.collectAsStateWithLifecycle()
    val query by viewModel.query.collectAsStateWithLifecycle()
    val area by viewModel.area.collectAsStateWithLifecycle()
    val results by viewModel.results.collectAsStateWithLifecycle()
    val uriHandler = LocalUriHandler.current

    val all = (places as? UiState.Success)?.data
    val searching = query.isNotBlank()
    // Null when the area holds too many entries to list.
    val slipways: List<SlipwaySummary>? = remember(all, searching, results, area) {
        when {
            all == null -> emptyList()
            searching -> results.orEmpty()
            else -> Overview.visible(all.slipways, area) { Position(it.latitude, it.longitude) }
        }
    }
    val marinas: List<Marina>? = remember(all, searching, query, area) {
        when {
            all == null -> emptyList()
            searching -> all.marinas.filter { Overview.matches(it, query) }
            else -> Overview.visible(all.marinas, area) { Position(it.latitude, it.longitude) }
        }
    }
    // Zoom to the hits of each finished search.
    val fitTo = remember(results) {
        results?.let { found ->
            MapArea.fitting(found.map { Position(it.latitude, it.longitude) } + marinas.orEmpty().map { Position(it.latitude, it.longitude) })
        }
    }
    val openMarina = { id: String ->
        all?.marinas?.firstOrNull { it.id == id }?.url?.let(Links::web)?.let { uriHandler.openUri(it) }
        Unit
    }

    Scaffold(topBar = { TopAppBar(title = { Text("Slipanlagen") }) }) { padding ->
        Column(Modifier.padding(padding).fillMaxSize()) {
            OutlinedTextField(
                value = query,
                onValueChange = viewModel::onQueryChange,
                modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp),
                placeholder = { Text("Nach Name, Ort oder Gewässer suchen") },
                leadingIcon = { Icon(Icons.Filled.Search, contentDescription = null) },
                singleLine = true,
            )
            OverviewMap(
                slipways = slipways.orEmpty(),
                marinas = marinas.orEmpty(),
                fitTo = fitTo,
                onAreaChange = viewModel::onAreaChange,
                onSlipwayClick = onSlipwayClick,
                onMarinaClick = openMarina,
                modifier = Modifier.fillMaxWidth().weight(1f),
            )
            Column(Modifier.fillMaxWidth().weight(1f)) {
                val shownSlipways = slipways
                val shownMarinas = marinas
                if (places is UiState.Error) {
                    LoadFailed("Slipanlagen konnten nicht geladen werden.", viewModel::load)
                } else if (places is UiState.Loading || (searching && results == null)) {
                    Centered { CircularProgressIndicator() }
                } else if (shownSlipways == null || shownMarinas == null) {
                    Hint("Zoome hinein oder suche", "Im Kartenausschnitt liegen zu viele Slipanlagen und Marinas für eine Liste.")
                } else if (shownSlipways.isEmpty() && shownMarinas.isEmpty()) {
                    Hint(
                        if (searching) "Keine Treffer" else "Hier gibt es keine Einträge",
                        if (searching) "Versuche einen anderen Suchbegriff." else "Verschiebe die Karte oder suche.",
                    )
                } else {
                    PlaceList(shownSlipways, shownMarinas, onSlipwayClick, openMarina)
                }
            }
        }
    }
}

@Composable
private fun PlaceList(
    slipways: List<SlipwaySummary>,
    marinas: List<Marina>,
    onSlipwayClick: (String) -> Unit,
    onMarinaClick: (String) -> Unit,
) {
    LazyColumn {
        if (slipways.isNotEmpty()) {
            header(Format.count(slipways.size, "Slipanlage", "Slipanlagen"))
            items(slipways, key = { "slipway-${it.id}" }) { slipway ->
                ListItem(
                    headlineContent = { Text(slipway.name) },
                    supportingContent = slipway.subtitle.takeIf { it.isNotEmpty() }?.let { text -> @Composable { Text(text) } },
                    modifier = Modifier.clickable { onSlipwayClick(slipway.id) },
                )
                HorizontalDivider()
            }
        }
        if (marinas.isNotEmpty()) {
            header(Format.count(marinas.size, "Marina", "Marinas"))
            items(marinas, key = { "marina-${it.id}" }) { marina ->
                ListItem(
                    headlineContent = { Text(marina.name) },
                    supportingContent = Format.place(marina.postalCode, marina.city)?.let { place -> @Composable { Text(place) } },
                    modifier = if (marina.url != null) Modifier.clickable { onMarinaClick(marina.id) } else Modifier,
                )
                HorizontalDivider()
            }
        }
        item { OsmAttribution(Modifier.padding(16.dp)) }
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

private fun LazyListScope.header(text: String) {
    item(key = "header-$text") {
        Text(
            text,
            modifier = Modifier.padding(start = 16.dp, end = 16.dp, top = 16.dp, bottom = 4.dp),
            style = MaterialTheme.typography.titleSmall,
            color = MaterialTheme.colorScheme.primary,
        )
    }
}
