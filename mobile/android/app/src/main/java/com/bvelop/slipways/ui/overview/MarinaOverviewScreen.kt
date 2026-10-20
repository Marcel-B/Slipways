package com.bvelop.slipways.ui.overview

import android.content.ActivityNotFoundException
import android.content.Context
import android.content.Intent
import android.net.Uri
import android.widget.Toast
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.AccessTime
import androidx.compose.material.icons.filled.Business
import androidx.compose.material.icons.filled.Directions
import androidx.compose.material.icons.filled.Email
import androidx.compose.material.icons.filled.Euro
import androidx.compose.material.icons.filled.Language
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.Map
import androidx.compose.material.icons.filled.Phone
import androidx.compose.material.icons.filled.Place
import androidx.compose.material.icons.filled.Water
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
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
import com.bvelop.slipways.shared.Marina
import com.bvelop.slipways.shared.Position
import com.bvelop.slipways.shared.Source
import com.bvelop.slipways.ui.Fact
import com.bvelop.slipways.ui.OsmAttribution
import com.bvelop.slipways.ui.UiState

/** Marinas on the map; a tap opens a short overview as a bottom sheet. */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MarinaOverviewScreen(
    onWaterClick: (String) -> Unit,
    viewModel: PlaceOverviewViewModel<Marina> = viewModel(factory = PlaceOverviewViewModel.Marinas),
) {
    val items by viewModel.items.collectAsStateWithLifecycle()
    var selectedId by rememberSaveable { mutableStateOf<String?>(null) }
    val selected = (items as? UiState.Success)?.data?.firstOrNull { it.id == selectedId }

    PlaceOverviewScreen(
        title = "Marinas",
        singular = "Marina",
        plural = "Marinas",
        color = "#0D9488",
        viewModel = viewModel,
        id = { it.id },
        position = { Position(it.latitude, it.longitude) },
        onSelect = { selectedId = it.id },
    ) { marina ->
        ListItem(
            headlineContent = { Text(marina.name) },
            supportingContent = Format.place(marina.postalCode, marina.city)?.let { place -> @Composable { Text(place) } },
            modifier = Modifier.clickable { selectedId = marina.id },
        )
    }

    if (selected != null) {
        ModalBottomSheet(onDismissRequest = { selectedId = null }) {
            MarinaSheet(
                selected,
                onWaterClick = { water ->
                    selectedId = null
                    onWaterClick(water)
                },
            )
        }
    }
}

@Composable
private fun MarinaSheet(marina: Marina, onWaterClick: (String) -> Unit) {
    val context = LocalContext.current
    val uriHandler = LocalUriHandler.current

    Column(
        Modifier.verticalScroll(rememberScrollState()).padding(start = 16.dp, end = 16.dp, bottom = 32.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        Text(marina.name, style = MaterialTheme.typography.titleLarge)
        marina.water?.let { water ->
            Box(Modifier.clickable { onWaterClick(water.id) }) {
                Fact("Gewässer", Icons.Filled.Water, water.name)
            }
        }
        Fact("Adresse", Icons.Filled.Place, marina.address)
        Fact("Betreiber", Icons.Filled.Business, marina.operator)
        Fact("Gebühr", Icons.Filled.Euro, marina.fee?.let(Labels::fee))
        Fact("Zugang", Icons.Filled.Lock, marina.access?.let(Labels::access))
        Fact("Öffnungszeiten", Icons.Filled.AccessTime, marina.openingHours)
        marina.url?.let { Fact("Webseite", Icons.Filled.Language, it, Links.web(it)) }
        marina.phone?.let { Fact("Telefon", Icons.Filled.Phone, it, Links.phone(it)) }
        marina.email?.let { Fact("E-Mail", Icons.Filled.Email, it, Links.mail(it)) }

        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            Button(
                onClick = {
                    // Google Maps starts the route right away; other map apps get the place.
                    val navigation = "google.navigation:q=${marina.latitude},${marina.longitude}".toUri()
                    if (!open(context, navigation)) open(context, geo(marina), report = true)
                },
                contentPadding = ButtonDefaults.ButtonWithIconContentPadding,
            ) {
                Icon(Icons.Filled.Directions, contentDescription = null, Modifier.padding(end = 8.dp))
                Text("Route planen")
            }
            OutlinedButton(
                onClick = { open(context, geo(marina), report = true) },
                contentPadding = ButtonDefaults.ButtonWithIconContentPadding,
            ) {
                Icon(Icons.Filled.Map, contentDescription = null, Modifier.padding(end = 8.dp))
                Text("In Karten öffnen")
            }
        }
        marina.osmElement?.let { element ->
            OutlinedButton(onClick = { uriHandler.openUri(Links.osm(element)) }) { Text("In OpenStreetMap ansehen") }
        }
        if (marina.source == Source.OSM) OsmAttribution()
    }
}

private fun geo(marina: Marina): Uri =
    "geo:0,0?q=${marina.latitude},${marina.longitude}(${Uri.encode(marina.name)})".toUri()

/** False when no app handles the link. */
private fun open(context: Context, uri: Uri, report: Boolean = false): Boolean =
    try {
        context.startActivity(Intent(Intent.ACTION_VIEW, uri))
        true
    } catch (_: ActivityNotFoundException) {
        if (report) Toast.makeText(context, "Keine Karten-App installiert.", Toast.LENGTH_SHORT).show()
        false
    }
