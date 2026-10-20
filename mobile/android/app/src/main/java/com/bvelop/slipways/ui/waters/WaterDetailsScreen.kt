package com.bvelop.slipways.ui.waters

import androidx.compose.foundation.clickable
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
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalUriHandler
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.bvelop.slipways.shared.Format
import com.bvelop.slipways.shared.Links
import com.bvelop.slipways.shared.WaterDetails
import com.bvelop.slipways.ui.LoadFailed
import com.bvelop.slipways.ui.OsmAttribution
import com.bvelop.slipways.ui.UiState
import com.bvelop.slipways.ui.WaterLevelText

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun WaterDetailsScreen(
    onBack: () -> Unit,
    onSlipwayClick: (String) -> Unit,
    viewModel: WaterDetailsViewModel = viewModel(factory = WaterDetailsViewModel.Factory),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()

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
                is UiState.Success -> Details(s.data, onSlipwayClick)
            }
        }
    }
}

@Composable
private fun Details(water: WaterDetails, onSlipwayClick: (String) -> Unit) {
    val uriHandler = LocalUriHandler.current

    LazyColumn {
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
                val url = marina.url?.let(Links::web)
                ListItem(
                    headlineContent = { Text(marina.name) },
                    supportingContent = Format.place(marina.postalCode, marina.city)?.let { place -> @Composable { Text(place) } },
                    modifier = if (url != null) Modifier.clickable { uriHandler.openUri(url) } else Modifier,
                )
                HorizontalDivider()
            }
        }
        if (water.usesOpenStreetMap) {
            item { OsmAttribution(Modifier.padding(16.dp)) }
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
