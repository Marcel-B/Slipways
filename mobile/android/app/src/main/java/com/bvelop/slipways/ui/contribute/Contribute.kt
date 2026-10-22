package com.bvelop.slipways.ui.contribute

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.selection.toggleable
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.OpenInNew
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.ArrowDropDown
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Checklist
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material.icons.filled.Flag
import androidx.compose.material.icons.filled.Lightbulb
import androidx.compose.material.icons.filled.VerifiedUser
import androidx.compose.material3.Button
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.rememberModalBottomSheetState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.produceState
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalUriHandler
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import com.bvelop.slipways.SlipwaysApplication
import com.bvelop.slipways.data.AppContainer
import com.bvelop.slipways.shared.ApiException
import com.bvelop.slipways.shared.Contributions
import com.bvelop.slipways.shared.Format
import com.bvelop.slipways.shared.Links
import com.bvelop.slipways.shared.PlaceType
import com.bvelop.slipways.shared.Proof
import com.bvelop.slipways.shared.ProofSolution
import com.bvelop.slipways.shared.SentSuggestion
import com.bvelop.slipways.shared.SlipwaysApi
import com.bvelop.slipways.shared.Source
import com.bvelop.slipways.shared.SuggestionField
import com.bvelop.slipways.shared.SuggestionKind
import com.bvelop.slipways.shared.SuggestionRequest
import com.bvelop.slipways.shared.TickedFact
import com.bvelop.slipways.ui.Note
import com.bvelop.slipways.ui.SectionTitle
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.launch

/** The place a suggestion is about. */
data class SuggestPlace(val type: PlaceType, val id: String, val name: String, val source: Source)

/** What the suggestion form asks for: values of some fields, a tip or a report of something wrong. */
sealed interface SuggestMode {
    val title: String
    val kind: SuggestionKind

    data class Edit(val fields: List<String>, override val title: String) : SuggestMode {
        override val kind get() = SuggestionKind.EDIT
    }

    data object Tip : SuggestMode {
        override val title get() = "Tipp geben"
        override val kind get() = SuggestionKind.TIP
    }

    data object Report : SuggestMode {
        override val title get() = "Etwas stimmt nicht"
        override val kind get() = SuggestionKind.REPORT
    }

    companion object {
        /** The form for one field, from the pencil next to its value or from „Fehlt noch“. */
        fun field(field: String, adding: Boolean = false) =
            Edit(listOf(field), "${Contributions.label(field)} ${if (adding) "ergänzen" else "korrigieren"}")
    }
}

@Composable
fun appContainer(): AppContainer = (LocalContext.current.applicationContext as SlipwaysApplication).container

/** A solved proof of work, or why there is none yet; [renew] starts over after a solution was used or turned down. */
class ProofState(val solution: ProofSolution?, val error: String?, val renew: () -> Unit)

/**
 * Fetches and solves a proof-of-work challenge as soon as a form opens, so it is usually done by the time the sender
 * presses send.
 */
@Composable
fun rememberProof(api: SlipwaysApi): ProofState {
    var round by remember { mutableIntStateOf(0) }
    val result by produceState<Result<ProofSolution>?>(null, round) {
        value = null
        value = try {
            Result.success(Proof.solve(api.challenge()))
        } catch (e: CancellationException) {
            throw e
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    return ProofState(
        solution = result?.getOrNull(),
        error = result?.exceptionOrNull()?.let { "Der Spam-Schutz ließ sich nicht berechnen. Bitte versuch es später noch einmal." },
        renew = { round++ },
    )
}

/** A German text for people for a failed send. */
fun sendError(e: Exception): String =
    (e as? ApiException)?.userMessage ?: "Der Vorschlag konnte nicht gesendet werden. Bitte prüf die Verbindung."

/** The form for a suggestion about a slipway or marina, as a bottom sheet. */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SuggestSheet(
    place: SuggestPlace,
    mode: SuggestMode,
    /** The values the place shows now, to fill in the form. */
    current: Map<String, String>,
    onDismiss: () -> Unit,
) {
    val container = appContainer()
    val proof = rememberProof(container.api)
    val scope = rememberCoroutineScope()
    val fields = (mode as? SuggestMode.Edit)?.fields.orEmpty()
    val values = remember { mutableStateMapOf<String, String>().apply { fields.forEach { put(it, current[it] ?: "") } } }
    var text by remember { mutableStateOf("") }
    var consent by remember { mutableStateOf(false) }
    var sending by remember { mutableStateOf(false) }
    var error by remember { mutableStateOf<String?>(null) }
    var sent by remember { mutableStateOf(false) }

    val changed = Contributions.changed(values.toMap(), current)
    val hasContent = if (mode is SuggestMode.Edit) changed.isNotEmpty() else text.isNotBlank()
    val canSend = hasContent && consent && proof.solution != null && !sending

    fun send() {
        val solution = proof.solution ?: return
        sending = true
        error = null
        scope.launch {
            try {
                val created = container.api.suggest(
                    SuggestionRequest(
                        kind = mode.kind,
                        placeType = place.type,
                        placeId = place.id,
                        fields = changed.takeIf { mode is SuggestMode.Edit },
                        text = text.trim().ifEmpty { null },
                        proof = solution,
                    ),
                )
                container.suggestions.remember(SentSuggestion(created, mode.kind, place.name, place.type, place.id, changed.keys))
                sent = true
            } catch (e: CancellationException) {
                throw e
            } catch (e: Exception) {
                error = sendError(e)
                proof.renew()
            } finally {
                sending = false
            }
        }
    }

    ModalBottomSheet(onDismissRequest = onDismiss, sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true)) {
        Column(
            Modifier.verticalScroll(rememberScrollState()).imePadding().padding(start = 16.dp, end = 16.dp, bottom = 32.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            Text(mode.title, style = MaterialTheme.typography.titleLarge)
            Note(place.name)
            if (sent) {
                Sent("Dein Vorschlag wird geprüft und erscheint nach der Freigabe.", onDismiss)
                return@Column
            }
            fields.forEach { name ->
                Contributions.FIELDS[name]?.let { field -> FieldInput(field, values[name].orEmpty()) { values[name] = it } }
            }
            val limit = if (mode == SuggestMode.Tip) Contributions.TIP_LENGTH else Contributions.TEXT_LENGTH
            OutlinedTextField(
                value = text,
                onValueChange = { text = it.take(limit) },
                modifier = Modifier.fillMaxWidth(),
                label = {
                    Text(
                        when (mode) {
                            SuggestMode.Tip -> "Dein Tipp"
                            SuggestMode.Report -> "Was stimmt nicht?"
                            is SuggestMode.Edit -> "Woher weißt du das? (freiwillig)"
                        },
                    )
                },
                placeholder = {
                    Text(
                        when (mode) {
                            SuggestMode.Tip -> "z. B. „Am Wochenende früh kommen, der Parkplatz ist klein.“"
                            SuggestMode.Report -> "z. B. „Die Slipanlage ist seit Sommer gesperrt.“"
                            is SuggestMode.Edit -> "z. B. „War im August dort.“"
                        },
                    )
                },
                supportingText = if (mode == SuggestMode.Tip) {
                    @Composable { Text("${text.length}/${Contributions.TIP_LENGTH} Zeichen. Bitte sachlich, ohne Werbung und ohne Links.") }
                } else {
                    null
                },
                minLines = if (mode is SuggestMode.Edit) 2 else 4,
            )
            Consent(
                consent,
                { consent = it },
                "Ich weiß das aus eigener Erfahrung und habe es nicht aus anderen Verzeichnissen abgeschrieben. Slipways " +
                    "darf den Beitrag veröffentlichen" +
                    (if (place.source == Source.OSM) ", als Ergänzung zu OpenStreetMap unter der ODbL" else "") + ".",
            )
            SendState(proof, error, sending)
            Button(onClick = ::send, enabled = canSend, modifier = Modifier.fillMaxWidth()) { Text("Vorschlag senden") }
            Note("Alles wird vor der Veröffentlichung von Hand geprüft. Kein Konto, keine Namen.")
        }
    }
}

/** A dropdown for fields with choices, else a text field. */
@Composable
fun FieldInput(field: SuggestionField, value: String, onChange: (String) -> Unit) {
    if (field.choices.isEmpty()) {
        OutlinedTextField(
            value = value,
            onValueChange = { onChange(it.take(100)) },
            modifier = Modifier.fillMaxWidth(),
            label = { Text(field.label) },
            placeholder = field.placeholder?.let { hint -> @Composable { Text(hint) } },
            singleLine = true,
            keyboardOptions = KeyboardOptions(
                keyboardType = when (field.keyboard) {
                    SuggestionField.Keyboard.TEXT -> KeyboardType.Text
                    SuggestionField.Keyboard.DECIMAL -> KeyboardType.Decimal
                    SuggestionField.Keyboard.PHONE -> KeyboardType.Phone
                    SuggestionField.Keyboard.EMAIL -> KeyboardType.Email
                },
            ),
        )
        return
    }
    var expanded by remember { mutableStateOf(false) }
    Box {
        OutlinedTextField(
            value = if (value.isEmpty()) "weiß nicht" else field.labelOf(value),
            onValueChange = {},
            modifier = Modifier.fillMaxWidth(),
            readOnly = true,
            label = { Text(field.label) },
            trailingIcon = { Icon(Icons.Filled.ArrowDropDown, contentDescription = null) },
        )
        // The read-only field would take the touch itself and only show a cursor.
        Box(Modifier.matchParentSize().clickable(role = Role.DropdownList) { expanded = true })
        DropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            (listOf("" to "weiß nicht") + field.choices).forEach { (choice, label) ->
                DropdownMenuItem(text = { Text(label) }, onClick = {
                    onChange(choice)
                    expanded = false
                })
            }
        }
    }
}

@Composable
fun Consent(checked: Boolean, onChange: (Boolean) -> Unit, text: String) {
    val uriHandler = LocalUriHandler.current
    Column {
        Row(
            Modifier.fillMaxWidth().toggleable(checked, role = Role.Checkbox, onValueChange = onChange),
            horizontalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            Checkbox(checked = checked, onCheckedChange = null, modifier = Modifier.padding(top = 2.dp))
            Text(text, style = MaterialTheme.typography.bodyMedium)
        }
        TextButton(onClick = { uriHandler.openUri(Links.CONTRIBUTE) }) { Text("Mehr dazu") }
    }
}

/** The state of the spam protection and the last error, above the send button. */
@Composable
fun SendState(proof: ProofState, error: String?, sending: Boolean) {
    error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
    proof.error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
    Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
        when {
            sending -> {
                CircularProgressIndicator(Modifier.size(16.dp), strokeWidth = 2.dp)
                Note("Wird gesendet…")
            }
            proof.solution != null -> {
                Icon(Icons.Filled.VerifiedUser, contentDescription = null, Modifier.size(16.dp), tint = MaterialTheme.colorScheme.onSurfaceVariant)
                Note("Spam-Schutz bereit")
            }
            proof.error == null -> {
                CircularProgressIndicator(Modifier.size(16.dp), strokeWidth = 2.dp)
                Note("Spam-Schutz wird berechnet…")
            }
        }
    }
}

/** Thanks after sending, with where to find the suggestion's state. */
@Composable
fun Sent(message: String, onDone: () -> Unit) {
    Column(Modifier.fillMaxWidth(), horizontalAlignment = Alignment.CenterHorizontally, verticalArrangement = Arrangement.spacedBy(12.dp)) {
        Icon(Icons.Filled.CheckCircle, contentDescription = null, Modifier.size(40.dp), tint = Color(0xFF2E8B57))
        Text("Danke!", style = MaterialTheme.typography.titleMedium)
        Text("$message Den Stand siehst du unter Info › Meine Vorschläge, dort kannst du ihn auch zurückziehen.")
        Button(onClick = onDone) { Text("Fertig") }
    }
}

/** A small pencil next to a value that opens the form for it. */
@Composable
fun EditButton(label: String, onClick: () -> Unit) {
    IconButton(onClick = onClick, modifier = Modifier.size(32.dp)) {
        Icon(Icons.Filled.Edit, contentDescription = "$label korrigieren", Modifier.size(16.dp))
    }
}

/** „Was gibt es dort?“ for slipways, missing details, and the ways to contribute, below the facts of a place. */
@OptIn(ExperimentalLayoutApi::class)
@Composable
fun ContributeSection(
    place: SuggestPlace,
    /** The values the place shows now; empty ones count as missing. */
    values: Map<String, String>,
    /** Facts users ticked; only for slipways. */
    ticked: List<TickedFact>,
    osmElement: String?,
    /** Fields this device sent values for that still wait for moderation. */
    pending: Set<String>,
    onSuggest: (SuggestMode) -> Unit,
) {
    val uriHandler = LocalUriHandler.current
    Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
        if (place.type == PlaceType.SLIPWAY) {
            Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
                SectionTitle("Was gibt es dort?")
                if (ticked.isEmpty()) Note("Noch keine Angaben.")
                ticked.forEach { fact ->
                    Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
                        Icon(
                            if (fact.present) Icons.Filled.Check else Icons.Filled.Close,
                            contentDescription = null,
                            Modifier.size(18.dp),
                            tint = if (fact.present) Color(0xFF2E8B57) else MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                        Text(fact.label)
                    }
                }
                ticked.firstOrNull()?.let { Note("von Nutzern ergänzt, Stand ${Format.monthYear(it.retrieved)}") }
                if (ticked.isEmpty() && pending.any { it in Contributions.FACTS }) Pending()
                OutlinedButton(onClick = { onSuggest(SuggestMode.Edit(Contributions.FACTS, "Was gibt es dort?")) }) {
                    Icon(Icons.Filled.Checklist, contentDescription = null, Modifier.padding(end = 8.dp))
                    Text("Angaben ankreuzen")
                }
            }
        }

        val missing = Contributions.wanted(place.type).filter { values[it].isNullOrEmpty() && it !in pending }
        if (missing.isNotEmpty()) {
            Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
                SectionTitle("Fehlt noch")
                Note("Weißt du mehr? Ergänze es, es wird vor der Veröffentlichung geprüft.")
                FlowRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    missing.forEach { field ->
                        OutlinedButton(onClick = { onSuggest(SuggestMode.field(field, adding = true)) }) {
                            Icon(Icons.Filled.Add, contentDescription = null, Modifier.padding(end = 4.dp))
                            Text(Contributions.label(field))
                        }
                    }
                }
            }
        }

        Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
            SectionTitle("Mitmachen")
            FlowRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedButton(onClick = { onSuggest(SuggestMode.Tip) }) {
                    Icon(Icons.Filled.Lightbulb, contentDescription = null, Modifier.padding(end = 4.dp))
                    Text("Tipp geben")
                }
                OutlinedButton(onClick = { onSuggest(SuggestMode.Report) }) {
                    Icon(Icons.Filled.Flag, contentDescription = null, Modifier.padding(end = 4.dp))
                    Text("Etwas stimmt nicht")
                }
                osmElement?.let(Contributions::osmEditUrl)?.let { url ->
                    OutlinedButton(onClick = { uriHandler.openUri(url) }) {
                        Icon(Icons.AutoMirrored.Filled.OpenInNew, contentDescription = null, Modifier.padding(end = 4.dp))
                        Text("In OpenStreetMap bearbeiten")
                    }
                }
            }
        }
    }
}

/** Below a value this device sent a suggestion for that waits. */
@Composable
fun Pending() {
    Text("Dein Vorschlag wird geprüft", style = MaterialTheme.typography.bodySmall, color = Color(0xFFE69500))
}
