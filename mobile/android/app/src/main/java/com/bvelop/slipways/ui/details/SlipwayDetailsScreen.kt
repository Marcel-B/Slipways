package com.bvelop.slipways.ui.details

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.AccessTime
import androidx.compose.material.icons.filled.Anchor
import androidx.compose.material.icons.filled.Business
import androidx.compose.material.icons.filled.Directions
import androidx.compose.material.icons.filled.Euro
import androidx.compose.material.icons.filled.Layers
import androidx.compose.material.icons.filled.Language
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.Map
import androidx.compose.material.icons.filled.MyLocation
import androidx.compose.material.icons.filled.Place
import androidx.compose.material.icons.filled.Star
import androidx.compose.material.icons.filled.ThumbDown
import androidx.compose.material.icons.filled.ThumbUp
import androidx.compose.material.icons.filled.Verified
import androidx.compose.material.icons.filled.Water
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalUriHandler
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.bvelop.slipways.shared.Labels
import com.bvelop.slipways.shared.Links
import com.bvelop.slipways.shared.MapArea
import com.bvelop.slipways.shared.NearestGauge
import com.bvelop.slipways.shared.Position
import com.bvelop.slipways.shared.SlipwayDetails
import com.bvelop.slipways.shared.Source
import com.bvelop.slipways.shared.SourcedValue
import com.bvelop.slipways.ui.Fact
import com.bvelop.slipways.ui.LoadFailed
import com.bvelop.slipways.ui.MapsApp
import com.bvelop.slipways.ui.NearestGaugeCard
import com.bvelop.slipways.ui.OsmAttribution
import com.bvelop.slipways.ui.SourcedFact
import com.bvelop.slipways.ui.TickedFacts
import com.bvelop.slipways.ui.Tips
import com.bvelop.slipways.ui.UiState
import com.bvelop.slipways.ui.map.DetailMap
import com.bvelop.slipways.ui.map.DetailPin
import com.bvelop.slipways.ui.map.PinColors
import com.bvelop.slipways.ui.marina.MarinaSheet
import java.util.Locale

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SlipwayDetailsScreen(
    onBack: () -> Unit,
    onWaterClick: (String) -> Unit,
    onSlipwayClick: (String) -> Unit,
    viewModel: SlipwayDetailsViewModel = viewModel(factory = SlipwayDetailsViewModel.Factory),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val nearestGauge by viewModel.nearestGauge.collectAsStateWithLifecycle()
    var marinaId by rememberSaveable { mutableStateOf<String?>(null) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text((state as? UiState.Success)?.data?.name ?: "Slipanlage", maxLines = 1) },
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
                is UiState.Success -> Details(s.data, nearestGauge, onWaterClick, onMarinaClick = { marinaId = it })
            }
        }
    }

    marinaId?.let { id ->
        MarinaSheet(id, onDismiss = { marinaId = null }, onWaterClick = onWaterClick, onSlipwayClick = onSlipwayClick)
    }
}

@Composable
private fun Details(
    slipway: SlipwayDetails,
    nearestGauge: NearestGauge?,
    onWaterClick: (String) -> Unit,
    onMarinaClick: (String) -> Unit,
) {
    val context = LocalContext.current
    val uriHandler = LocalUriHandler.current
    val position = Position(slipway.latitude, slipway.longitude)

    Column(Modifier.verticalScroll(rememberScrollState())) {
        DetailMap(
            area = MapArea.fitting(listOf(position))!!,
            pins = listOf(DetailPin(position, PinColors.SLIPWAY)),
            modifier = Modifier.fillMaxWidth().height(240.dp),
        )
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
            slipway.water?.let { water ->
                Box(Modifier.clickable { onWaterClick(water.id) }) {
                    Fact("Gewässer", Icons.Filled.Water, water.name)
                }
            }
            SourcedFact("Kosten", Icons.Filled.Euro, (slipway.value("costs") ?: slipway.value("fee"))?.copy(value = slipway.costsLabel)
                ?: SourcedValue(slipway.costsLabel, null))
            Fact("Adresse", Icons.Filled.Place, slipway.address)
            SourcedFact("Betreiber", Icons.Filled.Business, slipway.value("operator"))
            SourcedFact("Zugang", Icons.Filled.Lock, slipway.value("access"), Labels::access)
            SourcedFact("Öffnungszeiten", Icons.Filled.AccessTime, slipway.value("opening_hours"))
            SourcedFact("Untergrund", Icons.Filled.Layers, slipway.value("surface"), Labels::surface)
            slipway.url?.let { Fact("Webseite", Icons.Filled.Language, it, Links.web(it)) }
            slipway.marina?.let { marina ->
                Box(Modifier.clickable { onMarinaClick(marina.id) }) {
                    Fact("Marina", Icons.Filled.Anchor, marina.name)
                }
            }
            if (slipway.extras.isNotEmpty()) Fact("Extras", Icons.Filled.Star, slipway.extras.joinToString(", "))
            Fact("Koordinaten", Icons.Filled.MyLocation, String.format(Locale.ROOT, "%.5f, %.5f", slipway.latitude, slipway.longitude))
            Fact(
                "Quelle",
                Icons.Filled.Verified,
                if (slipway.source == Source.OSM) "OpenStreetMap" else "eigene Erfassung",
            )

            slipway.comment?.let { Text(it, style = MaterialTheme.typography.bodyLarge) }
            slipway.pro?.let { Fact("Pro", Icons.Filled.ThumbUp, it) }
            slipway.contra?.let { Fact("Contra", Icons.Filled.ThumbDown, it) }

            TickedFacts(slipway.ticked)
            Tips(slipway.tips)

            nearestGauge?.let { gauge -> NearestGaugeCard(gauge) { slipway.water?.let { onWaterClick(it.id) } } }

            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                Button(
                    onClick = { MapsApp.route(context, slipway.latitude, slipway.longitude, slipway.name) },
                    contentPadding = ButtonDefaults.ButtonWithIconContentPadding,
                ) {
                    Icon(Icons.Filled.Directions, contentDescription = null, Modifier.padding(end = 8.dp))
                    Text("Route planen")
                }
                OutlinedButton(
                    onClick = { MapsApp.show(context, slipway.latitude, slipway.longitude, slipway.name) },
                    contentPadding = ButtonDefaults.ButtonWithIconContentPadding,
                ) {
                    Icon(Icons.Filled.Map, contentDescription = null, Modifier.padding(end = 8.dp))
                    Text("In Karten öffnen")
                }
            }
            slipway.osmElement?.let { element ->
                OutlinedButton(onClick = { uriHandler.openUri(Links.osm(element)) }) { Text("In OpenStreetMap ansehen") }
            }
            if (slipway.source == Source.OSM) OsmAttribution()
        }
    }
}
