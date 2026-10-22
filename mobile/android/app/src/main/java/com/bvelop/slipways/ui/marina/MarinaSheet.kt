package com.bvelop.slipways.ui.marina

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
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
import androidx.compose.material.icons.filled.MyLocation
import androidx.compose.material.icons.filled.Phone
import androidx.compose.material.icons.filled.Place
import androidx.compose.material.icons.filled.Water
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.produceState
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalUriHandler
import androidx.compose.ui.unit.dp
import com.bvelop.slipways.SlipwaysApplication
import com.bvelop.slipways.shared.ApiException
import com.bvelop.slipways.shared.Format
import com.bvelop.slipways.shared.Labels
import com.bvelop.slipways.shared.Links
import com.bvelop.slipways.shared.MapArea
import com.bvelop.slipways.shared.MarinaDetails
import com.bvelop.slipways.shared.NearestGauge
import com.bvelop.slipways.shared.PlaceType
import com.bvelop.slipways.shared.Position
import com.bvelop.slipways.ui.Centered
import com.bvelop.slipways.ui.Fact
import com.bvelop.slipways.ui.LoadFailed
import com.bvelop.slipways.ui.MapsApp
import com.bvelop.slipways.ui.NearestGaugeCard
import com.bvelop.slipways.ui.OsmAttribution
import com.bvelop.slipways.ui.SectionTitle
import com.bvelop.slipways.ui.SourcedFact
import com.bvelop.slipways.ui.Tips
import com.bvelop.slipways.ui.UiState
import com.bvelop.slipways.ui.contribute.ContributeSection
import com.bvelop.slipways.ui.contribute.SuggestMode
import com.bvelop.slipways.ui.contribute.SuggestPlace
import com.bvelop.slipways.ui.contribute.SuggestSheet
import com.bvelop.slipways.ui.map.DetailMap
import com.bvelop.slipways.ui.map.DetailPin
import com.bvelop.slipways.ui.map.PinColors
import java.util.Locale
import kotlinx.coroutines.CancellationException

/**
 * A marina with its slipways, details from its website and users, and the nearest gauge, as a bottom sheet over the
 * screen that opened it.
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MarinaSheet(
    marinaId: String,
    onDismiss: () -> Unit,
    onWaterClick: (String) -> Unit,
    onSlipwayClick: (String) -> Unit,
) {
    val container = (LocalContext.current.applicationContext as SlipwaysApplication).container
    val api = container.api
    var suggest by remember { mutableStateOf<SuggestMode?>(null) }
    // Read again after each form, so a value just sent shows as waiting.
    var sentRound by remember { mutableIntStateOf(0) }
    var attempt by remember { mutableIntStateOf(0) }
    val state by produceState<UiState<MarinaDetails>>(UiState.Loading, marinaId, attempt) {
        value = UiState.Loading
        value = try {
            UiState.Success(api.marina(marinaId))
        } catch (e: CancellationException) {
            throw e
        } catch (e: ApiException) {
            UiState.Error(if (e.status == 404) "Diese Marina gibt es nicht." else "Fehler ${e.status}")
        } catch (e: Exception) {
            UiState.Error(e.message ?: "Unbekannter Fehler")
        }
    }
    val marina = (state as? UiState.Success)?.data
    val gauge by produceState<NearestGauge?>(null, marina) {
        val waterId = marina?.water?.id ?: return@produceState
        value = try {
            api.water(waterId).nearestGauge(marina.latitude, marina.longitude)
        } catch (e: CancellationException) {
            throw e
        } catch (_: Exception) {
            null
        }
    }

    ModalBottomSheet(onDismissRequest = onDismiss) {
        when (val s = state) {
            UiState.Loading -> Box(Modifier.fillMaxWidth().height(200.dp)) { Centered { CircularProgressIndicator() } }
            is UiState.Error -> Box(Modifier.fillMaxWidth().height(200.dp)) { LoadFailed(s.message) { attempt++ } }
            is UiState.Success -> Details(
                s.data,
                gauge,
                pending = remember(s.data, sentRound) {
                    container.suggestions.pendingFields(PlaceType.MARINA, s.data.id, s.data.lastChanged)
                },
                onSuggest = { suggest = it },
                onWaterClick = { water ->
                    onDismiss()
                    onWaterClick(water)
                },
                onSlipwayClick = { slipway ->
                    onDismiss()
                    onSlipwayClick(slipway)
                },
            )
        }
    }

    val mode = suggest
    if (marina != null && mode != null) {
        SuggestSheet(marina.place, mode, marina.values, onDismiss = {
            suggest = null
            sentRound++
        })
    }
}

@Composable
private fun Details(
    marina: MarinaDetails,
    gauge: NearestGauge?,
    pending: Set<String>,
    onSuggest: (SuggestMode) -> Unit,
    onWaterClick: (String) -> Unit,
    onSlipwayClick: (String) -> Unit,
) {
    val context = LocalContext.current
    val uriHandler = LocalUriHandler.current
    val pins = listOf(DetailPin(Position(marina.latitude, marina.longitude), PinColors.MARINA)) +
        marina.slipways.map { DetailPin(Position(it.latitude, it.longitude), PinColors.SLIPWAY) }

    Column(
        Modifier.verticalScroll(rememberScrollState()).padding(start = 16.dp, end = 16.dp, bottom = 32.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        Text(marina.name, style = MaterialTheme.typography.titleLarge)
        DetailMap(
            area = MapArea.fitting(pins.map { it.position })!!,
            pins = pins,
            modifier = Modifier.fillMaxWidth().height(200.dp),
        )
        marina.water?.let { water ->
            Box(Modifier.clickable { onWaterClick(water.id) }) {
                Fact("Gewässer", Icons.Filled.Water, water.name)
            }
        }
        Fact("Adresse", Icons.Filled.Place, marina.address)
        SourcedFact("Gebühr", Icons.Filled.Euro, marina.value("fee"), Labels::fee, pending = "fee" in pending, onEdit = { onSuggest(SuggestMode.field("fee")) })
        SourcedFact("Betreiber", Icons.Filled.Business, marina.value("operator"), pending = "operator" in pending, onEdit = { onSuggest(SuggestMode.field("operator")) })
        SourcedFact("Zugang", Icons.Filled.Lock, marina.value("access"), Labels::access, pending = "access" in pending, onEdit = { onSuggest(SuggestMode.field("access")) })
        SourcedFact("Öffnungszeiten", Icons.Filled.AccessTime, marina.value("opening_hours"), pending = "opening_hours" in pending, onEdit = { onSuggest(SuggestMode.field("opening_hours")) })
        SourcedFact("Telefon", Icons.Filled.Phone, marina.value("phone"), link = Links::phone, pending = "phone" in pending, onEdit = { onSuggest(SuggestMode.field("phone")) })
        SourcedFact("E-Mail", Icons.Filled.Email, marina.value("email"), link = Links::mail, pending = "email" in pending, onEdit = { onSuggest(SuggestMode.field("email")) })
        marina.url?.let { Fact("Webseite", Icons.Filled.Language, it, Links.web(it)) }
        Fact("Koordinaten", Icons.Filled.MyLocation, String.format(Locale.ROOT, "%.5f, %.5f", marina.latitude, marina.longitude))

        if (marina.slipways.isNotEmpty()) {
            SectionTitle(Format.count(marina.slipways.size, "Slipanlage", "Slipanlagen"))
            Column {
                marina.slipways.forEach { slipway ->
                    ListItem(
                        headlineContent = { Text(slipway.name) },
                        supportingContent = Format.place(slipway.postalCode, slipway.city)?.let { place -> @Composable { Text(place) } },
                        modifier = Modifier.clickable { onSlipwayClick(slipway.id) },
                    )
                }
            }
        }
        Tips(marina.tips)
        ContributeSection(marina.place, marina.values, emptyList(), marina.osmElement, pending, onSuggest)
        gauge?.let { NearestGaugeCard(it) { marina.water?.let { water -> onWaterClick(water.id) } } }

        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            Button(
                onClick = { MapsApp.route(context, marina.latitude, marina.longitude, marina.name) },
                contentPadding = ButtonDefaults.ButtonWithIconContentPadding,
            ) {
                Icon(Icons.Filled.Directions, contentDescription = null, Modifier.padding(end = 8.dp))
                Text("Route planen")
            }
            OutlinedButton(
                onClick = { MapsApp.show(context, marina.latitude, marina.longitude, marina.name) },
                contentPadding = ButtonDefaults.ButtonWithIconContentPadding,
            ) {
                Icon(Icons.Filled.Map, contentDescription = null, Modifier.padding(end = 8.dp))
                Text("In Karten öffnen")
            }
        }
        marina.osmElement?.let { element ->
            OutlinedButton(onClick = { uriHandler.openUri(Links.osm(element)) }) { Text("In OpenStreetMap ansehen") }
        }
        if (marina.usesOpenStreetMap) OsmAttribution() else Text("Quelle: eigene Erfassung", style = MaterialTheme.typography.labelSmall)
    }
}

private val MarinaDetails.place: SuggestPlace
    get() = SuggestPlace(PlaceType.MARINA, id, name, source)

private val MarinaDetails.values: Map<String, String>
    get() = listOf("fee", "operator", "access", "opening_hours", "phone", "email")
        .mapNotNull { field -> value(field)?.let { field to it.value } }
        .toMap()
