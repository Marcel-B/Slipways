package com.bvelop.slipways.ui.contribute

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.produceState
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import com.bvelop.slipways.shared.ApiException
import com.bvelop.slipways.shared.Format
import com.bvelop.slipways.shared.SentSuggestion
import com.bvelop.slipways.shared.SuggestionStatus
import com.bvelop.slipways.ui.overview.Hint
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.launch

/** Suggestions sent from this device, with their state; pending ones can be withdrawn. */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MySuggestionsScreen(onBack: () -> Unit) {
    val store = appContainer().suggestions
    var suggestions by remember { mutableStateOf(store.all()) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Meine Vorschläge") },
                navigationIcon = {
                    IconButton(onClick = onBack) { Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Zurück") }
                },
            )
        },
    ) { padding ->
        Box(Modifier.padding(padding).fillMaxSize()) {
            if (suggestions.isEmpty()) {
                Hint("Noch keine Vorschläge", "Was du zu Slipanlagen und Marinas vorschlägst, erscheint hier, bis es entschieden ist.")
            } else {
                LazyColumn {
                    items(suggestions, key = { it.id }) { suggestion ->
                        SuggestionRow(suggestion) {
                            store.forget(suggestion.id)
                            suggestions = store.all()
                        }
                        HorizontalDivider()
                    }
                }
            }
        }
    }
}

private sealed interface RowState {
    data object Loading : RowState
    data class Loaded(val status: SuggestionStatus) : RowState
    data object Gone : RowState
    data object Failed : RowState
}

@Composable
private fun SuggestionRow(suggestion: SentSuggestion, onWithdrawn: () -> Unit) {
    val api = appContainer().api
    val scope = rememberCoroutineScope()
    var error by remember { mutableStateOf<String?>(null) }
    var withdrawing by remember { mutableStateOf(false) }
    val state by produceState<RowState>(RowState.Loading, suggestion.id) {
        value = try {
            RowState.Loaded(api.suggestion(suggestion.id, suggestion.token))
        } catch (e: CancellationException) {
            throw e
        } catch (e: ApiException) {
            // Decided suggestions are deleted after a while, withdrawn ones at once.
            if (e.status == 404) RowState.Gone else RowState.Failed
        } catch (_: Exception) {
            RowState.Failed
        }
    }

    ListItem(
        headlineContent = { Text(suggestion.name) },
        supportingContent = {
            Column {
                Text("${suggestion.kind.label}, gesendet am ${Format.date(suggestion.sent)}")
                when (val s = state) {
                    RowState.Loading -> Text("Lade…", color = MaterialTheme.colorScheme.onSurfaceVariant)
                    RowState.Gone -> Text("Nicht mehr vorhanden", color = MaterialTheme.colorScheme.onSurfaceVariant)
                    RowState.Failed -> Text("Der Stand konnte nicht geladen werden.", color = MaterialTheme.colorScheme.error)
                    is RowState.Loaded -> when (s.status.status) {
                        "pending" -> Text("Wird geprüft", color = Color(0xFFE69500))
                        "accepted" -> Text("Angenommen am ${s.status.decided?.let(Format::date) ?: "?"}. Danke!", color = Color(0xFF2E8B57))
                        else -> Text(
                            "Nicht übernommen (${s.status.decided?.let(Format::date) ?: "?"}). Oft fehlt eine Quelle oder es passt nicht zum Eintrag.",
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                    }
                }
                error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
            }
        },
        trailingContent = if ((state as? RowState.Loaded)?.status?.status == "pending") {
            @Composable {
                TextButton(enabled = !withdrawing, onClick = {
                    withdrawing = true
                    scope.launch {
                        try {
                            api.withdrawSuggestion(suggestion.id, suggestion.token)
                            onWithdrawn()
                        } catch (e: CancellationException) {
                            throw e
                        } catch (e: Exception) {
                            error = (e as? ApiException)?.userMessage ?: "Der Vorschlag konnte nicht zurückgezogen werden."
                        } finally {
                            withdrawing = false
                        }
                    }
                }) { Text("Zurückziehen") }
            }
        } else {
            null
        },
        modifier = Modifier.padding(vertical = 4.dp),
    )
}
