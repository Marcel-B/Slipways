package com.bvelop.slipways.ui.waters

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowRight
import androidx.compose.material.icons.filled.Clear
import androidx.compose.material.icons.filled.RestartAlt
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ElevatedCard
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ListItem
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
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.bvelop.slipways.shared.Countries
import com.bvelop.slipways.shared.Format
import com.bvelop.slipways.shared.MapArea
import com.bvelop.slipways.shared.Overview
import com.bvelop.slipways.shared.WaterSummary
import com.bvelop.slipways.ui.Centered
import com.bvelop.slipways.ui.CountryMenu
import com.bvelop.slipways.ui.LoadFailed
import com.bvelop.slipways.ui.OsmAttribution
import com.bvelop.slipways.ui.map.MapTarget
import com.bvelop.slipways.ui.UiState
import com.bvelop.slipways.ui.WaterLevelText
import com.bvelop.slipways.ui.overview.Hint
import com.bvelop.slipways.ui.overview.ListHeader

/**
 * Waters on a map as their courses and areas, with search and choice of country; the list below shows the waters in
 * the visible area or the search hits.
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun WaterOverviewScreen(
    onWaterClick: (String) -> Unit,
    viewModel: WaterOverviewViewModel = viewModel(factory = WaterOverviewViewModel.Factory),
) {
    val items by viewModel.items.collectAsStateWithLifecycle()
    val query by viewModel.query.collectAsStateWithLifecycle()
    val results by viewModel.results.collectAsStateWithLifecycle()
    val area by viewModel.area.collectAsStateWithLifecycle()
    val shapes by viewModel.shapes.collectAsStateWithLifecycle()
    val chosen by viewModel.country.collectAsStateWithLifecycle()
    var text by rememberSaveable { mutableStateOf("") }
    var selectedId by rememberSaveable { mutableStateOf<String?>(null) }

    val all = (items as? UiState.Success)?.data
    val searching = query.isNotEmpty()
    val countries = remember(all) { Countries.counts(all.orEmpty().map { it.countries }) }
    val candidates = remember(all, searching, results, chosen) {
        val shown = if (searching) results.orEmpty() else all.orEmpty()
        if (chosen == null) shown else shown.filter { chosen in it.countries }
    }
    // Shapes and list are fetched separately; draw only what the list shows, too.
    val geoJson = remember(shapes, candidates) { shapes?.toGeoJson(candidates.mapTo(HashSet()) { it.id }) ?: EMPTY }
    val listed: List<WaterSummary>? = remember(all, searching, candidates, area) {
        when {
            all == null -> emptyList()
            searching -> candidates
            else -> Overview.visible(candidates.filter { it.position != null }, area) { it.position!! }
        }
    }
    val selected = candidates.firstOrNull { it.id == selectedId }

    // A search moves the map to its hits, a chosen country to that country.
    var fitTo by remember { mutableStateOf<MapTarget?>(null) }
    LaunchedEffect(results) {
        if (results != null) MapArea.fitting(candidates.mapNotNull { it.position })?.let { fitTo = MapTarget(it) }
    }
    val chooseCountry = { code: String? ->
        viewModel.onCountryChange(code)
        val base = if (searching) results.orEmpty() else all.orEmpty()
        val points = base.filter { code == null || code in it.countries }.mapNotNull { it.position }
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
        selectedId = null
        fitTo = MapTarget(MapArea.START, padding = 0)
    }

    Scaffold(topBar = { TopAppBar(title = { Text("Gewässer") }) }) { padding ->
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
                    placeholder = { Text("Nach Gewässer suchen") },
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
            Box(Modifier.fillMaxWidth().weight(1f)) {
                WaterShapesMap(
                    geoJson = geoJson,
                    selectedId = selectedId,
                    fitTo = fitTo,
                    onViewChange = { visible, zoom ->
                        viewModel.onViewChange(visible, zoom)
                        onView(visible, zoom)
                    },
                    onSelect = { selectedId = it },
                    modifier = Modifier.fillMaxSize(),
                )
                selected?.let { water ->
                    // The tapped water, as the website's popup.
                    ElevatedCard(
                        Modifier.align(Alignment.BottomCenter).padding(start = 12.dp, end = 12.dp, bottom = 32.dp).fillMaxWidth()
                            .clickable { onWaterClick(water.id) },
                    ) {
                        ListItem(
                            headlineContent = { Text(water.name) },
                            supportingContent = { Text(contents(water)) },
                            trailingContent = { Icon(Icons.AutoMirrored.Filled.KeyboardArrowRight, contentDescription = "Öffnen") },
                        )
                    }
                }
            }
            Column(Modifier.fillMaxWidth().weight(1f)) {
                val shown = listed
                if (items is UiState.Error) {
                    LoadFailed("Gewässer konnten nicht geladen werden.", viewModel::load)
                } else if (items is UiState.Loading || (searching && results == null)) {
                    Centered { CircularProgressIndicator() }
                } else if (shown == null) {
                    Hint("Zoome hinein oder suche", "${Format.count(candidates.size, "Gewässer", "Gewässer")}, zu viele für eine Liste.")
                } else if (shown.isEmpty()) {
                    Hint(
                        if (searching) "Keine Treffer" else "Hier liegt kein Gewässer",
                        if (searching) "Versuche einen anderen Suchbegriff." else "Verschiebe die Karte oder suche.",
                    )
                } else {
                    LazyColumn {
                        item(key = "header") {
                            ListHeader(
                                if (searching) Format.count(shown.size, "Treffer", "Treffer")
                                else "${Format.count(shown.size, "Gewässer", "Gewässer")} im Kartenausschnitt",
                            )
                        }
                        items(shown, key = { it.id }) { water ->
                            ListItem(
                                headlineContent = { Text(water.name) },
                                supportingContent = {
                                    Column {
                                        Text(contents(water))
                                        water.gauge?.let { gauge ->
                                            Row {
                                                Text("Pegel ${gauge.stationName}: ")
                                                WaterLevelText(gauge.waterLevel)
                                            }
                                        }
                                    }
                                },
                                modifier = Modifier.clickable { onWaterClick(water.id) },
                            )
                            HorizontalDivider()
                        }
                        item(key = "attribution") { OsmAttribution(Modifier.padding(16.dp)) }
                    }
                }
            }
        }
    }
}

private const val EMPTY = """{"type":"FeatureCollection","features":[]}"""

private fun contents(water: WaterSummary): String {
    val parts = listOfNotNull(
        Format.count(water.slipwayCount, "Slipanlage", "Slipanlagen").takeIf { water.slipwayCount > 0 },
        Format.count(water.stationCount, "Pegel", "Pegel").takeIf { water.stationCount > 0 },
    )
    return if (parts.isEmpty()) "Keine Slipanlagen oder Pegel" else parts.joinToString(" · ")
}
