package com.bvelop.slipways.ui.contribute

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.selection.toggleable
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Place
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.Checkbox
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableDoubleStateOf
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import com.bvelop.slipways.shared.Contributions
import com.bvelop.slipways.shared.MapArea
import com.bvelop.slipways.shared.NearbySlipway
import com.bvelop.slipways.shared.NearbySlipwaysException
import com.bvelop.slipways.shared.Position
import com.bvelop.slipways.shared.SentSuggestion
import com.bvelop.slipways.shared.SuggestionKind
import com.bvelop.slipways.shared.SuggestionRequest
import com.bvelop.slipways.ui.Note
import com.bvelop.slipways.ui.SectionTitle
import com.bvelop.slipways.ui.map.STYLE_URL
import com.bvelop.slipways.ui.map.rememberMapView
import com.bvelop.slipways.ui.map.toBounds
import com.bvelop.slipways.ui.map.webZoom
import java.util.Locale
import kotlin.math.roundToInt
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.launch
import org.maplibre.android.camera.CameraUpdateFactory
import org.maplibre.android.maps.Style

/** Close enough to place the pin on the ramp, counted like the website's zoom. */
private const val PRECISE_ZOOM = 13.0

/** Suggests a slipway we do not know yet: a pin in the middle of a map to move under the ramp, a name and a few details. */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun NewSlipwayScreen(start: MapArea, onBack: () -> Unit) {
    val container = appContainer()
    val proof = rememberProof(container.api)
    val scope = rememberCoroutineScope()
    var center by remember { mutableStateOf(Position(start.centerLatitude, start.centerLongitude)) }
    var zoom by remember { mutableDoubleStateOf(0.0) }
    var name by remember { mutableStateOf("") }
    val fields = remember { mutableStateMapOf<String, String>() }
    var text by remember { mutableStateOf("") }
    var consent by remember { mutableStateOf(false) }
    var nearby by remember { mutableStateOf<List<NearbySlipway>?>(null) }
    var sending by remember { mutableStateOf(false) }
    var error by remember { mutableStateOf<String?>(null) }
    var sent by remember { mutableStateOf(false) }

    val precise = zoom >= PRECISE_ZOOM
    val canSend = name.isNotBlank() && precise && consent && proof.solution != null && !sending

    fun send(confirmNew: Boolean = false) {
        val solution = proof.solution ?: return
        sending = true
        error = null
        scope.launch {
            try {
                val created = container.api.suggest(
                    SuggestionRequest(
                        kind = SuggestionKind.NEW_SLIPWAY,
                        name = name.trim(),
                        latitude = (center.latitude * 1e6).roundToInt() / 1e6,
                        longitude = (center.longitude * 1e6).roundToInt() / 1e6,
                        fields = fields.filterValues { it.isNotEmpty() },
                        text = text.trim().ifEmpty { null },
                        confirmNew = confirmNew,
                        proof = solution,
                    ),
                )
                container.suggestions.remember(SentSuggestion(created, SuggestionKind.NEW_SLIPWAY, name.trim()))
                sent = true
            } catch (e: CancellationException) {
                throw e
            } catch (e: NearbySlipwaysException) {
                nearby = e.slipways
                proof.renew()
            } catch (e: Exception) {
                error = sendError(e)
                proof.renew()
            } finally {
                sending = false
            }
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Slipanlage fehlt?") },
                navigationIcon = {
                    IconButton(onClick = onBack) { Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Zurück") }
                },
            )
        },
    ) { padding ->
        if (sent) {
            Box(Modifier.padding(padding).padding(16.dp)) {
                Sent("Deine Slipanlage wird geprüft und erscheint nach der Freigabe auf der Karte.", onBack)
            }
            return@Scaffold
        }
        Column(Modifier.padding(padding).fillMaxSize().verticalScroll(rememberScrollState()).imePadding()) {
            PickMap(start, onMove = { position, webZoom ->
                center = position
                zoom = webZoom
                nearby = null
            }, modifier = Modifier.fillMaxWidth().height(300.dp))
            Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
                Note(
                    if (precise) String.format(Locale.ROOT, "Position: %.5f, %.5f", center.latitude, center.longitude)
                    else "Zoome hinein und schieb die Karte, bis der Pin genau auf der Rampe liegt.",
                )
                OutlinedTextField(
                    value = name,
                    onValueChange = { name = it.take(Contributions.NAME_LENGTH) },
                    modifier = Modifier.fillMaxWidth(),
                    label = { Text("Name") },
                    placeholder = { Text("z. B. Slipanlage am Yachthafen Musterstadt") },
                    singleLine = true,
                )
                listOf("fee", "surface", "access").forEach { field ->
                    Contributions.FIELDS[field]?.let { FieldInput(it, fields[field].orEmpty()) { value -> fields[field] = value } }
                }
                SectionTitle("Was gibt es dort?")
                Column {
                    Contributions.FACTS.forEach { fact ->
                        Row(
                            Modifier.fillMaxWidth().toggleable(fields[fact] == "yes", role = Role.Checkbox) { fields[fact] = if (it) "yes" else "" },
                            verticalAlignment = Alignment.CenterVertically,
                        ) {
                            Checkbox(checked = fields[fact] == "yes", onCheckedChange = null)
                            Text(Contributions.label(fact), Modifier.padding(start = 8.dp))
                        }
                    }
                }
                OutlinedTextField(
                    value = text,
                    onValueChange = { text = it.take(Contributions.TEXT_LENGTH) },
                    modifier = Modifier.fillMaxWidth(),
                    label = { Text("Woher weißt du das? (freiwillig)") },
                    placeholder = { Text("z. B. „War im August dort.“") },
                    minLines = 2,
                )
                Consent(
                    consent,
                    { consent = it },
                    "Ich kenne die Slipanlage selbst und habe die Angaben nicht aus anderen Verzeichnissen abgeschrieben. " +
                        "Slipways darf sie veröffentlichen.",
                )
                nearby?.let { slipways ->
                    Card {
                        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
                            Text("Ist es eine von diesen?", style = MaterialTheme.typography.titleSmall)
                            Note("In der Nähe kennen wir schon diese Slipanlagen. Wenn deine dabei ist, ergänze sie lieber dort.")
                            slipways.forEach { Text("${it.name} (${it.distance.roundToInt()} m)") }
                            OutlinedButton(onClick = { send(confirmNew = true) }, enabled = canSend) {
                                Text("Nein, trotzdem neu eintragen")
                            }
                        }
                    }
                }
                SendState(proof, error, sending)
                if (nearby == null) {
                    Button(onClick = { send() }, enabled = canSend, modifier = Modifier.fillMaxWidth()) { Text("Vorschlag senden") }
                }
                Note("Alles wird vor der Veröffentlichung von Hand geprüft. Kein Konto, keine Namen.")
            }
        }
    }
}

/** A map with a pin fixed in the middle; moving the map moves the place under the pin. */
@Composable
private fun PickMap(start: MapArea, onMove: (Position, Double) -> Unit, modifier: Modifier = Modifier) {
    val mapView = rememberMapView()
    LaunchedEffect(mapView) {
        mapView.getMapAsync { map ->
            map.uiSettings.isRotateGesturesEnabled = false
            map.uiSettings.isTiltGesturesEnabled = false
            map.setStyle(Style.Builder().fromUri(STYLE_URL)) {
                map.moveCamera(CameraUpdateFactory.newLatLngBounds(start.toBounds(), 0))
                map.addOnCameraIdleListener {
                    val target = map.cameraPosition.target ?: return@addOnCameraIdleListener
                    onMove(Position(target.latitude, target.longitude), map.webZoom)
                }
            }
        }
    }
    Box(modifier) {
        AndroidView(factory = { mapView }, modifier = Modifier.fillMaxSize())
        // The tip of the pin marks the middle of the map.
        Icon(
            Icons.Filled.Place,
            contentDescription = "Position der Slipanlage, zum Verschieben die Karte bewegen",
            modifier = Modifier.align(Alignment.Center).size(40.dp).offset(y = (-20).dp),
            tint = Color(0xFFD32F2F),
        )
    }
}
