package com.bvelop.slipways.ui.waters

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyListScope
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
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
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalUriHandler
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.bvelop.slipways.shared.Format
import com.bvelop.slipways.shared.Links
import com.bvelop.slipways.shared.MapArea
import com.bvelop.slipways.shared.Position
import com.bvelop.slipways.shared.WaterDetails
import com.bvelop.slipways.ui.LoadFailed
import com.bvelop.slipways.ui.OsmAttribution
import com.bvelop.slipways.ui.UiState
import com.bvelop.slipways.ui.WaterLevelText
import com.bvelop.slipways.ui.map.DetailMap
import com.bvelop.slipways.ui.map.DetailPin
import com.bvelop.slipways.ui.map.PinColors
import com.bvelop.slipways.ui.marina.MarinaSheet

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun WaterDetailsScreen(
    onBack: () -> Unit,
    onSlipwayClick: (String) -> Unit,
    onWaterClick: (String) -> Unit,
    viewModel: WaterDetailsViewModel = viewModel(factory = WaterDetailsViewModel.Factory),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    var marinaId by rememberSaveable { mutableStateOf<String?>(null) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text((state as? UiState.Success)?.data?.name ?: "Gewässer", maxLines = 1) },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Zurück")
                    }
                },
            )
        },
    ) { padding ->
        Box(Modifier.padding(padding).fillMaxSize()) {
            when (val s = state) {
                UiState.Loading -> CircularProgressIndicator(Modifier.align(Alignment.Center))
                is UiState.Error -> LoadFailed(s.message, viewModel::load)
                is UiState.Success -> Details(s.data, onSlipwayClick, onMarinaClick = { marinaId = it })
            }
        }
    }

    marinaId?.let { id ->
        MarinaSheet(id, onDismiss = { marinaId = null }, onWaterClick = onWaterClick, onSlipwayClick = onSlipwayClick)
    }
}

@Composable
private fun Details(water: WaterDetails, onSlipwayClick: (String) -> Unit, onMarinaClick: (String) -> Unit) {
    val pins = remember(water) {
        water.slipways.map { DetailPin(Position(it.latitude, it.longitude), PinColors.SLIPWAY) } +
            water.stations.map { DetailPin(Position(it.latitude, it.longitude), PinColors.STATION) } +
            water.marinas.map { DetailPin(Position(it.latitude, it.longitude), PinColors.MARINA) }
    }

    LazyColumn {
        water.bounds?.let { bounds ->
            item(key = "map") {
                DetailMap(
                    area = MapArea.fitting(listOf(Position(bounds.south, bounds.west), Position(bounds.north, bounds.east)))!!,
                    pins = pins,
                    shape = water.shape?.toString(),
                    modifier = Modifier.fillMaxWidth().height(280.dp),
                )
                Legend(water)
            }
        }
        if (water.bounds == null) {
            item(key = "empty") {
                Text(
                    "Zu diesem Gewässer sind noch keine Orte bekannt.",
                    modifier = Modifier.padding(16.dp),
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        }
        if (water.stations.isNotEmpty()) {
            header(Format.count(water.stations.size, "Pegel", "Pegel"))
            items(water.stations, key = { "station-${it.id}" }) { station ->
                val level = station.waterLevel
                ListItem(
                    headlineContent = { Text(station.name) },
                    supportingContent = { Text(level?.let { "gemessen ${Format.measuredAt(it)}" } ?: "kein Messwert") },
                    trailingContent = level?.let { @Composable { WaterLevelText(it) } },
                )
                HorizontalDivider()
            }
        }
        if (water.slipways.isNotEmpty()) {
            header(Format.count(water.slipways.size, "Slipanlage", "Slipanlagen"))
            items(water.slipways, key = { "slipway-${it.id}" }) { slipway ->
                ListItem(
                    headlineContent = { Text(slipway.name) },
                    supportingContent = Format.place(slipway.postalCode, slipway.city)?.let { place -> @Composable { Text(place) } },
                    modifier = Modifier.clickable { onSlipwayClick(slipway.id) },
                )
                HorizontalDivider()
            }
        }
        if (water.marinas.isNotEmpty()) {
            header(Format.count(water.marinas.size, "Marina", "Marinas"))
            items(water.marinas, key = { "marina-${it.id}" }) { marina ->
                ListItem(
                    headlineContent = { Text(marina.name) },
                    supportingContent = Format.place(marina.postalCode, marina.city)?.let { place -> @Composable { Text(place) } },
                    modifier = Modifier.clickable { onMarinaClick(marina.id) },
                )
                HorizontalDivider()
            }
        }
        if (water.usesOpenStreetMap) {
            item { OsmAttribution(Modifier.padding(16.dp)) }
        }
    }
}

/** What the colours on the map mean, as on the website. */
@OptIn(ExperimentalLayoutApi::class)
@Composable
private fun Legend(water: WaterDetails) {
    val uriHandler = LocalUriHandler.current
    val entries = listOfNotNull(
        water.shape?.let { "Verlauf aus OpenStreetMap" to PinColors.WATER },
        ("Slipanlage" to PinColors.SLIPWAY).takeIf { water.slipways.isNotEmpty() },
        ("Pegel" to PinColors.STATION).takeIf { water.stations.isNotEmpty() },
        ("Marina" to PinColors.MARINA).takeIf { water.marinas.isNotEmpty() },
    )
    FlowRow(
        Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
        horizontalArrangement = Arrangement.spacedBy(12.dp),
        verticalArrangement = Arrangement.spacedBy(4.dp),
    ) {
        entries.forEach { (label, color) ->
            Row(
                verticalAlignment = Alignment.CenterVertically,
                modifier = if (color == PinColors.WATER && water.osmElements.isNotEmpty()) {
                    Modifier.clickable { uriHandler.openUri(Links.osm(water.osmElements.first())) }
                } else {
                    Modifier
                },
            ) {
                Box(Modifier.size(10.dp).background(Color(android.graphics.Color.parseColor(color)), CircleShape))
                Text(label, Modifier.padding(start = 6.dp), style = MaterialTheme.typography.bodySmall)
            }
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
