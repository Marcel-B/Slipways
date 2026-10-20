package com.bvelop.slipways.ui.details

import android.content.ActivityNotFoundException
import android.content.Intent
import android.net.Uri
import android.widget.Toast
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.AccessTime
import androidx.compose.material.icons.filled.Anchor
import androidx.compose.material.icons.filled.Business
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
import androidx.compose.material3.Card
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
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalUriHandler
import androidx.compose.ui.unit.dp
import androidx.core.net.toUri
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.bvelop.slipways.shared.Format
import com.bvelop.slipways.shared.Labels
import com.bvelop.slipways.shared.Links
import com.bvelop.slipways.shared.NearestGauge
import com.bvelop.slipways.shared.SlipwayDetails
import com.bvelop.slipways.shared.Source
import com.bvelop.slipways.ui.Fact
import com.bvelop.slipways.ui.LoadFailed
import com.bvelop.slipways.ui.OsmAttribution
import com.bvelop.slipways.ui.UiState
import com.bvelop.slipways.ui.WaterLevelText
import java.util.Locale

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SlipwayDetailsScreen(
    onBack: () -> Unit,
    onWaterClick: (String) -> Unit,
    viewModel: SlipwayDetailsViewModel = viewModel(factory = SlipwayDetailsViewModel.Factory),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val nearestGauge by viewModel.nearestGauge.collectAsStateWithLifecycle()

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
                is UiState.Success -> Details(s.data, nearestGauge, onWaterClick)
            }
        }
    }
}

@Composable
private fun Details(slipway: SlipwayDetails, nearestGauge: NearestGauge?, onWaterClick: (String) -> Unit) {
    val context = LocalContext.current
    val uriHandler = LocalUriHandler.current

    Column(
        Modifier.verticalScroll(rememberScrollState()).padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        slipway.water?.let { water ->
            Box(Modifier.clickable { onWaterClick(water.id) }) {
                Fact("Gewässer", Icons.Filled.Water, water.name)
            }
        }
        Fact("Kosten", Icons.Filled.Euro, slipway.costsLabel)
        Fact("Adresse", Icons.Filled.Place, slipway.address)
        Fact("Betreiber", Icons.Filled.Business, slipway.operator)
        Fact("Zugang", Icons.Filled.Lock, slipway.access?.let(Labels::access))
        Fact("Öffnungszeiten", Icons.Filled.AccessTime, slipway.openingHours)
        Fact("Untergrund", Icons.Filled.Layers, slipway.surface?.let(Labels::surface))
        slipway.url?.let { Fact("Webseite", Icons.Filled.Language, it, Links.web(it)) }
        slipway.marina?.let { Fact("Marina", Icons.Filled.Anchor, it.name, it.url?.let(Links::web)) }
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

        nearestGauge?.let { gauge ->
            Card(Modifier.fillMaxWidth().clickable { slipway.water?.let { onWaterClick(it.id) } }) {
                Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                    Text("Nächster Pegel: ${gauge.station.name}", style = MaterialTheme.typography.titleSmall)
                    WaterLevelText(gauge.level)
                    Text(
                        "${Format.kilometres(gauge.km)} entfernt · gemessen ${Format.measuredAt(gauge.level)}",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }
        }

        Button(
            onClick = {
                val label = Uri.encode(slipway.name)
                val uri = "geo:0,0?q=${slipway.latitude},${slipway.longitude}($label)".toUri()
                try {
                    context.startActivity(Intent(Intent.ACTION_VIEW, uri))
                } catch (_: ActivityNotFoundException) {
                    Toast.makeText(context, "Keine Karten-App installiert.", Toast.LENGTH_SHORT).show()
                }
            },
            contentPadding = ButtonDefaults.ButtonWithIconContentPadding,
        ) {
            Icon(Icons.Filled.Map, contentDescription = null, Modifier.padding(end = 8.dp))
            Text("In Karten öffnen")
        }
        slipway.osmElement?.let { element ->
            OutlinedButton(onClick = { uriHandler.openUri(Links.osm(element)) }) { Text("In OpenStreetMap ansehen") }
        }
        if (slipway.source == Source.OSM) OsmAttribution()
    }
}
